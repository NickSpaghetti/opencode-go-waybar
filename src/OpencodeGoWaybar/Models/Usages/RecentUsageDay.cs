namespace OpencodeGoWaybar.Models.Usages;

/// <summary>One day's spend, aggregated from opencode's message table.</summary>
public sealed record RecentUsageDay(DateOnly Date, long Tokens, decimal Cost);
