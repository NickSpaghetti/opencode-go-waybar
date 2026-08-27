namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// The allowance windows with their health already decided. Classification lands
/// here rather than at the exposure surface so both exposers read the same verdict
/// and neither has to make it (§3.0.0.0).
/// </summary>
internal sealed record UsageWindowSnapshot(
    bool ProcessIsActive,
    UsageWindowState Rolling,
    UsageWindowState Weekly,
    UsageWindowState Monthly,
    DateTimeOffset? ApiRetrievedAt,
    bool IsRateLimited,
    UsageResponse? Usage);
