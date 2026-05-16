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
    private string _status = "Starting";
    private string _lastTranscript = "";
    private double _downloadProgress;
    private string _hotkeyStatus = "";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ToggleRequested;
    public event EventHandler? DownloadModelRequested;
    public event EventHandler? ShowWindowRequested;

    public MainViewModel(SettingsService settings, ModelManager modelManager)
    {
        _settings = settings;
        ModelManager = modelManager;
        ToggleCommand = new RelayCommand(() => ToggleRequested?.Invoke(this, EventArgs.Empty));
        DownloadModelCommand = new RelayCommand(() => DownloadModelRequested?.Invoke(this, EventArgs.Empty));
        ShowWindowCommand = new RelayCommand(() => ShowWindowRequested?.Invoke(this, EventArgs.Empty));
    }

    public ModelManager ModelManager { get; }

    public IReadOnlyList<ModelProfile> Models => ModelProfile.All;

    public AppSettings Settings => _settings.Current;

    public ICommand ToggleCommand { get; }
    public ICommand DownloadModelCommand { get; }
    public ICommand ShowWindowCommand { get; }

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
        set => Set(ref _hotkeyStatus, value);
    }

    public string ToggleText => State == DictationState.Recording ? "Stop and insert" : "Start dictation";

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
