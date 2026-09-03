namespace OpencodeGoWaybar.Models.Hyprland;

/// <summary>
/// A window placed on a workspace, reduced to the pair the visibility rule
/// compares. This is the foundation service's own contract: the broker's
/// <see cref="HyprlandClient"/> carries a nullable workspace because the IPC
/// payload may omit one, and callers should not have to keep re-asking that
/// question.
/// </summary>
internal sealed record HyprlandWindow(int ProcessId, int WorkspaceId);
