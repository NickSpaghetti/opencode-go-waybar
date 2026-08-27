using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.UsageHistoryCache;

internal interface IUsageHistoryCacheService
{
    /// <summary>Null when nothing has been cached yet.</summary>
    ValueTask<UsageHistoryCacheState?> RetrieveStateAsync(CancellationToken cancellationToken);

    ValueTask StoreStateAsync(UsageHistoryCacheState state, CancellationToken cancellationToken);
}
