namespace OpencodeGoWaybar.Brokers.Apis.Usage;

internal interface IUsageBroker
{
    ValueTask<UsageApiBrokerResponse> GetUsageAsync(string apiKey, CancellationToken cancellationToken);
}
