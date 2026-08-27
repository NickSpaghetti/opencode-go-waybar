namespace OpencodeGoWaybar.Models.Usages;

/// <summary>The three allowance windows OpenCode Go reports.</summary>
internal sealed record Usage(
    UsageWindow Rolling,
    UsageWindow Weekly,
    UsageWindow Monthly);
