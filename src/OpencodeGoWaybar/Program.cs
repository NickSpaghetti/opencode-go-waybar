using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using OpencodeGoWaybar.Configurations;
using OpencodeGoWaybar.Exposers.Waybar;

namespace OpencodeGoWaybar;

internal static class Program
{
    private const string WatchFlag = "--watch";

    internal static async Task<int> Main(string[] args)
    {
        using var serviceProvider = UsageComposition.BuildServiceProvider();

        return args.Contains(WatchFlag, StringComparer.Ordinal)
            ? await WatchAsync(serviceProvider)
            : await ReportOnceAsync(serviceProvider);
    }

    /// <summary>
    /// One payload and out, for a Waybar module configured with an interval. This
    /// is still the default: nothing about the module requires a resident process,
    /// and a one-shot binary is far easier to reason about when something is wrong.
    /// </summary>
    private static async Task<int> ReportOnceAsync(IServiceProvider serviceProvider)
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var waybarExposer = serviceProvider.GetRequiredService<IWaybarExposer>();

        Console.WriteLine(await waybarExposer.ExposeAsync(cancellationSource.Token));

        return 0;
    }

    /// <summary>
    /// Stays resident and writes a payload whenever the bar should change. Waybar
    /// reads one JSON object per line for as long as the process lives.
    ///
    /// Ctrl+C and SIGTERM are handled rather than left to kill the process, so
    /// that Waybar restarting the module — or a user stopping the bar — unwinds
    /// the compositor connection instead of dropping it.
    /// </summary>
    private static async Task<int> WatchAsync(IServiceProvider serviceProvider)
    {
        using var cancellationSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArguments) =>
        {
            eventArguments.Cancel = true;
            cancellationSource.Cancel();
        };

        using var termination = PosixSignalRegistration.Create(
            PosixSignal.SIGTERM,
            context =>
            {
                context.Cancel = true;
                cancellationSource.Cancel();
            });

        var streamExposer = serviceProvider.GetRequiredService<IWaybarStreamExposer>();

        try
        {
            await foreach (var payload in streamExposer.ExposeStreamAsync(cancellationSource.Token))
            {
                Console.WriteLine(payload);
            }
        }
        catch (OperationCanceledException)
        {
            // Asked to stop. That is not a failure.
        }

        return 0;
    }
}
