using System.Text.Json.Serialization;

namespace OpencodeGoWaybar.Models.OpenCodeAuths;

[JsonSerializable(typeof(Dictionary<string, OpenCodeAuthEntry>), TypeInfoPropertyName = "AuthDocument")]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class OpenCodeAuthJsonContext : JsonSerializerContext
{
}
