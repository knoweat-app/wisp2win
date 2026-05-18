using System.IO;
using Wisp2Win.Models;
using Wisp2Win.ViewModels;

namespace Wisp2Win.Services;

public sealed class DictationCoordinator : IDisposable
{
    private readonly SettingsService _settings;
    private readonly AudioRecorder _recorder;
    private readonly AudioFileConverter _audioFileConverter;
    private readonly WhisperTranscriber _transcriber;
    private readonly TranscriptPostProcessor _postProcessor = new();
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
        AudioFileConverter audioFileConverter,
        WhisperTranscriber transcriber,
        PasteService pasteService,
        WindowTargetService windowTargetService,
        DiagnosticsService diagnosticsService,
        Action? beforePaste,
        MainViewModel viewModel)
    {
        _settings = settings;
        _recorder = recorder;
        _audioFileConverter = audioFileConverter;
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
            _viewModel.Status = "Готово";
            return;
        }

        _viewModel.State = DictationState.DownloadingModel;
        _viewModel.Status = $"Загрузка модели {profile.DisplayName}";
        try
        {
            await _viewModel.ModelManager.EnsureInstalledAsync(
                profile,
                new Progress<double>(value => _viewModel.DownloadProgress = value));
            _viewModel.State = DictationState.Idle;
            _viewModel.Status = "Готово";
            _viewModel.RefreshModelInstalled();
        }
        catch (Exception ex)
        {
            AppLog.Error("model", ex);
            _viewModel.State = DictationState.Error;
            _viewModel.Status = ex.Message;
        }
    }

    public async Task ImportAudioAsync(string sourcePath)
    {
        await _gate.WaitAsync();
        try
        {
            if (_viewModel.State is DictationState.Recording or DictationState.Transcribing or DictationState.DownloadingModel or DictationState.Inserting)
            {
                return;
            }

            _viewModel.State = DictationState.Transcribing;
            _viewModel.Status = "Подготовка аудиофайла";
            string? wavPath = null;

            try
            {
                await EnsureModelAsync();
                EnsureSelectedModelInstalled();
                _viewModel.State = DictationState.Transcribing;
                _viewModel.Status = "Подготовка аудиофайла";

                wavPath = _audioFileConverter.ConvertToWhisperWav(sourcePath);
                _viewModel.Status = "Распознавание файла";

                var text = await TranscribeCurrentSettingsAsync(wavPath);
                _viewModel.LastTranscript = text;
                _viewModel.State = DictationState.Idle;
                _viewModel.Status = string.IsNullOrWhiteSpace(text) ? "Речь не обнаружена" : "Файл расшифрован";
            }
            catch (Exception ex)
            {
                AppLog.Error("import", ex);
                _viewModel.State = DictationState.Error;
                _viewModel.Status = ex.Message;
            }
            finally
            {
                TryDelete(wavPath);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void StartRecording()
    {
        _windowTargetService.CaptureForegroundWindow();
        _recordingPath = _recorder.Start();
        _viewModel.State = DictationState.Recording;
        _viewModel.Status = "Идет запись";
    }

    private async Task StopAndTranscribeAsync()
    {
        var path = _recorder.Stop();
        _recordingPath = null;
        _viewModel.State = DictationState.Transcribing;
        _viewModel.Status = "Распознавание";

        try
        {
            await EnsureModelAsync();
            EnsureSelectedModelInstalled();
            _viewModel.State = DictationState.Transcribing;

            var text = await TranscribeCurrentSettingsAsync(path);

            _viewModel.LastTranscript = text;
            if (!string.IsNullOrWhiteSpace(text) && _settings.Current.PasteAfterTranscription)
            {
                _viewModel.State = DictationState.Inserting;
                _viewModel.Status = "Вставка";
                AppLog.Info("paste", $"Before hide foreground={_windowTargetService.DescribeForegroundWindow()}");
                _beforePaste?.Invoke();
                AppLog.Info("paste", $"After hide foreground={_windowTargetService.DescribeForegroundWindow()}");
                await _pasteService.PasteAsync(
                    text,
                    ResolvePasteShortcut(),
                    _windowTargetService.ActivateLastExternalWindow);
            }

            _viewModel.State = DictationState.Idle;
            _viewModel.Status = string.IsNullOrWhiteSpace(text) ? "Речь не обнаружена" : "Готово";
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

    private async Task<string> TranscribeCurrentSettingsAsync(string wavPath)
    {
        var text = await _transcriber.TranscribeAsync(
            wavPath,
            ModelProfile.ById(_settings.Current.ModelId),
            _settings.Current.Language,
            _settings.Current.TranslateToEnglish);

        if (_settings.Current.PolishTranscript)
        {
            text = _postProcessor.Polish(text, _settings.Current.Language);
        }

        return text;
    }

    private void EnsureSelectedModelInstalled()
    {
        var profile = ModelProfile.ById(_settings.Current.ModelId);
        if (!_viewModel.ModelManager.IsInstalled(profile))
        {
            throw new InvalidOperationException($"Model {profile.DisplayName} is not installed.");
        }
    }

    private string ResolvePasteShortcut()
    {
        var configured = _settings.Current.PasteShortcut;
        if (!string.Equals(configured, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return configured;
        }

        var title = _windowTargetService.LastExternalWindowTitle;
        var method = IsTermius(title) ? "type-text" : IsTerminalLike(title) ? "ctrl-shift-v" : "ctrl-v";
        AppLog.Info("paste", $"Auto insert method={method}; target={_windowTargetService.DescribeLastExternalWindow()}");
        return method;
    }

    private static bool IsTermius(string title) =>
        title.Contains("Termius", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalLike(string title)
    {
        return title.Contains("Windows Terminal", StringComparison.OrdinalIgnoreCase)
            || title.Contains("PowerShell", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Command Prompt", StringComparison.OrdinalIgnoreCase)
            || title.Contains("cmd.exe", StringComparison.OrdinalIgnoreCase)
            || title.Contains("WSL", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _recorder.Dispose();
        _windowTargetService.Dispose();
        TryDelete(_recordingPath);
        _gate.Dispose();
    }
}
