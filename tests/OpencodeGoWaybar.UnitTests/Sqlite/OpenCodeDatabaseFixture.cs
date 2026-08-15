using Microsoft.Data.Sqlite;

namespace OpencodeGoWaybar.UnitTests.Sqlite;

/// <summary>
/// Builds an in-memory SQLite database that mirrors the OpenCode <c>message</c>
/// table. Tests use it as a fixture to exercise the recent-usage query.
///
/// <para>
/// This fixture is isolated. It uses SQLite's <c>:memory:</c> data source, so it
/// cannot read or write the user's real OpenCode database. The connection is
/// disposed when the fixture is disposed, which removes the in-memory database.
/// </para>
/// </summary>
public sealed class OpenCodeDatabaseFixture : IDisposable
{
    private const string InMemoryDataSource = ":memory:";

    private readonly SqliteConnection _connection;

    public string ConnectionString => _connection.ConnectionString;

    public SqliteCommand CreateCommand() => _connection.CreateCommand();

    public OpenCodeDatabaseFixture()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = InMemoryDataSource,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        };
        _connection = new SqliteConnection(builder.ConnectionString);
        _connection.Open();
        CreateSchema();
    }

    private void CreateSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE message (
              id TEXT PRIMARY KEY,
              session_id TEXT NOT NULL,
              time_created INTEGER NOT NULL,
              time_updated INTEGER NOT NULL,
              data TEXT NOT NULL
            );
            CREATE INDEX message_session_time_created_id_idx
              ON message (session_id, time_created, id);
            """;
        command.ExecuteNonQuery();
    }

    public void InsertMessage(string id, string sessionId, long timeCreatedMs, string dataJson)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            INSERT INTO message (id, session_id, time_created, time_updated, data)
            VALUES ($id, $session, $time_created, $time_updated, $data)
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$session", sessionId);
        command.Parameters.AddWithValue("$time_created", timeCreatedMs);
        command.Parameters.AddWithValue("$time_updated", timeCreatedMs);
        command.Parameters.AddWithValue("$data", dataJson);
        command.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}