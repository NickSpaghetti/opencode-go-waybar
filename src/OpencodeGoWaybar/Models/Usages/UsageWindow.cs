namespace OpencodeGoWaybar.Models.Usages;

/// <summary>One allowance window: how much is spent, and whether it is healthy.</summary>
internal sealed record UsageWindow(
    string Status,
    int? Percent,
    DateTimeOffset? ResetsAt,
    decimal? LimitDollars = null);
