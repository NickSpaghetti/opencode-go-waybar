using System.Text.RegularExpressions;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.AcceptanceTests;

/// <summary>
/// The usage vertical, end to end against the live API. Every other acceptance
/// test reaches "visible" through the error branch, because without credentials
/// the usage call fails — these are the only ones that prove the success path.
///
/// Carries no Layer trait, so the ordinary layered run never picks it up; it
/// needs a live key and runs only via `make acceptance-usage`.
/// </summary>
[Trait("Tier", "Acceptance")]
[Trait("Requires", "ApiKey")]
public sealed class UsageVerticalTests(ITestOutputHelper output)
{
    private static readonly Regex UsagePercent = new(@"^Go · \d{1,3}%$", RegexOptions.Compiled);

    [Fact]
    public async Task ShouldReportRealUsageWhenCredentialsAreConfiguredAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;

        // then
        Assert.True(
            E2eEnvironment.HasApiKey,
            "OPENCODE_GO_API_KEY did not reach the container; run this through `make acceptance-usage`.");

        using var agent = await OpenCodeAcpAgent.StartAsync(
            E2eEnvironment.ScriptInstalledOpenCode, cancellationToken);

        var payload = await WaybarModule.RunAsync(cancellationToken);
        output.WriteLine(payload.Json);

        Assert.True(payload.Visible);

        Assert.True(
            payload.Class is "opencode-go" or "opencode-go-rate-limited",
            $"Expected a usage class, got '{payload.Class}'. The usage call failed rather than succeeded. " +
            $"Payload: {payload.Json}");

        Assert.Matches(UsagePercent, payload.Text);
        Assert.Contains("Weekly:", payload.Tooltip, StringComparison.Ordinal);
    }
}
