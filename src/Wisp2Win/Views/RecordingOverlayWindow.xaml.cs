using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wisp2Win.Models;
using Wisp2Win.ViewModels;

namespace Wisp2Win.Views;

public partial class RecordingOverlayWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Storyboard _pulseStoryboard;
    private readonly Storyboard _fadeInStoryboard;
    private readonly Storyboard _fadeOutStoryboard;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private bool _isRecordingVisible;

    public RecordingOverlayWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _pulseStoryboard = (Storyboard)FindResource("PulseStoryboard");
        _fadeInStoryboard = (Storyboard)FindResource("FadeInStoryboard");
        _fadeOutStoryboard = (Storyboard)FindResource("FadeOutStoryboard");
        _fadeOutStoryboard.Completed += (_, _) =>
        {
            if (!_isRecordingVisible)
            {
                Hide();
            }
        };
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateElapsedText();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += (_, _) => PositionWindow();
        SourceInitialized += (_, _) => PositionWindow();
        UpdateVisibility();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.State) or nameof(MainViewModel.ShowRecordingOverlay))
        {
            Dispatcher.Invoke(UpdateVisibility);
        }
    }

    private void UpdateVisibility()
    {
        if (_viewModel.State == DictationState.Recording && _viewModel.ShowRecordingOverlay)
        {
            ShowRecording();
            return;
        }

        HideRecording();
    }

    private void ShowRecording()
    {
        if (_isRecordingVisible)
        {
            return;
        }

        _isRecordingVisible = true;
        _stopwatch.Restart();
        UpdateElapsedText();
        PositionWindow();
        if (!IsVisible)
        {
            Opacity = 0;
            Show();
        }

        _timer.Start();
        _fadeOutStoryboard.Stop(this);
        _fadeInStoryboard.Begin(this, true);
        _pulseStoryboard.Begin(this, true);
    }

    private void HideRecording()
    {
        if (!_isRecordingVisible && !IsVisible)
        {
            return;
        }

        _isRecordingVisible = false;
        _stopwatch.Stop();
        _timer.Stop();
        _pulseStoryboard.Stop(this);
        _fadeInStoryboard.Stop(this);
        _fadeOutStoryboard.Begin(this, true);
    }

    private void UpdateElapsedText()
    {
        var elapsed = _stopwatch.Elapsed;
        ElapsedText.Text = elapsed.TotalHours >= 1
            ? elapsed.ToString(@"h\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");
    }

    private void PositionWindow()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Bottom - Height - 34;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _timer.Stop();
        base.OnClosed(e);
    }
}
