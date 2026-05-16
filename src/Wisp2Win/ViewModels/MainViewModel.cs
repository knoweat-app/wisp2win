using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Wisp2Win.Models;
using Wisp2Win.Services;

namespace Wisp2Win.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;
    private DictationState _state;
    private string _status = "Запуск";
    private string _lastTranscript = "";
    private double _downloadProgress;
    private string _hotkeyStatus = "";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ToggleRequested;
    public event EventHandler? DownloadModelRequested;
    public event EventHandler? ShowWindowRequested;
    public event EventHandler? OpenLogsRequested;
    public event EventHandler<HotkeyChangeRequest>? HotkeyChangeRequested;

    public MainViewModel(SettingsService settings, ModelManager modelManager)
    {
        _settings = settings;
        ModelManager = modelManager;
        ToggleCommand = new RelayCommand(() => ToggleRequested?.Invoke(this, EventArgs.Empty));
        DownloadModelCommand = new RelayCommand(() => DownloadModelRequested?.Invoke(this, EventArgs.Empty));
        ShowWindowCommand = new RelayCommand(() => ShowWindowRequested?.Invoke(this, EventArgs.Empty));
        OpenLogsCommand = new RelayCommand(() => OpenLogsRequested?.Invoke(this, EventArgs.Empty));
    }

    public ModelManager ModelManager { get; }

    public IReadOnlyList<ModelProfile> Models => ModelProfile.All;

    public IReadOnlyList<HotkeyOption> Hotkeys => HotkeyOption.All;

    public IReadOnlyList<PasteShortcutOption> PasteShortcuts => PasteShortcutOption.All;

    public AppSettings Settings => _settings.Current;

    public ICommand ToggleCommand { get; }
    public ICommand DownloadModelCommand { get; }
    public ICommand ShowWindowCommand { get; }
    public ICommand OpenLogsCommand { get; }

    public DictationState State
    {
        get => _state;
        set
        {
            if (Set(ref _state, value))
            {
                OnPropertyChanged(nameof(ToggleText));
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string LastTranscript
    {
        get => _lastTranscript;
        set => Set(ref _lastTranscript, value);
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => Set(ref _downloadProgress, value);
    }

    public string HotkeyStatus
    {
        get => _hotkeyStatus;
        set
        {
            if (Set(ref _hotkeyStatus, value))
            {
                OnPropertyChanged(nameof(HotkeyStatusText));
            }
        }
    }

    public string HotkeyDisplay => HotkeyOption.ByValue(Settings.Hotkey).DisplayName;

    public string HotkeyStatusText => $"Горячая клавиша: {HotkeyDisplay}. {HotkeyStatus}";

    public string ToggleText => State == DictationState.Recording ? "Остановить и вставить" : "Начать диктовку";

    public bool IsBusy => State is DictationState.DownloadingModel or DictationState.Transcribing or DictationState.Inserting;

    public ModelProfile SelectedModel
    {
        get => ModelProfile.ById(Settings.ModelId);
        set
        {
            if (Settings.ModelId == value.Id)
            {
                return;
            }

            Settings.ModelId = value.Id;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public HotkeyOption SelectedHotkey
    {
        get => HotkeyOption.ByValue(Settings.Hotkey);
        set
        {
            if (Settings.Hotkey == value.Value)
            {
                return;
            }

            var request = new HotkeyChangeRequest(value.Value);
            HotkeyChangeRequested?.Invoke(this, request);
            if (!request.Accepted)
            {
                HotkeyStatus = "Hotkey is already in use";
                OnPropertyChanged(nameof(SelectedHotkey));
                OnPropertyChanged(nameof(HotkeyDisplay));
                OnPropertyChanged(nameof(HotkeyStatusText));
                return;
            }

            Settings.Hotkey = value.Value;
            _settings.Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HotkeyDisplay));
            OnPropertyChanged(nameof(HotkeyStatusText));
        }
    }

    public PasteShortcutOption SelectedPasteShortcut
    {
        get => PasteShortcutOption.ByValue(Settings.PasteShortcut);
        set
        {
            if (Settings.PasteShortcut == value.Value)
            {
                return;
            }

            Settings.PasteShortcut = value.Value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public string Language
    {
        get => Settings.Language;
        set
        {
            if (Settings.Language == value)
            {
                return;
            }

            Settings.Language = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public bool PasteAfterTranscription
    {
        get => Settings.PasteAfterTranscription;
        set
        {
            if (Settings.PasteAfterTranscription == value)
            {
                return;
            }

            Settings.PasteAfterTranscription = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public bool PolishTranscript
    {
        get => Settings.PolishTranscript;
        set
        {
            if (Settings.PolishTranscript == value)
            {
                return;
            }

            Settings.PolishTranscript = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public bool ShowRecordingOverlay
    {
        get => Settings.ShowRecordingOverlay;
        set
        {
            if (Settings.ShowRecordingOverlay == value)
            {
                return;
            }

            Settings.ShowRecordingOverlay = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    public bool TranslateToEnglish
    {
        get => Settings.TranslateToEnglish;
        set
        {
            if (Settings.TranslateToEnglish == value)
            {
                return;
            }

            Settings.TranslateToEnglish = value;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class HotkeyChangeRequest
{
    public HotkeyChangeRequest(string hotkey)
    {
        Hotkey = hotkey;
    }

    public string Hotkey { get; }
    public bool Accepted { get; set; }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
