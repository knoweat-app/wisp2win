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
        var transcriber = new WhisperTranscriber(modelManager);
        var pasteService = new PasteService();
        var windowTargetService = new WindowTargetService();
        var diagnosticsService = new DiagnosticsService();
        _hotkeyService = new HotkeyService();

        var viewModel = new MainViewModel(settings, modelManager);
        _coordinator = new DictationCoordinator(
            settings,
            recorder,
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

    protected override void OnExit(ExitEventArgs e)
    {
        _coordinator?.Dispose();
        _hotkeyService?.Dispose();
        _notifyIcon?.Dispose();
        _overlayWindow?.Close();
        base.OnExit(e);
    }
}
