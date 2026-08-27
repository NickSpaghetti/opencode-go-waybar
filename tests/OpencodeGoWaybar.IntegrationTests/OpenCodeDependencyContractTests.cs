using System.Diagnostics;
using System.Text.Json.Nodes;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.IntegrationTests;

/// <summary>
/// Facts about opencode itself that this module's design leans on. These assert
/// nothing about our code — when one fails, opencode changed, not us, which is
/// why they carry their own tier and are excluded from the default run.
/// </summary>
[Trait("Tier", "Dependency")]
public sealed class OpenCodeDependencyContractTests(ITestOutputHelper output)
{
    /// <summary>
    /// The module identifies opencode by process name. The npm package could
    /// plausibly ship a Node shim, which would surface as 'node' and break
    /// detection outright — this pins that both installs agree, in ACP mode.
    /// </summary>
    [Fact]
    public async Task ShouldRunAcpUnderTheSameProcessNameForBothInstallMethodsAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;

        using var scriptAgent = await OpenCodeAcpAgent.StartAsync(
            E2eEnvironment.ScriptInstalledOpenCode, cancellationToken);
        using var npmAgent = await OpenCodeAcpAgent.StartAsync(
            E2eEnvironment.NpmInstalledOpenCode, cancellationToken);

        output.WriteLine($"install-script → '{scriptAgent.ProcessName}' (pid {scriptAgent.Id})");
        output.WriteLine($"npm            → '{npmAgent.ProcessName}' (pid {npmAgent.Id})");

        // then
        Assert.Equal("opencode", scriptAgent.ProcessName);
        Assert.Equal("opencode", npmAgent.ProcessName);
    }

    /// <summary>
    /// `opencode acp` answers the ACP initialize handshake without credentials,
    /// which is what makes the whole containerised suite possible. Driven
    /// through tests/e2e/AcpClient.cs — the standalone client an engineer can
    /// also run by hand against any opencode build.
    /// </summary>
    [Fact]
    public async Task ShouldCompleteTheAcpInitializeHandshakeUnauthenticatedAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;

        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = E2eEnvironment.Workspace,
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add(E2eEnvironment.AcpClientSource);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--bin");
        startInfo.ArgumentList.Add(E2eEnvironment.ScriptInstalledOpenCode);

        using var client = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the synthetic ACP client.");

        var standardOutput = client.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = client.StandardError.ReadToEndAsync(cancellationToken);

        // when
        await client.WaitForExitAsync(cancellationToken);

        var stdout = await standardOutput;
        var stderr = await standardError;
        output.WriteLine(stdout);

        // then
        Assert.True(client.ExitCode == 0, $"AcpClient.cs exited {client.ExitCode}: {stderr}");

        var reportLine = stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Last(line => line.StartsWith('{'));

        var report = JsonNode.Parse(reportLine)!;

        Assert.True(report["pid"]!.GetValue<int>() > 0);
        Assert.Equal(1, report["initialize"]!["protocolVersion"]!.GetValue<int>());
        Assert.Equal("OpenCode", report["initialize"]!["agentInfo"]!["name"]!.GetValue<string>());
    }
}
