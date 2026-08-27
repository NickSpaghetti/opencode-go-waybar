using Microsoft.Data.Sqlite;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Brokers.Storages;
using Xunit;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.IntegrationTests;

[Trait("Tier", "Integration")]
public sealed class OpenCodeDatabaseIntegrationTests
{
    [Fact]
    public async Task ShouldReadRecentOpenCodeGoUsageWithoutIncludingOtherProvidersAsync()
    {
        // given
        var databasePath = CreateDatabase();
        try
        {
            var broker = new OpenCodeDatabaseBroker(new OpenCodeGoOptions { DatabasePath = databasePath });

            var days = await broker.SelectUsageDaysByCutoffAsync(
                DateTimeOffset.FromUnixTimeMilliseconds(1_699_999_900_000),
                "opencode-go",
                CancellationToken.None);

            // then
            // Two opencode-go rows on one day; the other provider's row is excluded.
            var day = Assert.Single(days);
            Assert.Equal(30, day.Tokens);
            Assert.Equal("2023-11-14", day.Date);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ShouldOpenDatabaseReadOnlyAsync()
    {
        // given
        var databasePath = CreateDatabase();
        try
        {
            var broker = new OpenCodeDatabaseBroker(new OpenCodeGoOptions { DatabasePath = databasePath });

            await broker.SelectUsageDaysByCutoffAsync(DateTimeOffset.UnixEpoch, "opencode-go", CancellationToken.None);

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

    /// <summary>
    /// The daily totals are summed by SQLite, so this drives a real database to
    /// prove the grouping, the provider filter and the date bucketing agree with
    /// what the bar used to compute in C#.
    /// </summary>
    [Fact]
    public async Task ShouldAggregateRecentUsageDaysInTheDatabaseAsync()
    {
        // given
        var databasePath = CreateDatabase();

        try
        {
            var options = new OpenCodeGoOptions { DatabasePath = databasePath };
            var service = new OpenCodeDatabaseService(
                new OpenCodeDatabaseBroker(options),
                Substitute.For<ILoggingBroker>(),
                options);

            // when
            IReadOnlyList<RecentUsageDay> days = await service.RetrieveRecentUsageDaysAsync(
                DateTimeOffset.FromUnixTimeMilliseconds(1_699_999_900_000),
                "opencode-go",
                CancellationToken.None);

            // then
            // The fixture holds two opencode-go rows (10 + 20 tokens) and one
            // belonging to another provider, all on the same day.
            RecentUsageDay day = Assert.Single(days);
            Assert.Equal(30, day.Tokens);
            Assert.Equal(new DateOnly(2023, 11, 14), day.Date);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ShouldReturnNoUsageDaysWhenNoRowsMatchTheProviderAsync()
    {
        // given
        var databasePath = CreateDatabase();

        try
        {
            var options = new OpenCodeGoOptions { DatabasePath = databasePath };
            var service = new OpenCodeDatabaseService(
                new OpenCodeDatabaseBroker(options),
                Substitute.For<ILoggingBroker>(),
                options);

            // when
            IReadOnlyList<RecentUsageDay> days = await service.RetrieveRecentUsageDaysAsync(
                DateTimeOffset.FromUnixTimeMilliseconds(1_699_999_900_000),
                "not-a-provider",
                CancellationToken.None);

            // then
            Assert.Empty(days);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}
