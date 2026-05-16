using System.IO;
using Wisp2Win.Models;
using Wisp2Win.ViewModels;

namespace Wisp2Win.Services;

public sealed class DictationCoordinator : IDisposable
{
    private readonly SettingsService _settings;
    private readonly AudioRecorder _recorder;
    private readonly WhisperTranscriber _transcriber;
    private readonly PasteService _pasteService;
    private readonly WindowTargetService _windowTargetService;
    private readonly Action? _beforePaste;
    private readonly DiagnosticsService _diagnosticsService;
    private readonly MainViewModel _viewModel;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _recordingPath;

    public DictationCoordinator(
        SettingsService settings,
        AudioRecorder recorder,
        WhisperTranscriber transcriber,
        PasteService pasteService,
        WindowTargetService windowTargetService,
        DiagnosticsService diagnosticsService,
        Action? beforePaste,
        MainViewModel viewModel)
    {
        _settings = settings;
        _recorder = recorder;
        _transcriber = transcriber;
        _pasteService = pasteService;
        _windowTargetService = windowTargetService;
        _diagnosticsService = diagnosticsService;
        _beforePaste = beforePaste;
        _viewModel = viewModel;
        _viewModel.OpenLogsRequested += (_, _) => _diagnosticsService.OpenLogsDirectory();
    }

    public async Task ToggleAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_viewModel.State is DictationState.Transcribing or DictationState.DownloadingModel or DictationState.Inserting)
            {
                return;
            }

            if (_viewModel.State == DictationState.Recording)
            {
                AppLog.Info("dictation", "Toggle stop");
                await StopAndTranscribeAsync();
            }
            else
            {
                AppLog.Info("dictation", "Toggle start");
                StartRecording();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EnsureModelAsync()
    {
        var profile = ModelProfile.ById(_settings.Current.ModelId);
        if (_viewModel.ModelManager.IsInstalled(profile))
        {
            _viewModel.DownloadProgress = 1;
            _viewModel.Status = "Ready";
            return;
        }

        _viewModel.State = DictationState.DownloadingModel;
        _viewModel.Status = $"Downloading {profile.DisplayName} model";
        try
        {
            await _viewModel.ModelManager.EnsureInstalledAsync(
                profile,
                new Progress<double>(value => _viewModel.DownloadProgress = value));
            _viewModel.State = DictationState.Idle;
            _viewModel.Status = "Ready";
        }
        catch (Exception ex)
        {
            AppLog.Error("model", ex);
            _viewModel.State = DictationState.Error;
            _viewModel.Status = ex.Message;
        }
    }

    private void StartRecording()
    {
        _windowTargetService.CaptureForegroundWindow();
        _recordingPath = _recorder.Start();
        _viewModel.State = DictationState.Recording;
        _viewModel.Status = "Recording";
    }

    private async Task StopAndTranscribeAsync()
    {
        var path = _recorder.Stop();
        _recordingPath = null;
        _viewModel.State = DictationState.Transcribing;
        _viewModel.Status = "Transcribing";

        try
        {
            await EnsureModelAsync();
            _viewModel.State = DictationState.Transcribing;

            var text = await _transcriber.TranscribeAsync(
                path,
                ModelProfile.ById(_settings.Current.ModelId),
                _settings.Current.Language,
                _settings.Current.TranslateToEnglish);

            _viewModel.LastTranscript = text;
            if (!string.IsNullOrWhiteSpace(text) && _settings.Current.PasteAfterTranscription)
            {
                _viewModel.State = DictationState.Inserting;
                _viewModel.Status = "Inserting";
                AppLog.Info("paste", $"Before hide foreground={_windowTargetService.DescribeForegroundWindow()}");
                _beforePaste?.Invoke();
                AppLog.Info("paste", $"After hide foreground={_windowTargetService.DescribeForegroundWindow()}");
                await _pasteService.PasteAsync(text, _windowTargetService.ActivateLastExternalWindow);
            }

            _viewModel.State = DictationState.Idle;
            _viewModel.Status = string.IsNullOrWhiteSpace(text) ? "No speech detected" : "Ready";
        }
        catch (Exception ex)
        {
            AppLog.Error("dictation", ex);
            _viewModel.State = DictationState.Error;
            _viewModel.Status = ex.Message;
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static void TryDelete(string? path)
    {
        if (path is null)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch
        {
            // Temp audio cleanup is best-effort.
        }
    }

    public void Dispose()
    {
        _recorder.Dispose();
        _windowTargetService.Dispose();
        TryDelete(_recordingPath);
        _gate.Dispose();
    }
}
