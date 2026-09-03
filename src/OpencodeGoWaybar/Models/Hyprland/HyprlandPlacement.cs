namespace OpencodeGoWaybar.Models.Hyprland;

/// <summary>
/// Everything about the compositor that the visibility rule reads: which
/// workspace is focused, and where every window sits.
///
/// This exists to be compared rather than rendered. The module cannot ask
/// Hyprland which of its events matter — the socket protocol publishes no list
/// of event names, and the names are not stable across versions — so instead of
/// classifying what happened, it re-reads what is and asks whether the answer
/// moved. An event nobody anticipated cannot be missed, because nothing is
/// anticipating events.
/// </summary>
internal sealed record HyprlandPlacement(int? ActiveWorkspaceId, IReadOnlyList<HyprlandWindow> Windows)
{
    public static HyprlandPlacement None { get; } = new(null, []);

    /// <summary>
    /// Whether this describes the same arrangement as <paramref name="other"/>.
    ///
    /// Compared as a set, for two reasons. Hyprland promises no order for its
    /// window list, so order changes are noise. And duplicates carry nothing the
    /// rule can use: two windows of one process on one workspace answer "is that
    /// process on that workspace" exactly as one does, so collapsing them cannot
    /// hide a change that matters.
    /// </summary>
    public bool Matches(HyprlandPlacement other) =>
        ActiveWorkspaceId == other.ActiveWorkspaceId
        && Windows.ToHashSet().SetEquals(other.Windows);
}
