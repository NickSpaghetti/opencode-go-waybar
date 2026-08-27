using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Orchestrations.UsageHistory;

internal interface IUsageHistoryOrchestrationService
{
    ValueTask<UsageHistorySnapshot> RetrieveHistoryAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
