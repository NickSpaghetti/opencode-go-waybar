namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// Everything the bar needs in one contract: whether OpenCode is running at all,
/// and the usage to show when it is.
/// </summary>
internal sealed record WaybarStatus(
    bool ProcessIsActive,
    UsageSnapshot? Usage,
    bool IsRateLimited = false);
