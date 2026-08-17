using OpencodeGoWaybar.Brokers.Storages.Cache;
using OpencodeGoWaybar.Models.Processings.Usage;

namespace OpencodeGoWaybar.Services.Foundations.Cache;

internal sealed class CacheService(ICacheBroker broker) : ICacheService
{
    public ValueTask<UsageCacheState?> RetrieveStateAsync(CancellationToken cancellationToken) =>
        broker.RetrieveStateAsync(cancellationToken);

    public ValueTask StoreStateAsync(UsageCacheState state, CancellationToken cancellationToken) =>
        broker.StoreStateAsync(state, cancellationToken);

    public ValueTask<bool> TryAcquireLockAsync(TimeSpan staleAfter, CancellationToken cancellationToken) =>
        broker.TryAcquireLockAsync(staleAfter, cancellationToken);

    public ValueTask ReleaseLockAsync(CancellationToken cancellationToken) =>
        broker.ReleaseLockAsync(cancellationToken);
}
