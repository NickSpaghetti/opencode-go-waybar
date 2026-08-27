using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Brokers.Caches;

/// <summary>The history cache file. See IUsageWindowCacheBroker on the split.</summary>
internal interface IUsageHistoryCacheBroker
{
    /// <summary>Throws when no cache file has been written yet.</summary>
    ValueTask<UsageHistoryCacheState?> ReadStateAsync(CancellationToken cancellationToken);

    ValueTask WriteStateAsync(UsageHistoryCacheState state, CancellationToken cancellationToken);
}
