using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Models.Usages.Exposures;

/// <summary>
/// One usage window as a consumer reads it. Carries facts only — how to phrase a
/// countdown or which colour to paint is the consumer's decision.
/// </summary>
public sealed record UsageWindowView(
    string Label,
    int? Percent,
    UsageWindowStatus Status,
    DateTimeOffset? ResetsAt);
