using OpencodeGoWaybar.Brokers.Caches;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.UsageWindowCache;

/// <summary>
/// Owns one meaning: that a missing cache file is "nothing cached yet" rather than
/// a failure.
///
/// Every routine is a single broker call, which is what Pure-Primitive asks for
/// (§2.1.2.0). The compound TryAcquireLockAsync that used to sit here — create,
/// then read a timestamp, then compare, then delete — is gone with the lock it
/// managed.
/// </summary>
internal sealed partial class UsageWindowCacheService(
    IUsageWindowCacheBroker cacheBroker,
    ILoggingBroker loggingBroker,
    OpenCodeGoOptions options) : IUsageWindowCacheService
{
    public ValueTask<UsageWindowCacheState?> RetrieveStateAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(() => RetrieveStateCoreAsync(cancellationToken));

    public ValueTask StoreStateAsync(
        UsageWindowCacheState state,
        CancellationToken cancellationToken) =>
        TryCatchAsync(() => StoreStateCoreAsync(state, cancellationToken));

    private async ValueTask<UsageWindowCacheState?> RetrieveStateCoreAsync(
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
        UsageWindowCacheState state,
        CancellationToken cancellationToken)
    {
        ValidateCacheDirectory();
        ValidateState(state);

        await cacheBroker.WriteStateAsync(state, cancellationToken);
    }
}
