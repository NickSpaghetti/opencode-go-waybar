using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Aggregations.Usage;

internal interface IUsageAggregationService
{
    /// <summary>The contract the Waybar module's exposer maps to a JSON payload.</summary>
    ValueTask<WaybarStatus> RetrieveStatusAsync(CancellationToken cancellationToken);

    /// <summary>Both halves of the usage picture, classified and totalled.</summary>
    ValueTask<UsageAggregate> RetrieveUsageAsync(CancellationToken cancellationToken);
}
