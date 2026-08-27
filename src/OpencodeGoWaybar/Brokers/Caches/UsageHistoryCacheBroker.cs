using System.Text.Json;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Brokers.Caches;

internal sealed class UsageHistoryCacheBroker(OpenCodeGoOptions options) : IUsageHistoryCacheBroker
{
    private string CachePath => Path.Combine(options.CacheDirectory, "history.json");

    public async ValueTask<UsageHistoryCacheState?> ReadStateAsync(CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(CachePath, cancellationToken);

        return JsonSerializer.Deserialize(json, UsageCacheJsonContext.Default.UsageHistoryCacheState);
    }

    public async ValueTask WriteStateAsync(
        UsageHistoryCacheState state,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.CacheDirectory);

        var temporaryPath = $"{CachePath}.{Guid.NewGuid():N}.tmp";
        var json = JsonSerializer.Serialize(state, UsageCacheJsonContext.Default.UsageHistoryCacheState);
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, CachePath, overwrite: true);
    }
}
