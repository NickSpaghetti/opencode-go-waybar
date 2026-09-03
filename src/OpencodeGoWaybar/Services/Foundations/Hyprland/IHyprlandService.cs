using OpencodeGoWaybar.Models.Hyprland;

namespace OpencodeGoWaybar.Services.Foundations.Hyprland;

internal interface IHyprlandService
{
    /// <summary>
    /// The workspace currently focused, or null when this session is not running
    /// under Hyprland. Null is an answer, not a failure: a machine on another
    /// compositor has no active workspace to report.
    /// </summary>
    ValueTask<int?> RetrieveActiveWorkspaceIdAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Every toplevel window Hyprland has placed on a workspace. Empty when this
    /// session is not running under Hyprland.
    /// </summary>
    ValueTask<IReadOnlyList<HyprlandWindow>> RetrieveWindowsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The focused workspace and the window layout together, as one value that
    /// can be compared against a previous reading to decide whether anything the
    /// module cares about actually moved.
    /// </summary>
    ValueTask<HyprlandPlacement> RetrievePlacementAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The name of each event the compositor reports, unfiltered, ending when the
    /// stream closes or immediately when Hyprland is absent.
    ///
    /// Unfiltered on purpose. Hyprland publishes no way to enumerate its event
    /// names, so any list of the ones worth reacting to would be a guess frozen
    /// at the time of writing, quietly wrong the moment an event is renamed or
    /// added. Deciding what an event means is the caller's problem, and the
    /// caller is expected to answer it by re-reading state rather than by
    /// recognising names.
    /// </summary>
    IAsyncEnumerable<string> StreamEventsAsync(CancellationToken cancellationToken);
}
