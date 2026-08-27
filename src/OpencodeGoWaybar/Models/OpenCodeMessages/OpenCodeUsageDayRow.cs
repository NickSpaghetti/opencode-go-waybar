namespace OpencodeGoWaybar.Models.OpenCodeMessages;

/// <summary>
/// One day of usage as SQLite aggregates it: the date as an ISO string, and the
/// summed totals. Turning those into a domain model is the service's job.
/// </summary>
internal sealed record OpenCodeUsageDayRow(string Date, long Tokens, double Cost);
