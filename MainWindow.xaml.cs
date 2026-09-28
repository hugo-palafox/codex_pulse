using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CodexPulse.Services;
using CodexPulse.ViewModels;

namespace CodexPulse;

public partial class MainWindow : Window
{
    private readonly CodexAppServerClient _codexClient = new();
    private readonly DashboardViewModel _viewModel = new();
    private readonly DispatcherTimer _refreshTimer;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _codexClient.RateLimitsUpdated += OnRateLimitsUpdated;
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
        _refreshTimer.Start();
        Loaded += async (_, _) => await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_viewModel.IsLoading)
        {
            return;
        }

        _viewModel.SetLoading(true);
        try
        {
            var limits = await _codexClient.GetRateLimitsAsync();
            _viewModel.Apply(limits);
        }
        catch (Exception exception)
        {
            _viewModel.ShowError(exception);
        }
        finally
        {
            _viewModel.SetLoading(false);
        }
    }

    private void OnRateLimitsUpdated(object? sender, Models.RateLimitsResponse response)
    {
        Dispatcher.Invoke(() => _viewModel.Apply(response));
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void DragHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            e.Handled = true;
            DragMove();
        }
    }

    private void ToggleTopmostClick(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        TopmostIcon.Opacity = Topmost ? 1 : 0.35;
    }

    private void HideClick(object sender, RoutedEventArgs e) => Hide();

    private async void RefreshClick(object sender, RoutedEventArgs e) => await RefreshAsync();

    public void DisposeClient()
    {
        _refreshTimer.Stop();
        _codexClient.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

}
