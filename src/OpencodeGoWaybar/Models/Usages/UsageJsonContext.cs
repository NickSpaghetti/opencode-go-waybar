using System.Text.Json.Serialization;

namespace OpencodeGoWaybar.Models.Usages;

[JsonSerializable(typeof(UsageResponse))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class UsageJsonContext : JsonSerializerContext
{
}
