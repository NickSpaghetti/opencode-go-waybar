using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exposures;
using OpencodeGoWaybar.Models.Usages.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Exposers.Usages;

public sealed partial class UsageExposerTests
{
    [Fact]
    public async Task ShouldExposeAFailureMessageWhenNoApiKeyIsConfiguredAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(
            new UsageCredentialsMissingException());

        // when
        UsageView view = await CreateExposer(aggregationService)
            .ExposeUsageAsync(CancellationToken.None);

        // then a window that is already open still has three labelled windows
        Assert.Equal("No OpenCode Go API key configured", view.FailureMessage);
        Assert.False(view.ProcessIsActive);
        Assert.Empty(view.RecentDays);
        Assert.Equal(UsageWindowStatus.Unknown, view.Weekly.Status);
        Assert.Equal("Weekly", view.Weekly.Label);
    }

    [Fact]
    public async Task ShouldExposeATimeoutMessageWhenTheRefreshIsCancelledAsync()
    {
        // given
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        var aggregationService = CreateFailingAggregationService(new OperationCanceledException());

        // when
        UsageView view = await CreateExposer(aggregationService)
            .ExposeUsageAsync(cancellationSource.Token);

        // then
        Assert.Equal("OpenCode Go usage refresh timed out", view.FailureMessage);
    }

    [Fact]
    public async Task ShouldExposeAGenericFailureMessageForAnUnexpectedErrorAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(new InvalidOperationException("boom"));

        // when
        UsageView view = await CreateExposer(aggregationService)
            .ExposeUsageAsync(CancellationToken.None);

        // then
        Assert.Equal("OpenCode Go usage unavailable", view.FailureMessage);
    }

    [Fact]
    public async Task ShouldNotLeakAnExceptionMessageIntoTheFailureMessageAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(
            new InvalidOperationException("Bearer sk-secret-token leaked here"));

        // when
        UsageView view = await CreateExposer(aggregationService)
            .ExposeUsageAsync(CancellationToken.None);

        // then
        Assert.DoesNotContain("sk-secret-token", view.FailureMessage!, StringComparison.Ordinal);
    }
}
