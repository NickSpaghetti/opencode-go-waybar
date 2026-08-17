using OpencodeGoWaybar.Models.Processings.Usage;

namespace OpencodeGoWaybar.Services.Processings.Usage;

internal interface IUsageProcessingService
{
    ValueTask<UsageSnapshot> RetrieveUsageAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
