using Avalonia;

namespace OpencodeGoWaybar.Ui;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// UseWaylandWithFallback is documented as a no-op away from Linux, so this
    /// single path runs the native Wayland backend on Hyprland and the platform
    /// backend on macOS during development — no environment sniffing needed.
    ///
    /// OPENCODE_GO_UI_BACKEND=x11 is the escape hatch for the one case that
    /// cannot be checked without the hardware: fractional scaling under the
    /// native backend. Forcing it drops the window onto XWayland instead.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        AppBuilder builder = AppBuilder.Configure<App>().UsePlatformDetect();

        if (Environment.GetEnvironmentVariable("OPENCODE_GO_UI_BACKEND") != "x11")
        {
            builder = builder.UseWaylandWithFallback();
        }

        return builder.WithInterFont().LogToTrace();
    }
}
