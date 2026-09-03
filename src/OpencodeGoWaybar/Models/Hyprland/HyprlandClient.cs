namespace OpencodeGoWaybar.Models.Hyprland;

/// <summary>
/// One toplevel window as Hyprland reports it. Only the two facts this module
/// needs are modelled: which process owns the window, and which workspace the
/// window sits on. Everything else in the IPC payload is ignored.
/// </summary>
internal sealed record HyprlandClient(int Pid, HyprlandWorkspace? Workspace);
