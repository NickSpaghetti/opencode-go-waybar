using Microsoft.Extensions.DependencyInjection;
using OpencodeGoWaybar.Configurations;
using OpencodeGoWaybar.Exposers.Waybar;

namespace OpencodeGoWaybar;

internal static class Program
{
    internal static async Task<int> Main()
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = cancellationSource.Token;

        using var serviceProvider = UsageComposition.BuildServiceProvider();

        var waybarExposer = serviceProvider.GetRequiredService<IWaybarExposer>();

        Console.WriteLine(await waybarExposer.ExposeAsync(cancellationToken));

        return 0;
    }
}
