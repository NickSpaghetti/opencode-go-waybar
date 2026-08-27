using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Models.Usages.Exposures;

/// <summary>
/// The whole usage picture for one refresh — the contract a detail window binds.
///
/// A failure is reported rather than thrown: a window that is already open needs
/// something to show, which is the same reason the Waybar exposer renders its
/// failures into a payload instead of letting them escape.
/// </summary>
public sealed record UsageView(
    bool ProcessIsActive,
    UsageWindowView Rolling,
    UsageWindowView Weekly,
    UsageWindowView Monthly,
    IReadOnlyList<RecentUsageDay> RecentDays,
    long RecentTokens,
    bool IsRateLimited,
    DateTimeOffset? ApiRetrievedAt,
    DateTimeOffset? DatabaseLastWriteTime,
    string? FailureMessage);
