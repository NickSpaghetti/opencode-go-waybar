using System.Text.Json.Serialization;

namespace OpencodeGoWaybar.Models.Hyprland;

[JsonSerializable(typeof(HyprlandWorkspace))]
[JsonSerializable(typeof(HyprlandClient[]))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class HyprlandJsonContext : JsonSerializerContext
{
}
