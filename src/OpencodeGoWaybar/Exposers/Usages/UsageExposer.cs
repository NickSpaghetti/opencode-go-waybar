using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exposures;
using OpencodeGoWaybar.Services.Aggregations.Usage;

namespace OpencodeGoWaybar.Exposers.Usages;

/// <summary>
/// Publishes the usage view. Mapping, not pass-through: the classification arrives
/// already made, and all this adds is the naming a detail window shows. Three
/// scalar constructions, no iteration, no decisions (§3.0.0.0) — the recorded days
/// travel as recorded, so there is no collection to project.
///
/// The remaining catch is the one thing §3.0.0 does ask of an exposer: turning a
/// failure into something the consumer can render, the way a controller maps an
/// exception to a status code.
/// </summary>
internal sealed partial class UsageExposer(
    IUsageAggregationService aggregationService) : IUsageExposer
{
    private const string RollingLabel = "Rolling · 5h";
    private const string WeeklyLabel = "Weekly";
    private const string MonthlyLabel = "Monthly";

    public async ValueTask<UsageView> ExposeUsageAsync(CancellationToken cancellationToken)
    {
        try
        {
            UsageAggregate aggregate = await aggregationService.RetrieveUsageAsync(cancellationToken);

            return new UsageView(
                aggregate.Windows.ProcessIsActive,
                ToView(RollingLabel, aggregate.Windows.Rolling),
                ToView(WeeklyLabel, aggregate.Windows.Weekly),
                ToView(MonthlyLabel, aggregate.Windows.Monthly),
                aggregate.History.RecordedDays,
                aggregate.History.TotalTokens,
                aggregate.Windows.IsRateLimited,
                aggregate.Windows.ApiRetrievedAt,
                aggregate.History.DatabaseLastWriteTime,
                FailureMessage: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CreateFailureView(DescribeFailure(
                new TimeoutException("The usage refresh exceeded its budget.")));
        }
        catch (Exception exception)
        {
            return CreateFailureView(DescribeFailure(exception));
        }
    }

    private static UsageWindowView ToView(string label, UsageWindowState window) =>
        new(label, window.Percent, window.Status, window.ResetsAt);

    /// <summary>
    /// A consumer always receives three labelled windows, so an open window has
    /// something to show when a refresh fails.
    /// </summary>
    private static UsageView CreateFailureView(string failureMessage) =>
        new(ProcessIsActive: false,
            UnknownView(RollingLabel),
            UnknownView(WeeklyLabel),
            UnknownView(MonthlyLabel),
            RecentDays: [],
            RecentTokens: 0,
            IsRateLimited: false,
            ApiRetrievedAt: null,
            DatabaseLastWriteTime: null,
            failureMessage);

    private static UsageWindowView UnknownView(string label) =>
        new(label, Percent: null, UsageWindowStatus.Unknown, ResetsAt: null);
}
