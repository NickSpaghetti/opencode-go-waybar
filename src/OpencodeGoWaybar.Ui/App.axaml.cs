using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using OpencodeGoWaybar.Configurations;
using OpencodeGoWaybar.Exposers.Themes;
using OpencodeGoWaybar.Exposers.Usages;
using OpencodeGoWaybar.Models.Themes;
using OpencodeGoWaybar.Ui.Theming;
using OpencodeGoWaybar.Ui.ViewModels;
using OpencodeGoWaybar.Ui.Views;

namespace OpencodeGoWaybar.Ui;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string[] arguments = desktop.Args ?? [];

            // The same composition the Waybar module uses. The UI resolves only
            // exposers from it; every broker and service stays internal.
            this.serviceProvider = UsageComposition.BuildServiceProvider();

            var themeExposer = this.serviceProvider.GetRequiredService<IThemeExposer>();
            var usageExposer = this.serviceProvider.GetRequiredService<IUsageExposer>();

            StartThemeMatching(themeExposer, forceLight: arguments.Contains("--light"));

            var viewModel = new UsageWindowViewModel(usageExposer, TimeProvider.System);

            desktop.MainWindow = CreateWindow(arguments, viewModel);
            desktop.Exit += (_, _) => this.serviceProvider?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateWindow(string[] arguments, UsageWindowViewModel viewModel)
    {
        if (arguments.Contains("--rings"))
        {
            return new RingPopup { DataContext = viewModel };
        }

        if (arguments.Contains("--dashboard"))
        {
            return new UsageWindow { DataContext = viewModel };
        }

        return new MeterPopup { DataContext = viewModel };
    }

    /// <summary>
    /// Reads the bar's palette and keeps following it.
    ///
    /// Applied asynchronously rather than blocking startup: the retrieval awaits
    /// file reads, and waiting on it from the UI thread before the loop is running
    /// risks deadlocking against Avalonia's own synchronization context. The cost
    /// is that the first frame may use the shipped fallback, which is the same
    /// path a live theme change takes anyway.
    /// </summary>
    private void StartThemeMatching(IThemeExposer themeExposer, bool forceLight)
    {
        if (forceLight)
        {
            RequestedThemeVariant = ThemeVariant.Light;
        }

        _ = ApplyPaletteAsync(themeExposer, forceLight);

        themeExposer.WatchPalette(palette =>
            Dispatcher.UIThread.Post(() => ApplyPalette(palette, forceLight)));
    }

    private async Task ApplyPaletteAsync(IThemeExposer themeExposer, bool forceLight)
    {
        try
        {
            ThemePalette? palette =
                await themeExposer.ExposePaletteAsync(CancellationToken.None);

            if (palette is not null)
            {
                Dispatcher.UIThread.Post(() => ApplyPalette(palette, forceLight));
            }
        }
        catch (Exception)
        {
            // Already logged behind the exposer. A machine whose stylesheet cannot
            // be read still gets a usable window on the shipped palette.
        }
    }

    private void ApplyPalette(ThemePalette palette, bool forceLight)
    {
        PaletteApplier.Apply(Resources, palette);

        // --light is an explicit override, so a dark bar does not undo it.
        if (!forceLight)
        {
            RequestedThemeVariant = palette.IsLight ? ThemeVariant.Light : ThemeVariant.Dark;
        }
    }
}
