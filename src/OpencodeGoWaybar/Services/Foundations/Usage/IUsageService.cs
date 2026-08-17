using OpencodeGoWaybar.Brokers.Apis.Usage;

namespace OpencodeGoWaybar.Services.Foundations.Usage;

internal interface IUsageService
{
    ValueTask<UsageResponse> RetrieveUsageAsync(CancellationToken cancellationToken);
}
