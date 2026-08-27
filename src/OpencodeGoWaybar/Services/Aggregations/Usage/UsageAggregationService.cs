using OpencodeGoWaybar.Brokers.DateTimes;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Orchestrations.UsageHistory;
using OpencodeGoWaybar.Services.Orchestrations.UsageWindows;

namespace OpencodeGoWaybar.Services.Aggregations.Usage;

/// <summary>
/// The knot (§2.4.0). It ties the two usage orchestrations into one contract per
/// exposure surface and holds no business logic: no branching, no iteration, and
/// neither call's result feeds the other.
///
/// Both dependencies are orchestrations, so the variation requirement holds
/// (§2.4.2.0), and both take the same input contract (§2.4.2.6). The process gate
/// that used to sit here — a business rule an aggregation may not hold — lives in
/// the windows orchestration now.
///
/// The two calls are written in sequence, but nothing here depends on that order
/// (§2.4.2.1). What makes the shared cache safe is that each orchestration
/// re-reads the state inside the lock and preserves the slice it does not own; the
/// sequencing is not load-bearing. Running them concurrently would still be safe
/// for the data — but they contend for one cache lock, so the loser would serve
/// stale state and quietly skip its refresh.
/// </summary>
internal sealed class UsageAggregationService(
    IUsageWindowsOrchestrationService windowsOrchestrationService,
    IUsageHistoryOrchestrationService historyOrchestrationService,
    IDateTimeBroker dateTimeBroker) : IUsageAggregationService
{
    public async ValueTask<WaybarStatus> RetrieveStatusAsync(CancellationToken cancellationToken)
    {
        UsageAggregate aggregate = await RetrieveUsageAsync(cancellationToken);

        return new WaybarStatus(
            aggregate.Windows.ProcessIsActive,
            new UsageSnapshot(
                aggregate.Windows.Usage,
                aggregate.History.RecordedDays,
                aggregate.Windows.ApiRetrievedAt,
                aggregate.History.DatabaseLastWriteTime),
            aggregate.Windows.IsRateLimited);
    }

    public async ValueTask<UsageAggregate> RetrieveUsageAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = dateTimeBroker.GetCurrentDateTime();

        UsageWindowSnapshot windows =
            await windowsOrchestrationService.RetrieveWindowsAsync(now, cancellationToken);

        UsageHistorySnapshot history =
            await historyOrchestrationService.RetrieveHistoryAsync(now, cancellationToken);

        return new UsageAggregate(windows, history);
    }
}
