namespace OpencodeGoWaybar.Brokers.Hyprland;

/// <summary>
/// Provides read access to the running Hyprland compositor over its IPC sockets.
///
/// Split by target (The Standard 1.2.5). The compositor exposes two sockets that
/// happen to share a directory: one answers questions and closes, the other only
/// reports what happened and is never asked anything. Behind the question socket
/// sit two separate collections, workspaces and clients, retrieved by different
/// commands and returning different shapes. Each of those is its own partial;
/// what stays here is what belongs to the compositor as a whole rather than to
/// any one of them.
/// </summary>
internal partial interface IHyprlandBroker
{
    /// <summary>
    /// Whether a Hyprland IPC socket exists for this session. False on a machine
    /// running any other compositor, which is a supported state rather than a fault.
    /// </summary>
    bool IsHyprlandPresent { get; }
}
