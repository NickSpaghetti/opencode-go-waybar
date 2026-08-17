using Microsoft.Data.Sqlite;

namespace OpencodeGoWaybar.Brokers.Storages.OpenCode;

internal sealed class OpenCodeDatabaseBroker(string databasePath) : IOpenCodeDatabaseBroker
{
    public async ValueTask<IReadOnlyList<OpenCodeMessage>> RetrieveMessagesAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
        }.ConnectionString;

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                time_created,
                data
            FROM message
            WHERE time_created > $cutoff
            ORDER BY time_created;
            """;
        command.Parameters.AddWithValue("$cutoff", cutoff.ToUnixTimeMilliseconds());

        var messages = new List<OpenCodeMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new OpenCodeMessage(
                DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(0)),
                reader.GetString(1)));
        }

        return messages;
    }
}
