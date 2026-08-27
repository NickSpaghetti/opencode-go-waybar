using OpencodeGoWaybar.Brokers.Caches;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.UsageHistoryCache;

/// <summary>
/// The history half of the cache. Same shape as the window service over its own
/// file: a missing file means "nothing cached yet", and every routine is a single
/// broker call (§2.1.2.0).
/// </summary>
internal sealed partial class UsageHistoryCacheService(
    IUsageHistoryCacheBroker cacheBroker,
    ILoggingBroker loggingBroker,
    OpenCodeGoOptions options) : IUsageHistoryCacheService
{
    public ValueTask<UsageHistoryCacheState?> RetrieveStateAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(() => RetrieveStateCoreAsync(cancellationToken));

    public ValueTask StoreStateAsync(
        UsageHistoryCacheState state,
        CancellationToken cancellationToken) =>
        TryCatchAsync(() => StoreStateCoreAsync(state, cancellationToken));

    private async ValueTask<UsageHistoryCacheState?> RetrieveStateCoreAsync(
        CancellationToken cancellationToken)
    {
        ValidateCacheDirectory();

        try
        {
            return await cacheBroker.ReadStateAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // Nothing has been cached yet; that is a state, not a failure.
            return null;
        }
    }

    private async ValueTask StoreStateCoreAsync(
        UsageHistoryCacheState state,
        CancellationToken cancellationToken)
    {
        ValidateCacheDirectory();
        ValidateState(state);

        await cacheBroker.WriteStateAsync(state, cancellationToken);
    }
}
