using OpencodeGoWaybar.Models.OpenCodeMessages;

namespace OpencodeGoWaybar.Brokers.Storages;

internal partial interface IOpenCodeDatabaseBroker
{
    /// <summary>
    /// Daily totals for one provider since the cutoff, summed by SQLite.
    ///
    /// The grouping lives in the query rather than in a service because the
    /// alternative is carrying every message row across this boundary to
    /// produce a handful of numbers — measured at 81ms versus 27ms for ten
    /// thousand rows. Which provider to total stays a caller's decision.
    /// </summary>
    ValueTask<IReadOnlyList<OpenCodeUsageDayRow>> SelectUsageDaysByCutoffAsync(
        DateTimeOffset cutoff,
        string providerId,
        CancellationToken cancellationToken);
}
