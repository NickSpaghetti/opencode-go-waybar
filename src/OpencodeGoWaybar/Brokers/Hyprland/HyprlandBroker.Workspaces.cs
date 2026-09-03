using System.Text.Json;
using OpencodeGoWaybar.Models.Hyprland;

namespace OpencodeGoWaybar.Brokers.Hyprland;

internal sealed partial class HyprlandBroker
{
    private const string ActiveWorkspaceCommand = "j/activeworkspace";

    public async ValueTask<HyprlandWorkspace?> GetActiveWorkspaceAsync(CancellationToken cancellationToken)
    {
        var payload = await RequestAsync(ActiveWorkspaceCommand, cancellationToken);

        return payload is null
            ? null
            : JsonSerializer.Deserialize(payload, HyprlandJsonContext.Default.HyprlandWorkspace);
    }
}
