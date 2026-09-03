using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Processes;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Processes;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.IntegrationTests;

/// <summary>
/// The real broker reading real procfs, wired to the real service, against a real
/// OpenCode agent. This is the only tier that can prove the lineage walk: a native
/// <see cref="Process"/> cannot be fabricated with a chosen name, and the parentage
/// the walk climbs is written by the kernel rather than by a test.
///
/// The workspace filter rests entirely on this walk. If the lineage stops short,
/// the filter cannot find the window that owns a session and the module stays
/// visible everywhere; if it climbs to the wrong process, the module hides on the
/// workspace the user is actually looking at.
/// </summary>
[Trait("Tier", "Integration")]
public sealed class ProcessLineageIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShouldReportNoLineagesWhenNoAgentIsPresentAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();

        // then
        Assert.Empty(OpenCodeAcpAgent.RunningOpenCodeProcessIds());

        Assert.Empty(await CreateService().RetrieveOpenCodeLineagesAsync(timeout.Token));
    }

    [Theory]
    [InlineData("install-script")]
    [InlineData("npm")]
    public async Task ShouldClimbFromTheAgentToTheProcessThatSpawnedItAsync(string installMethod)
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;
        var service = CreateService();

        using var testProcess = Process.GetCurrentProcess();

        using var agent = await OpenCodeAcpAgent.StartAsync(BinaryFor(installMethod), cancellationToken);

        // when
        IReadOnlyList<OpenCodeProcessLineage> lineages =
            await service.RetrieveOpenCodeLineagesAsync(cancellationToken);

        // then
        OpenCodeProcessLineage lineage = Assert.Single(
            lineages,
            candidate => candidate.ProcessId == agent.Id);

        output.WriteLine($"{installMethod}: {string.Join(" -> ", lineage.LineageProcessIds)}");

        // The session itself comes first: a caller matching windows walks this
        // list in order and must not be handed an ancestor before the process.
        Assert.Equal(agent.Id, lineage.LineageProcessIds[0]);

        // This test spawned the agent, so the walk has to reach this process —
        // the stand-in for the terminal or editor that owns the window.
        Assert.Contains(testProcess.Id, lineage.LineageProcessIds);

        // Nearest first: the spawning process is reached after the agent, never before.
        Assert.True(
            lineage.LineageProcessIds.ToList().IndexOf(testProcess.Id) > 0,
            "The lineage must be ordered from the session outwards.");

        Assert.Equal(lineage.LineageProcessIds.Distinct().Count(), lineage.LineageProcessIds.Count);
    }

    [Fact]
    public async Task ShouldReportParentageForTheRunningProcessAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();
        using var currentProcess = Process.GetCurrentProcess();

        // when
        var parentProcessIds = await new ProcessBroker().GetParentProcessIdsAsync(timeout.Token);

        // then
        Assert.NotEmpty(parentProcessIds);

        Assert.True(
            parentProcessIds.ContainsKey(currentProcess.Id),
            "procfs must report a parent for the process doing the reading.");
    }

    private static ProcessService CreateService() =>
        new(
            new ProcessBroker(),
            new LoggingBroker(NullLogger<LoggingBroker>.Instance),
            new OpenCodeGoOptions());

    private static string BinaryFor(string installMethod) => installMethod switch
    {
        "install-script" => E2eEnvironment.ScriptInstalledOpenCode,
        "npm" => E2eEnvironment.NpmInstalledOpenCode,
        _ => throw new ArgumentOutOfRangeException(nameof(installMethod), installMethod, null),
    };
}
