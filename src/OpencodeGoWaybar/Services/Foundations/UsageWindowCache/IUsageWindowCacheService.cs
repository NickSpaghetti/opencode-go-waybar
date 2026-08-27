using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.UsageWindowCache;

internal interface IUsageWindowCacheService
{
    /// <summary>Null when nothing has been cached yet.</summary>
    ValueTask<UsageWindowCacheState?> RetrieveStateAsync(CancellationToken cancellationToken);

    ValueTask StoreStateAsync(UsageWindowCacheState state, CancellationToken cancellationToken);
}
