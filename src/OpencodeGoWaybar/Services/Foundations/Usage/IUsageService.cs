using OpencodeGoWaybar.Brokers.Usages;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.Usage;

internal interface IUsageService
{
    ValueTask<UsageResponse> RetrieveUsageAsync(CancellationToken cancellationToken);
}
