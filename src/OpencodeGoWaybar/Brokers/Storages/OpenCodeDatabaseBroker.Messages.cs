using Dapper;
using Microsoft.Data.Sqlite;
using OpencodeGoWaybar.Models.OpenCodeMessages;

namespace OpencodeGoWaybar.Brokers.Storages;

internal sealed partial class OpenCodeDatabaseBroker
{
    public async ValueTask<IReadOnlyList<OpenCodeUsageDayRow>> SelectUsageDaysByCutoffAsync(
        DateTimeOffset cutoff,
        string providerId,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(ConnectionString);

        var sql = """
            SELECT
                date(time_created / 1000, 'unixepoch')                  AS Date,
                COALESCE(SUM(json_extract(data, '$.tokens.total')), 0)  AS Tokens,
                COALESCE(SUM(json_extract(data, '$.cost')), 0.0)        AS Cost
            FROM message
            WHERE time_created > @Cutoff
              AND json_extract(data, '$.providerID') = @ProviderId
            GROUP BY 1
            ORDER BY 1;
            """;

        var parameters = new
        {
            Cutoff = cutoff.ToUnixTimeMilliseconds(),
            ProviderId = providerId,
        };

        return (await connection.QueryAsync<OpenCodeUsageDayRow>(sql, parameters)).AsList();
    }
}
