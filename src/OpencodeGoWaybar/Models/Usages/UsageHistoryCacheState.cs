namespace OpencodeGoWaybar.Models.Usages;

/// <summary>What the module remembers about recent daily usage between polls.</summary>
internal sealed class UsageHistoryCacheState
{
    public IReadOnlyList<RecentUsageDay> RecentDays { get; set; } = Array.Empty<RecentUsageDay>();

    public DateTimeOffset? DatabaseLastWriteTime { get; set; }
}
