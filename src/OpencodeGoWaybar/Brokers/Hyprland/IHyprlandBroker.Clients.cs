using OpencodeGoWaybar.Models.Hyprland;

namespace OpencodeGoWaybar.Brokers.Hyprland;

internal partial interface IHyprlandBroker
{
    /// <summary>Retrieves every toplevel window Hyprland knows, or null when Hyprland is absent.</summary>
    ValueTask<IReadOnlyList<HyprlandClient>?> GetClientsAsync(CancellationToken cancellationToken);
}
