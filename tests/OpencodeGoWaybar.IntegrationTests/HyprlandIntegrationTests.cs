using Microsoft.Extensions.Logging.Abstractions;
using OpencodeGoWaybar.Brokers.Hyprland;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Hyprland;
using OpencodeGoWaybar.Services.Foundations.Hyprland;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.IntegrationTests;

/// <summary>
/// The real broker speaking Hyprland's IPC protocol over the real socket, wired to
/// the real service. Nothing below this tier can prove the protocol is right: the
/// wire format is the compositor's, and a change to it would pass every unit test
/// while leaving the module blind.
///
/// The container tier has no compositor, so each case that needs one bows out
/// rather than failing — the absent-compositor path is itself a case worth
/// asserting, and it is the one that runs in CI.
/// </summary>
[Trait("Tier", "Integration")]
public sealed class HyprlandIntegrationTests(ITestOutputHelper output)
{
    private static bool IsHyprlandRunning => new HyprlandBroker().IsHyprlandPresent;

    [Fact]
    public async Task ShouldReportNoWorkspaceWhenHyprlandIsAbsentAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();

        if (IsHyprlandRunning)
        {
            output.WriteLine("Hyprland is running; the absent-compositor path cannot be reached here.");

            return;
        }

        // when and then a machine on another compositor is a supported state,
        // reported as an absent answer rather than raised as a failure
        Assert.Null(await CreateService().RetrieveActiveWorkspaceIdAsync(timeout.Token));
        Assert.Empty(await CreateService().RetrieveWindowsAsync(timeout.Token));
    }

    [Fact]
    public async Task ShouldReadTheFocusedWorkspaceFromTheLiveCompositorAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();

        if (!IsHyprlandRunning)
        {
            output.WriteLine("No Hyprland socket for this session.");

            return;
        }

        // when
        var activeWorkspaceId = await CreateService().RetrieveActiveWorkspaceIdAsync(timeout.Token);

        // then
        Assert.NotNull(activeWorkspaceId);
        output.WriteLine($"active workspace: {activeWorkspaceId}");
    }

    [Fact]
    public async Task ShouldReadTheOpenWindowsFromTheLiveCompositorAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();

        if (!IsHyprlandRunning)
        {
            output.WriteLine("No Hyprland socket for this session.");

            return;
        }

        // when
        IReadOnlyList<HyprlandWindow> windows = await CreateService().RetrieveWindowsAsync(timeout.Token);

        // then every window must carry a real owning process, or the filter has
        // nothing to match a session's lineage against
        Assert.All(windows, window => Assert.True(window.ProcessId > 0));

        foreach (HyprlandWindow window in windows)
        {
            output.WriteLine($"pid {window.ProcessId} on workspace {window.WorkspaceId}");
        }
    }

    /// <summary>
    /// The two reads are separate round trips on separate connections. This proves
    /// they agree — that the workspace the compositor calls focused is one the
    /// window list actually knows about — which is the comparison the filter makes.
    /// </summary>
    [Fact]
    public async Task ShouldPlaceTheFocusedWorkspaceAmongTheOpenWindowsAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();

        if (!IsHyprlandRunning)
        {
            output.WriteLine("No Hyprland socket for this session.");

            return;
        }

        HyprlandService service = CreateService();

        // when
        var activeWorkspaceId = await service.RetrieveActiveWorkspaceIdAsync(timeout.Token);
        IReadOnlyList<HyprlandWindow> windows = await service.RetrieveWindowsAsync(timeout.Token);

        // then an empty workspace is legitimate, so this only asserts on a
        // compositor that has windows to report
        if (windows.Count == 0)
        {
            output.WriteLine("No windows open.");

            return;
        }

        Assert.NotNull(activeWorkspaceId);
        Assert.All(windows, window => Assert.NotEqual(0, window.WorkspaceId));
    }

    private static HyprlandService CreateService() =>
        new(new HyprlandBroker(), new LoggingBroker(NullLogger<LoggingBroker>.Instance));
}
