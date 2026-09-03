using System.Diagnostics.CodeAnalysis;
using OpencodeGoWaybar.Models.Hyprland;
using OpencodeGoWaybar.Models.Hyprland.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Hyprland;

internal sealed partial class HyprlandService
{
    /// <summary>
    /// A present compositor that answers with nothing is a fault, unlike an absent
    /// one — the socket was there and the reply could not be read.
    /// </summary>
    private static void ValidateClients([NotNull] IReadOnlyList<HyprlandClient>? clients)
    {
        if (clients is null)
        {
            throw new HyprlandResponseException(
                new InvalidDataException("Hyprland returned no window list."));
        }
    }
}
