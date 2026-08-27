namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// One allowance window with its health already decided: domain scalars only, no
/// label and no view type. The exposure surface adds whatever naming it wants.
/// </summary>
internal sealed record UsageWindowState(
    int? Percent,
    UsageWindowStatus Status,
    DateTimeOffset? ResetsAt);
