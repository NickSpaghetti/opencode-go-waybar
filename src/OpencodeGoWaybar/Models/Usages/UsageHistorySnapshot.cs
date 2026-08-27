namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// Recent daily usage, already totalled. The sum lives here because an exposer may
/// not iterate (§3.0.0.0) and the bar's tooltip needs it.
/// </summary>
internal sealed record UsageHistorySnapshot(
    IReadOnlyList<RecentUsageDay> RecordedDays,
    long TotalTokens,
    DateTimeOffset? DatabaseLastWriteTime);
