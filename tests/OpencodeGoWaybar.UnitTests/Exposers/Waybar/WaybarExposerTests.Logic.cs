using OpencodeGoWaybar.Models.Usages;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Exposers.Waybar;

public sealed partial class WaybarExposerTests
{
    [Fact]
    public async Task ShouldEmitHiddenPayloadWhenOpenCodeIsNotRunningAsync()
    {
        // given
        var aggregationService = CreateAggregationService(
            new WaybarStatus(ProcessIsActive: false, Usage: null));

        // when
        var payload = Parse(await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None));

        // then
        Assert.False(payload.GetProperty("visible").GetBoolean());
        Assert.Equal("hidden", payload.GetProperty("class").GetString());
    }

    [Fact]
    public async Task ShouldEmitWeeklyUsageAndTooltipAsync()
    {
        // given
        var aggregationService = CreateAggregationService(CreateRunningStatus(CreateUsage()));

        // when
        var payload = Parse(await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("Go · 42%", payload.GetProperty("text").GetString());
        Assert.Contains("Weekly: 42%", payload.GetProperty("tooltip").GetString(), StringComparison.Ordinal);
        Assert.Equal("opencode-go", payload.GetProperty("class").GetString());
    }

    [Fact]
    public async Task ShouldDistinguishARateLimitedWeekFromAMerelySpentOneAsync()
    {
        // given
        var usage = CreateUsage(
            rolling: CreateWindow("ok", 0),
            weekly: CreateWindow("rate-limited", 100, new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero)),
            monthly: CreateWindow("ok", 97));

        // when
        var payload = Parse(await CreateExposer(CreateAggregationService(CreateRunningStatus(usage, isRateLimited: true)))
            .ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("opencode-go-rate-limited", payload.GetProperty("class").GetString());
        Assert.Equal("Go · 100%", payload.GetProperty("text").GetString());

        var tooltip = payload.GetProperty("tooltip").GetString()!;
        Assert.Contains("Weekly: 100% — rate-limited", tooltip, StringComparison.Ordinal);
        Assert.DoesNotContain("Weekly: rate-limited", tooltip, StringComparison.Ordinal);
        Assert.Contains("2026-08-24", tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldNotReportARateLimitWhenAWeekIsMerelyFullySpentAsync()
    {
        // given
        var usage = CreateUsage(weekly: CreateWindow("ok", 100));

        // when
        var payload = Parse(await CreateExposer(CreateAggregationService(CreateRunningStatus(usage)))
            .ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("opencode-go", payload.GetProperty("class").GetString());
    }

    [Fact]
    public async Task ShouldReportAThrottledRollingWindowEvenThoughTheBarShowsTheWeekAsync()
    {
        // given
        var usage = CreateUsage(
            rolling: CreateWindow("rate-limited", 100),
            weekly: CreateWindow("ok", 12),
            monthly: CreateWindow("ok", 30));

        // when
        var payload = Parse(await CreateExposer(CreateAggregationService(CreateRunningStatus(usage, isRateLimited: true)))
            .ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("opencode-go-rate-limited", payload.GetProperty("class").GetString());
        Assert.Equal("Go · 12%", payload.GetProperty("text").GetString());
        Assert.Contains("Rolling: rate-limited", payload.GetProperty("tooltip").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldTreatTheContractsHttp429SpellingAsRateLimitedAsync()
    {
        // given
        var usage = CreateUsage(weekly: CreateWindow("HTTP 429", 100));

        // when
        var payload = Parse(await CreateExposer(CreateAggregationService(CreateRunningStatus(usage, isRateLimited: true)))
            .ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("opencode-go-rate-limited", payload.GetProperty("class").GetString());
    }
}
