using OpencodeGoWaybar.Brokers.Apis.Usage;

namespace OpencodeGoWaybar.Models.Processings.Usage;

internal sealed class UsageCacheState
{
    public UsageResponse? Usage { get; set; }
    public IReadOnlyList<RecentUsageDay> RecentDays { get; set; } = Array.Empty<RecentUsageDay>();
    public DateTimeOffset ApiRetrievedAt { get; set; }
    public DateTimeOffset? DatabaseLastWriteTime { get; set; }
}

internal sealed record RecentUsageDay(DateOnly Date, long Tokens, decimal Cost);

internal sealed record UsageSnapshot(
    UsageResponse? Usage,
    IReadOnlyList<RecentUsageDay> RecentDays,
    DateTimeOffset ApiRetrievedAt,
    DateTimeOffset? DatabaseLastWriteTime);
