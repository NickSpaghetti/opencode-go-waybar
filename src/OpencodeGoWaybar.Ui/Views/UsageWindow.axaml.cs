using Avalonia.Controls;
using Avalonia.Threading;
using OpencodeGoWaybar.Ui.ViewModels;

namespace OpencodeGoWaybar.Ui.Views;

/// <summary>
/// The full dashboard. Unlike the two popups it stays open, so it polls: the
/// orchestration behind the exposer throttles to the configured refresh interval
/// anyway, which makes a timer here cheap rather than chatty.
/// </summary>
public partial class UsageWindow : Window
{
    private readonly DispatcherTimer timer = new()
    {
        Interval = TimeSpan.FromSeconds(30),
    };

    public UsageWindow()
    {
        InitializeComponent();

        this.timer.Tick += async (_, _) => await RefreshAsync();

        Opened += async (_, _) =>
        {
            await RefreshAsync();
            this.timer.Start();
        };

        Closed += (_, _) => this.timer.Stop();
    }

    private async Task RefreshAsync()
    {
        if (DataContext is UsageWindowViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}
