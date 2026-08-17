using Microsoft.Data.Sqlite;
using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Brokers.Storages.OpenCode;

public sealed class OpenCodeDatabaseBrokerTests
{
    [Fact]
    public async Task ReadsRecentOpenCodeGoUsageWithoutIncludingOtherProviders()
    {
        var databasePath = CreateDatabase();
        try
        {
            var broker = new OpenCodeDatabaseBroker(databasePath);

            var messages = await broker.RetrieveMessagesAsync(
                DateTimeOffset.FromUnixTimeMilliseconds(1_699_999_900_000),
                CancellationToken.None);

            Assert.Equal(3, messages.Count);
            Assert.Contains(messages, message => message.Data.Contains("opencode-go", StringComparison.Ordinal));
            Assert.Contains(messages, message => message.Data.Contains("providerID\":\"other", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task OpensDatabaseReadOnly()
    {
        var databasePath = CreateDatabase();
        try
        {
            var broker = new OpenCodeDatabaseBroker(databasePath);

            await broker.RetrieveMessagesAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);

            using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM message";
            Assert.Equal(3L, command.ExecuteScalar());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static string CreateDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"opencode-go-{Guid.NewGuid():N}.db");
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadWriteCreate");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE message (
              id TEXT PRIMARY KEY,
              session_id TEXT NOT NULL,
              time_created INTEGER NOT NULL,
              time_updated INTEGER NOT NULL,
              data TEXT NOT NULL
            );
            INSERT INTO message VALUES
              ('go-1', 'session-1', 1700000000000, 1700000000000,
               '{"providerID":"opencode-go","tokens":{"total":10},"cost":0.001}'),
              ('go-2', 'session-1', 1700000001000, 1700000001000,
               '{"providerID":"opencode-go","tokens":{"total":20},"cost":0.002}'),
              ('other-1', 'session-1', 1700000002000, 1700000002000,
               '{"providerID":"other","tokens":{"total":999},"cost":99.0}');
            """;
        command.ExecuteNonQuery();
        return path;
    }
}
