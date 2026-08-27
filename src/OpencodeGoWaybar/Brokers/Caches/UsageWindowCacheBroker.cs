using System.Text.Json;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Brokers.Caches;

internal sealed class UsageWindowCacheBroker(OpenCodeGoOptions options) : IUsageWindowCacheBroker
{
    /// <summary>Brokers own their own configuration, filename included (§1.7.3).</summary>
    private string CachePath => Path.Combine(options.CacheDirectory, "windows.json");

    public async ValueTask<UsageWindowCacheState?> ReadStateAsync(CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(CachePath, cancellationToken);

        return JsonSerializer.Deserialize(json, UsageCacheJsonContext.Default.UsageWindowCacheState);
    }

    public async ValueTask WriteStateAsync(
        UsageWindowCacheState state,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.CacheDirectory);

        // Written aside and moved so a reader never sees a half-written file.
        // File.Move is rename(2) — atomic — which is why no lock is needed here.
        var temporaryPath = $"{CachePath}.{Guid.NewGuid():N}.tmp";
        var json = JsonSerializer.Serialize(state, UsageCacheJsonContext.Default.UsageWindowCacheState);
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, CachePath, overwrite: true);
    }
}
