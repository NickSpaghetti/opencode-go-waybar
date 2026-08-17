using System.Text.Json.Serialization;
using System.Net;

namespace OpencodeGoWaybar.Brokers.Apis.Usage;

public sealed record UsageResponse(Usage Usage);

internal sealed record UsageApiBrokerResponse(HttpStatusCode StatusCode, string Body);

public sealed record Usage(
    UsageWindow Rolling,
    UsageWindow Weekly,
    UsageWindow Monthly);

public sealed record UsageWindow(
    string Status,
    int? Percent,
    DateTimeOffset? ResetsAt,
    decimal? LimitDollars = null);

[JsonSerializable(typeof(UsageResponse))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class UsageJsonContext : JsonSerializerContext
{
}
