namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// The unified view the orchestration service hands to the exposer, combining
/// the API's allowances with what the local database recorded.
/// </summary>
internal sealed record UsageSnapshot(
    UsageResponse? Usage,
    IReadOnlyList<RecentUsageDay> RecentDays,
    DateTimeOffset? ApiRetrievedAt,
    DateTimeOffset? DatabaseLastWriteTime);
