using OpencodeGoWaybar.Models.Hyprland;

namespace OpencodeGoWaybar.Brokers.Hyprland;

internal partial interface IHyprlandBroker
{
    /// <summary>Retrieves the workspace currently focused, or null when Hyprland is absent.</summary>
    ValueTask<HyprlandWorkspace?> GetActiveWorkspaceAsync(CancellationToken cancellationToken);
}
