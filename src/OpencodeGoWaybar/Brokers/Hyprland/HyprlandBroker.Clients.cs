using System.Text.Json;
using OpencodeGoWaybar.Models.Hyprland;

namespace OpencodeGoWaybar.Brokers.Hyprland;

internal sealed partial class HyprlandBroker
{
    private const string ClientsCommand = "j/clients";

    public async ValueTask<IReadOnlyList<HyprlandClient>?> GetClientsAsync(CancellationToken cancellationToken)
    {
        var payload = await RequestAsync(ClientsCommand, cancellationToken);

        return payload is null
            ? null
            : JsonSerializer.Deserialize(payload, HyprlandJsonContext.Default.HyprlandClientArray);
    }
}
