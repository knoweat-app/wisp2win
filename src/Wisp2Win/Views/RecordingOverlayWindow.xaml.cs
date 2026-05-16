using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using Wisp2Win.Models;
using Wisp2Win.ViewModels;

namespace Wisp2Win.Views;

public partial class RecordingOverlayWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Storyboard _pulseStoryboard;

    public RecordingOverlayWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _pulseStoryboard = (Storyboard)FindResource("PulseStoryboard");
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
            PositionWindow();
            if (!IsVisible)
            {
                Show();
            }

            _pulseStoryboard.Begin(this, true);
            return;
        }

        _pulseStoryboard.Stop(this);
        Hide();
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
        base.OnClosed(e);
    }
}
