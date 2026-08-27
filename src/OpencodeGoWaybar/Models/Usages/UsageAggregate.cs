namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// What the knot ties together: both halves of the usage picture, in domain terms.
/// Each exposure surface maps this to its own shape.
/// </summary>
internal sealed record UsageAggregate(
    UsageWindowSnapshot Windows,
    UsageHistorySnapshot History);
