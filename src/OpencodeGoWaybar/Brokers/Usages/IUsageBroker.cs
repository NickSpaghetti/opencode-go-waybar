using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Brokers.Usages;

internal interface IUsageBroker
{
    ValueTask<UsageApiBrokerResponse> GetUsageAsync(string apiKey, CancellationToken cancellationToken);
}
