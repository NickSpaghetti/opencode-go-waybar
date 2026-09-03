namespace OpencodeGoWaybar.Models.Hyprland;

/// <summary>
/// A Hyprland workspace. The identifier is what the module compares on: names
/// are user-facing and can be renamed, and special workspaces carry negative
/// identifiers, which compare correctly without being special-cased.
/// </summary>
internal sealed record HyprlandWorkspace(int Id, string? Name = null);
