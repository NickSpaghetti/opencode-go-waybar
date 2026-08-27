using NSubstitute;
using OpencodeGoWaybar.Exposers.Usages;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Aggregations.Usage;

namespace OpencodeGoWaybar.UnitTests.Exposers.Usages;

/// <summary>
/// The exposer maps: the classification arrives already made, and all it adds is
/// the naming a detail window shows. What is left to test is that the mapping is
/// faithful, and that a failure becomes something a consumer can render — the one
/// decision §3.0.0 asks an exposer to make.
///
/// The classification and threshold tests that used to live here moved down to
/// UsageWindowsOrchestrationServiceTests along with the logic.
/// </summary>
public sealed partial class UsageExposerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 15, 17, 0, TimeSpan.Zero);

    private static UsageExposer CreateExposer(IUsageAggregationService aggregationService) =>
        new(aggregationService);

    private static IUsageAggregationService CreateAggregationService(UsageAggregate aggregate)
    {
        var aggregationService = Substitute.For<IUsageAggregationService>();

        aggregationService.RetrieveUsageAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(aggregate));

        return aggregationService;
    }

    private static IUsageAggregationService CreateFailingAggregationService(Exception exception)
    {
        var aggregationService = Substitute.For<IUsageAggregationService>();

        aggregationService.RetrieveUsageAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageAggregate>>(_ => throw exception);

        return aggregationService;
    }

    private static UsageAggregate CreateAggregate() =>
        new(new UsageWindowSnapshot(
                ProcessIsActive: true,
                new UsageWindowState(61, UsageWindowStatus.Throttled, Now.AddHours(4)),
                new UsageWindowState(24, UsageWindowStatus.Ok, null),
                new UsageWindowState(12, UsageWindowStatus.Ok, null),
                ApiRetrievedAt: Now.AddSeconds(-4),
                IsRateLimited: false,
                Usage: null),
            new UsageHistorySnapshot(
                [new RecentUsageDay(new DateOnly(2026, 8, 20), 198_402, 2.94m)],
                TotalTokens: 198_402,
                DatabaseLastWriteTime: Now.AddMinutes(-1)));
}
