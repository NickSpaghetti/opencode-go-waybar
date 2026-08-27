using System.Diagnostics;
using System.Text.Json.Nodes;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.AcceptanceTests;

/// <summary>
/// Layer 2 — a real third-party ACP client. Neovim with CodeCompanion.nvim is
/// the only client from the opencode ACP docs that runs headlessly; Zed and
/// JetBrains are GUI applications.
/// </summary>
[Trait("Tier", "Acceptance")]
[Trait("Layer", "2")]
public sealed class NeovimLayer2Tests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShouldSeeTheAgentNeovimSpawnsOverAcpAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;

        // then
        Assert.Empty(OpenCodeAcpAgent.RunningOpenCodeProcessIds());

        try
        {
            await RunSessionAsync(cancellationToken);
        }
        finally
        {
            // The agent CodeCompanion spawned is not a child Neovim reaps.
            OpenCodeAcpAgent.KillAllRunning();
        }
    }

    private async Task RunSessionAsync(CancellationToken cancellationToken)
    {
        using var neovim = StartNeovim();

        var standardOutput = neovim.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = neovim.StandardError.ReadToEndAsync(cancellationToken);

        // The plugin spawns the agent lazily once the chat opens.
        var agentIds = await WaitForOpenCodeAsync(cancellationToken);

        output.WriteLine($"opencode pids while Neovim held the session: [{string.Join(", ", agentIds)}]");

        Assert.NotEmpty(agentIds);

        var payload = await WaybarModule.RunAsync(cancellationToken);
        output.WriteLine(payload.Json);

        Assert.True(
            payload.Visible,
            $"The module stayed hidden while Neovim held an ACP session. Payload: {payload.Json}");

        await neovim.WaitForExitAsync(cancellationToken);

        var stdout = await standardOutput;
        var stderr = await standardError;
        output.WriteLine($"--- neovim stdout ---{Environment.NewLine}{stdout}");
        output.WriteLine($"--- neovim stderr ---{Environment.NewLine}{stderr}");

        // Headless Neovim writes `print()` to stderr, so search both streams.
        var reportLine = $"{stdout}\n{stderr}"
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault(line => line.StartsWith('{') && line.Contains("\"spawned\""));

        Assert.NotNull(reportLine);
        Assert.True(JsonNode.Parse(reportLine)!["spawned"]!.GetValue<bool>());
    }

    private static Process StartNeovim()
    {
        var startInfo = new ProcessStartInfo("nvim")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = E2eEnvironment.Workspace,
        };

        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("-u");
        startInfo.ArgumentList.Add(E2eEnvironment.NeovimInit);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("lua E2eStartAcp()");

        startInfo.Environment["OPENCODE_BIN"] = E2eEnvironment.ScriptInstalledOpenCode;
        startInfo.Environment["E2E_HOLD_MS"] = "20000";

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Neovim.");
    }

    private static async Task<IReadOnlyList<int>> WaitForOpenCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 120; attempt++)
        {
            var ids = OpenCodeAcpAgent.RunningOpenCodeProcessIds();

            if (ids.Count > 0)
            {
                return ids;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return [];
    }
}
