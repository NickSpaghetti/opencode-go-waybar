using System.Text.Json;
using NSubstitute;
using OpencodeGoWaybar.Exposers.Waybar;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Aggregations.Usage;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Exposers.Waybar;

public sealed partial class WaybarExposerTests
{
    private static WaybarExposer CreateExposer(IUsageAggregationService aggregationService) =>
        new(aggregationService);

    private static IUsageAggregationService CreateAggregationService(WaybarStatus status)
    {
        var aggregationService = Substitute.For<IUsageAggregationService>();

        aggregationService.RetrieveStatusAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(status));

        return aggregationService;
    }

    private static IUsageAggregationService CreateFailingAggregationService(Exception exception)
    {
        var aggregationService = Substitute.For<IUsageAggregationService>();

        aggregationService.RetrieveStatusAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<WaybarStatus>>(_ => throw exception);

        return aggregationService;
    }

    /// <summary>
    /// isRateLimited is stated rather than derived: the exposer reads the verdict
    /// off the contract now instead of recomputing it, so deriving it here would
    /// put a second copy of the classifier in the tests. Whether the verdict is
    /// reached correctly is covered by UsageWindowsOrchestrationServiceTests.
    /// </summary>
    private static WaybarStatus CreateRunningStatus(
        UsageResponse usage,
        bool isRateLimited = false) =>
        new(ProcessIsActive: true, new UsageSnapshot(
            usage,
            Array.Empty<RecentUsageDay>(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow),
            isRateLimited);

    private static UsageResponse CreateUsage(
        UsageWindow? rolling = null,
        UsageWindow? weekly = null,
        UsageWindow? monthly = null) =>
        new(new Usage(
            rolling ?? CreateWindow("ok", 10),
            weekly ?? CreateWindow("ok", 42),
            monthly ?? CreateWindow("ok", 20)));

    private static UsageWindow CreateWindow(string status, int percent, DateTimeOffset? resetsAt = null) =>
        new(status, percent, resetsAt ?? DateTimeOffset.UtcNow);

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();
}
