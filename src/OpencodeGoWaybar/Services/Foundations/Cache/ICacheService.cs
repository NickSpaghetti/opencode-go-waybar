using OpencodeGoWaybar.Models.Processings.Usage;

namespace OpencodeGoWaybar.Services.Foundations.Cache;

internal interface ICacheService
{
    ValueTask<UsageCacheState?> RetrieveStateAsync(CancellationToken cancellationToken);

    ValueTask StoreStateAsync(UsageCacheState state, CancellationToken cancellationToken);

    ValueTask<bool> TryAcquireLockAsync(TimeSpan staleAfter, CancellationToken cancellationToken);

    ValueTask ReleaseLockAsync(CancellationToken cancellationToken);
}
