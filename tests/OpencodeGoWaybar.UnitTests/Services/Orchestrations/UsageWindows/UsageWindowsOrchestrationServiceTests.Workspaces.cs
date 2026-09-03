using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Hyprland;
using OpencodeGoWaybar.Models.Hyprland.Exceptions;
using OpencodeGoWaybar.Models.Processes;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Foundations.Hyprland;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.Services.Foundations.Usage;
using OpencodeGoWaybar.Services.Foundations.UsageWindowCache;
using OpencodeGoWaybar.Services.Orchestrations.UsageWindows;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Orchestrations.UsageWindows;

/// <summary>
/// The workspace filter. Every case is a process lineage plus a window layout,
/// because that pair is the rule's only input: OpenCode owns no window of its
/// own — it is a terminal program, or a child of the editor driving it over ACP —
/// so the session is placed by whichever ancestor does own one.
///
/// The cases that stay visible carry the weight here. Hiding is the one outcome
/// that can cost the user a warning they needed, so each way the rule could fail
/// to place a session has a test holding it open.
/// </summary>
public sealed partial class UsageWindowsOrchestrationServiceTests
{
    private const int ActiveWorkspace = 3;
    private const int OtherWorkspace = 7;
    private const int OpenCodeProcessId = 100;
    private const int TerminalProcessId = 200;
    private const int EditorProcessId = 300;

    [Fact]
    public async Task ShouldHideWhenTheSessionSitsOnAnotherWorkspaceAsync()
    {
        // given
        var cacheService = Substitute.For<IUsageWindowCacheService>();
        var usageService = Substitute.For<IUsageService>();

        // when
        UsageWindowSnapshot snapshot = await CreateWorkspaceService(
            lineage: [OpenCodeProcessId, TerminalProcessId],
            windows: [new HyprlandWindow(TerminalProcessId, OtherWorkspace)],
            cacheService: cacheService,
            usageService: usageService)
            .RetrieveWindowsAsync(Now, CancellationToken.None);

        // then a session you cannot see is not worth an API call either
        Assert.False(snapshot.ProcessIsActive);
        await cacheService.DidNotReceive().RetrieveStateAsync(Arg.Any<CancellationToken>());
        await usageService.DidNotReceive().RetrieveUsageAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldShowWhenTheSessionSitsOnTheActiveWorkspaceAsync()
    {
        // given
        UsageWindowsOrchestrationService orchestrationService = CreateWorkspaceService(
            lineage: [OpenCodeProcessId, TerminalProcessId],
            windows: [new HyprlandWindow(TerminalProcessId, ActiveWorkspace)]);

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.True(snapshot.ProcessIsActive);
    }

    /// <summary>
    /// An editor driving OpenCode over ACP: the session is a grandchild of the
    /// window rather than its child, so the walk has to keep climbing to place it.
    /// </summary>
    [Fact]
    public async Task ShouldPlaceASessionSeveralProcessesBelowItsWindowAsync()
    {
        // given
        UsageWindowsOrchestrationService orchestrationService = CreateWorkspaceService(
            lineage: [OpenCodeProcessId, 150, EditorProcessId],
            windows: [new HyprlandWindow(EditorProcessId, OtherWorkspace)]);

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.False(snapshot.ProcessIsActive);
    }

    /// <summary>
    /// The nearest window wins. A terminal launched from an editor leaves both in
    /// the lineage, and the session is displayed in the terminal — the editor's
    /// window being focused does not put this session on screen.
    /// </summary>
    [Fact]
    public async Task ShouldPlaceTheSessionInItsNearestWindowRatherThanAFurtherOneAsync()
    {
        // given
        UsageWindowsOrchestrationService orchestrationService = CreateWorkspaceService(
            lineage: [OpenCodeProcessId, TerminalProcessId, EditorProcessId],
            windows:
            [
                new HyprlandWindow(TerminalProcessId, OtherWorkspace),
                new HyprlandWindow(EditorProcessId, ActiveWorkspace),
            ]);

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.False(snapshot.ProcessIsActive);
    }

    /// <summary>
    /// A terminal that draws every window from a single process reports the same
    /// pid on each of them. The compositor cannot say which window holds the
    /// session, so the rule must not guess that it is the hidden one.
    /// </summary>
    [Fact]
    public async Task ShouldShowWhenTheOwningProcessDrawsAWindowOnTheActiveWorkspaceAsync()
    {
        // given
        UsageWindowsOrchestrationService orchestrationService = CreateWorkspaceService(
            lineage: [OpenCodeProcessId, TerminalProcessId],
            windows:
            [
                new HyprlandWindow(TerminalProcessId, OtherWorkspace),
                new HyprlandWindow(TerminalProcessId, ActiveWorkspace),
            ]);

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.True(snapshot.ProcessIsActive);
    }

    [Fact]
    public async Task ShouldShowWhenOneOfSeveralSessionsIsOnTheActiveWorkspaceAsync()
    {
        // given two sessions, only the second of them on screen
        IProcessService processService = CreateLineageProcessService(
            new OpenCodeProcessLineage(OpenCodeProcessId, [OpenCodeProcessId, TerminalProcessId]),
            new OpenCodeProcessLineage(101, [101, EditorProcessId]));

        UsageWindowsOrchestrationService orchestrationService = CreateService(
            processService,
            Substitute.For<IUsageWindowCacheService>(),
            Substitute.For<IUsageService>(),
            hyprlandService: CreateHyprlandService(
                ActiveWorkspace,
                [
                    new HyprlandWindow(TerminalProcessId, OtherWorkspace),
                    new HyprlandWindow(EditorProcessId, ActiveWorkspace),
                ]));

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.True(snapshot.ProcessIsActive);
    }

    /// <summary>A session no window owns — detached, or started by a service.</summary>
    [Fact]
    public async Task ShouldShowWhenNoWindowOwnsTheSessionAsync()
    {
        // given
        UsageWindowsOrchestrationService orchestrationService = CreateWorkspaceService(
            lineage: [OpenCodeProcessId, TerminalProcessId],
            windows: [new HyprlandWindow(EditorProcessId, OtherWorkspace)]);

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.True(snapshot.ProcessIsActive);
    }

    /// <summary>Any compositor that is not Hyprland reports no active workspace.</summary>
    [Fact]
    public async Task ShouldShowWhenThereIsNoActiveWorkspaceAsync()
    {
        // given
        UsageWindowsOrchestrationService orchestrationService = CreateService(
            CreateLineageProcessService(
                new OpenCodeProcessLineage(OpenCodeProcessId, [OpenCodeProcessId, TerminalProcessId])),
            Substitute.For<IUsageWindowCacheService>(),
            Substitute.For<IUsageService>(),
            hyprlandService: CreateHyprlandService(activeWorkspaceId: null, windows: []));

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.True(snapshot.ProcessIsActive);
    }

    [Fact]
    public async Task ShouldShowWhenHyprlandCannotBeReachedAsync()
    {
        // given
        var hyprlandService = Substitute.For<IHyprlandService>();

        hyprlandService.RetrieveActiveWorkspaceIdAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HyprlandUnavailableException(new IOException("socket closed")));

        UsageWindowsOrchestrationService orchestrationService = CreateService(
            CreateLineageProcessService(
                new OpenCodeProcessLineage(OpenCodeProcessId, [OpenCodeProcessId, TerminalProcessId])),
            Substitute.For<IUsageWindowCacheService>(),
            Substitute.For<IUsageService>(),
            hyprlandService: hyprlandService);

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then a compositor that will not answer stops the filtering, it does not
        // fail the poll
        Assert.True(snapshot.ProcessIsActive);
    }

    [Fact]
    public async Task ShouldNotConsultHyprlandWhenTheFilterIsOffAsync()
    {
        // given
        IHyprlandService hyprlandService = CreateHyprlandService(
            ActiveWorkspace,
            [new HyprlandWindow(TerminalProcessId, OtherWorkspace)]);

        UsageWindowsOrchestrationService orchestrationService = CreateFilteredService(
            CreateLineageProcessService(
                new OpenCodeProcessLineage(OpenCodeProcessId, [OpenCodeProcessId, TerminalProcessId])),
            hyprlandService,
            new OpenCodeGoOptions { ActiveWorkspaceOnly = false });

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.True(snapshot.ProcessIsActive);
        await hyprlandService.DidNotReceive().RetrieveActiveWorkspaceIdAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The override forces the process answer for container and acceptance runs,
    /// where there is neither a session nor a compositor to place it on. The
    /// filter must not overturn it.
    /// </summary>
    [Fact]
    public async Task ShouldNotFilterWhenTheProcessAnswerIsOverriddenAsync()
    {
        // given
        IHyprlandService hyprlandService = CreateHyprlandService(
            ActiveWorkspace,
            [new HyprlandWindow(TerminalProcessId, OtherWorkspace)]);

        UsageWindowsOrchestrationService orchestrationService = CreateFilteredService(
            CreateLineageProcessService(
                new OpenCodeProcessLineage(OpenCodeProcessId, [OpenCodeProcessId, TerminalProcessId])),
            hyprlandService,
            new OpenCodeGoOptions { ProcessPresentOverride = true });

        // when
        UsageWindowSnapshot snapshot =
            await orchestrationService.RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.True(snapshot.ProcessIsActive);
        await hyprlandService.DidNotReceive().RetrieveActiveWorkspaceIdAsync(Arg.Any<CancellationToken>());
    }

    private UsageWindowsOrchestrationService CreateWorkspaceService(
        int[] lineage,
        HyprlandWindow[] windows,
        IUsageWindowCacheService? cacheService = null,
        IUsageService? usageService = null) =>
        CreateService(
            CreateLineageProcessService(new OpenCodeProcessLineage(lineage[0], lineage)),
            cacheService ?? Substitute.For<IUsageWindowCacheService>(),
            usageService ?? Substitute.For<IUsageService>(),
            hyprlandService: CreateHyprlandService(ActiveWorkspace, windows));

    private static UsageWindowsOrchestrationService CreateFilteredService(
        IProcessService processService,
        IHyprlandService hyprlandService,
        OpenCodeGoOptions options) =>
        new(
            processService,
            hyprlandService,
            Substitute.For<IUsageWindowCacheService>(),
            Substitute.For<IUsageService>(),
            options);

    private static IProcessService CreateLineageProcessService(params OpenCodeProcessLineage[] lineages)
    {
        var processService = Substitute.For<IProcessService>();

        processService.IsOpenCodeRunningAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(true));

        processService.RetrieveOpenCodeLineagesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<OpenCodeProcessLineage>>(lineages));

        return processService;
    }
}
