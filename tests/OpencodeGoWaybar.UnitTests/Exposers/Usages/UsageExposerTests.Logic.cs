using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exposures;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Exposers.Usages;

public sealed partial class UsageExposerTests
{
    [Fact]
    public async Task ShouldNameTheWindowsAndCarryTheClassificationThroughAsync()
    {
        // given
        UsageAggregate aggregate = CreateAggregate();

        // when
        UsageView view = await CreateExposer(CreateAggregationService(aggregate))
            .ExposeUsageAsync(CancellationToken.None);

        // then the labels are the exposer's contribution
        Assert.Equal("Rolling · 5h", view.Rolling.Label);
        Assert.Equal("Weekly", view.Weekly.Label);
        Assert.Equal("Monthly", view.Monthly.Label);

        // and the verdict is carried, not re-derived
        Assert.Equal(UsageWindowStatus.Throttled, view.Rolling.Status);
        Assert.Equal(61, view.Rolling.Percent);
        Assert.Equal(aggregate.Windows.Rolling.ResetsAt, view.Rolling.ResetsAt);
    }

    [Fact]
    public async Task ShouldHandOnTheRecordedDaysAndTotalUntouchedAsync()
    {
        // given
        UsageAggregate aggregate = CreateAggregate();

        // when
        UsageView view = await CreateExposer(CreateAggregationService(aggregate))
            .ExposeUsageAsync(CancellationToken.None);

        // then the days travel as recorded — there is no projection to get wrong
        Assert.Same(aggregate.History.RecordedDays, view.RecentDays);
        Assert.Equal(198_402, view.RecentTokens);
        Assert.Equal(aggregate.Windows.ApiRetrievedAt, view.ApiRetrievedAt);
        Assert.Equal(aggregate.History.DatabaseLastWriteTime, view.DatabaseLastWriteTime);
        Assert.True(view.ProcessIsActive);
        Assert.Null(view.FailureMessage);
    }
}
