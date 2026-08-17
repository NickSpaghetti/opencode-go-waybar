using OpencodeGoWaybar.Models.Processings.Usage;

namespace OpencodeGoWaybar.Brokers.Storages.Cache;

internal interface ICacheBroker
{
    ValueTask<UsageCacheState?> RetrieveStateAsync(CancellationToken cancellationToken);

    ValueTask StoreStateAsync(UsageCacheState state, CancellationToken cancellationToken);

    ValueTask<bool> TryAcquireLockAsync(TimeSpan staleAfter, CancellationToken cancellationToken);

    ValueTask ReleaseLockAsync(CancellationToken cancellationToken);
}
