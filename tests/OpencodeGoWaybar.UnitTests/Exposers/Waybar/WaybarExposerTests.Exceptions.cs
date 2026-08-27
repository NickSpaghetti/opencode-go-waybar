using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Exposers.Waybar;

public sealed partial class WaybarExposerTests
{
    [Fact]
    public async Task ShouldEmitSafeErrorPayloadOnExposeIfTheAggregationFailsAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(
            new InvalidOperationException("secret-key-not-output"));

        // when
        var json = await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None);

        // then
        Assert.DoesNotContain("secret-key-not-output", json, StringComparison.Ordinal);
        Assert.Contains("error", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReportTheApiErrorOnExposeIfTheServiceReturnedOneAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(
            new UsageAuthenticationException(
                new InvalidOperationException("inner"),
                new UsageApiError("AuthError", "Unauthorized")));

        // when
        var payload = Parse(await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("OpenCode Go: Unauthorized (AuthError)", payload.GetProperty("tooltip").GetString());
        Assert.Equal("error", payload.GetProperty("class").GetString());
    }

    [Fact]
    public async Task ShouldReportTheFlatApiErrorShapeOnExposeAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(
            new UsageRateLimitedException(
                new InvalidOperationException("inner"),
                new UsageApiError("rate_limited", "Too many requests")));

        // when
        var payload = Parse(await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("OpenCode Go: Too many requests (rate_limited)", payload.GetProperty("tooltip").GetString());
    }

    [Fact]
    public async Task ShouldNameTheFailureOnExposeIfTheApiSaidNothingAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(new UsageCredentialsMissingException());

        // when
        var payload = Parse(await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("No OpenCode Go API key configured", payload.GetProperty("tooltip").GetString());
    }

    [Fact]
    public async Task ShouldFallBackToACategoryOnExposeIfTheBodyCarriedNoErrorAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(
            new UsageApiUnavailableException(new HttpRequestException("offline"), apiError: null));

        // when
        var payload = Parse(await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None));

        // then
        Assert.Equal("OpenCode Go could not be reached", payload.GetProperty("tooltip").GetString());
    }

    [Fact]
    public async Task ShouldCollapseAndTruncateAnOversizedApiMessageOnExposeAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(
            new UsageAuthenticationException(
                new InvalidOperationException("inner"),
                new UsageApiError(null, "line one\nline two " + new string('x', 300))));

        // when
        var payload = Parse(await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None));

        // then
        var tooltip = payload.GetProperty("tooltip").GetString()!;
        Assert.DoesNotContain('\n', tooltip);
        Assert.EndsWith("...", tooltip, StringComparison.Ordinal);
        Assert.True(tooltip.Length < 160, $"tooltip was {tooltip.Length} chars");
    }

    [Fact]
    public async Task ShouldNeverRenderTheExceptionsOwnMessageOnExposeAsync()
    {
        // given
        var aggregationService = CreateFailingAggregationService(
            new UsageAuthenticationException(
                new InvalidOperationException("Bearer sk-secret-value"),
                new UsageApiError("AuthError", "Unauthorized")));

        // when
        var json = await CreateExposer(aggregationService).ExposeAsync(CancellationToken.None);

        // then
        Assert.DoesNotContain("sk-secret-value", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShouldReportATimeoutOnExposeIfTheRefreshOutlivesTheBudgetAsync()
    {
        // given
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        var aggregationService = CreateFailingAggregationService(new OperationCanceledException());

        // when
        var payload = Parse(await CreateExposer(aggregationService).ExposeAsync(cancellationSource.Token));

        // then
        Assert.Equal("error", payload.GetProperty("class").GetString());
        Assert.Equal("OpenCode Go usage refresh timed out", payload.GetProperty("tooltip").GetString());
    }
}
