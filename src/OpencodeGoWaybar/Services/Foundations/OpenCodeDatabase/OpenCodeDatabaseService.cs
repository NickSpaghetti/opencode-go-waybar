using Microsoft.Data.Sqlite;
using OpencodeGoWaybar.Brokers.Storages;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.OpenCodeMessages;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;

internal sealed partial class OpenCodeDatabaseService : IOpenCodeDatabaseService
{
    /// <summary>What the file system reports for a file that does not exist.</summary>
    private static readonly DateTimeOffset MissingDatabaseWriteTime =
        new(DateTime.FromFileTimeUtc(0), TimeSpan.Zero);

    private readonly IOpenCodeDatabaseBroker _databaseBroker;
    private readonly ILoggingBroker _loggingBroker;
    private readonly OpenCodeGoOptions _options;

    public OpenCodeDatabaseService(
        IOpenCodeDatabaseBroker databaseBroker,
        ILoggingBroker loggingBroker,
        OpenCodeGoOptions options)
    {
        _databaseBroker = databaseBroker;
        _loggingBroker = loggingBroker;
        _options = options;
    }

    public ValueTask<IReadOnlyList<RecentUsageDay>> RetrieveRecentUsageDaysAsync(
        DateTimeOffset cutoff,
        string providerId,
        CancellationToken cancellationToken) =>
        TryCatchAsync(cutoff, providerId, cancellationToken);

    public ValueTask<DateTimeOffset?> RetrieveLastWriteTimeAsync(CancellationToken cancellationToken) =>
        TryCatchLastWriteTimeAsync(cancellationToken);

    /// <summary>
    /// Turns the aggregated rows into domain days. SQLite reports the date as an
    /// ISO string and the cost as a float; the rounding matches what the cache
    /// and the bar have always shown.
    /// </summary>
    private static IReadOnlyList<RecentUsageDay> MapUsageDays(IReadOnlyList<OpenCodeUsageDayRow> rows) =>
        rows
            .Select(row => new RecentUsageDay(
                DateOnly.Parse(row.Date, System.Globalization.CultureInfo.InvariantCulture),
                row.Tokens,
                decimal.Round((decimal)row.Cost, 4)))
            .ToList();
}
