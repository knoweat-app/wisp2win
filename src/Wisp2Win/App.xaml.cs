using System.IO;
using System.Windows;
using Wisp2Win.Services;
using Wisp2Win.ViewModels;
using Wisp2Win.Views;

namespace Wisp2Win;

public partial class App : System.Windows.Application
{
    private NotifyIconService? _notifyIcon;
    private MainWindow? _mainWindow;
    private RecordingOverlayWindow? _overlayWindow;
    private DictationCoordinator? _coordinator;
    private HotkeyService? _hotkeyService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = new SettingsService();
        var modelManager = new ModelManager();
        var recorder = new AudioRecorder();
        var audioFileConverter = new AudioFileConverter();
        var transcriber = new WhisperTranscriber(modelManager);
        var pasteService = new PasteService();
        var windowTargetService = new WindowTargetService();
        var diagnosticsService = new DiagnosticsService();
        _hotkeyService = new HotkeyService();

        var viewModel = new MainViewModel(settings, modelManager);
        _coordinator = new DictationCoordinator(
            settings,
            recorder,
            audioFileConverter,
            transcriber,
            pasteService,
            windowTargetService,
            diagnosticsService,
            beforePaste: () => _mainWindow?.Hide(),
            viewModel: viewModel);

        AppLog.Info("app", "Started");

        _mainWindow = new MainWindow(viewModel);
        _notifyIcon = new NotifyIconService(_mainWindow, _coordinator);

        _hotkeyService.HotkeyPressed += async (_, _) => await _coordinator.ToggleAsync();
        var registered = _hotkeyService.Register(settings.Current.Hotkey);
        viewModel.HotkeyStatus = registered ? "Активна" : "Клавиша уже занята";

        viewModel.ToggleRequested += async (_, _) => await _coordinator.ToggleAsync();
        viewModel.ImportAudioRequested += async (_, _) => await ImportAudioAsync();
        viewModel.ExportTranscriptRequested += async (_, _) => await ExportTranscriptAsync(viewModel);
        viewModel.DownloadModelRequested += async (_, _) => await _coordinator.EnsureModelAsync();
        viewModel.ShowWindowRequested += (_, _) => _mainWindow.ShowAndActivate();
        viewModel.HotkeyChangeRequested += (_, request) =>
        {
            request.Accepted = _hotkeyService.Register(request.Hotkey);
            viewModel.HotkeyStatus = request.Accepted ? "Активна" : "Клавиша уже занята";
        };

        _overlayWindow = new RecordingOverlayWindow(viewModel);
        _mainWindow.Show();
        await _coordinator.EnsureModelAsync();
    }

    private async Task ImportAudioAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выберите аудиофайл",
            Filter = AudioFileConverter.DialogFilter,
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(_mainWindow) == true && _coordinator is not null)
        {
            await _coordinator.ImportAudioAsync(dialog.FileName);
        }
    }

    private async Task ExportTranscriptAsync(MainViewModel viewModel)
    {
        if (string.IsNullOrWhiteSpace(viewModel.LastTranscript))
        {
            viewModel.Status = "Нет текста для экспорта";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Сохранить расшифровку",
            Filter = "Text files|*.txt|All files|*.*",
            DefaultExt = ".txt",
            AddExtension = true,
            FileName = $"wisp2win-transcript-{DateTimeOffset.Now:yyyyMMdd-HHmm}.txt"
        };

        if (dialog.ShowDialog(_mainWindow) != true)
        {
            return;
        }

        try
        {
            await File.WriteAllTextAsync(dialog.FileName, viewModel.LastTranscript);
            viewModel.Status = "TXT сохранен";
        }
        catch (Exception ex)
        {
            AppLog.Error("export", ex);
            viewModel.Status = ex.Message;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _coordinator?.Dispose();
        _hotkeyService?.Dispose();
        _notifyIcon?.Dispose();
        _overlayWindow?.Close();
        base.OnExit(e);
    }
}
