using System.Text.Json.Serialization;

namespace OpencodeGoWaybar.Models.Usages;

[JsonSerializable(typeof(UsageWindowCacheState))]
[JsonSerializable(typeof(UsageHistoryCacheState))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class UsageCacheJsonContext : JsonSerializerContext
{
}
