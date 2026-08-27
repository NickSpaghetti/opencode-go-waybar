using System.Text.Json.Nodes;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.AcceptanceTests;

/// <summary>
/// Waybar reads this process's stdout and parses each line as JSON. Anything
/// else written there — a stack trace, a warning — reaches Waybar as a
/// malformed payload, so the stream separation is part of the contract.
/// </summary>
[Trait("Tier", "Acceptance")]
[Trait("Layer", "1")]
public sealed class StandardStreamTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShouldCarryNothingButThePayloadOnStdoutWhileFailingAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;

        using var agent = await OpenCodeAcpAgent.StartAsync(
            E2eEnvironment.ScriptInstalledOpenCode, cancellationToken);

        // Unset the key so the usage call fails and the module logs a stack
        // trace — the condition that would otherwise pollute stdout.
        // when
        var payload = await WaybarModule.RunAsync(
            new Dictionary<string, string> { ["OPENCODE_GO_API_KEY"] = string.Empty },
            cancellationToken);

        output.WriteLine($"--- stdout ---\n{payload.StandardOutput}");
        output.WriteLine($"--- stderr ---\n{payload.StandardError}");

        var stdoutLines = payload.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // then
        Assert.True(
            stdoutLines.Length == 1,
            $"stdout must carry exactly one line for Waybar to parse. Got {stdoutLines.Length}:\n{payload.StandardOutput}");

        Assert.NotNull(JsonNode.Parse(stdoutLines[0]));
        Assert.Equal("error", payload.Class);

        // The diagnostics still have to go somewhere.
        Assert.Contains("opencode-go-waybar operation failed", payload.StandardError, StringComparison.Ordinal);
    }
}
