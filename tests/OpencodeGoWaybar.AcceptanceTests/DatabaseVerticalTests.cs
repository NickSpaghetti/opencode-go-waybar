using Microsoft.Data.Sqlite;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.AcceptanceTests;

/// <summary>
/// The database vertical through the shipped NativeAOT binary.
///
/// This is the only tier that can catch it: the module reads SQLite through
/// Dapper.AOT, whose materializers are generated at build time. Plain Dapper
/// emits IL at runtime and dies under NativeAOT — but every other tier runs
/// under the JIT, where the reflection path works fine and the failure hides.
///
/// No API key is needed. Priming the cache with a fresh API timestamp makes the
/// module skip the usage call and go straight to the database, which is exactly
/// the path under test.
/// </summary>
[Trait("Tier", "Acceptance")]
[Trait("Layer", "1")]
public sealed class DatabaseVerticalTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShouldReadRecentTokensFromTheOpenCodeDatabaseAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;

        var workspace = Directory.CreateTempSubdirectory("opencode-go-acceptance-").FullName;

        try
        {
            var databasePath = Path.Combine(workspace, "opencode.db");
            SeedDatabase(databasePath);
            PrimeCache(workspace);

            using var agent = await OpenCodeAcpAgent.StartAsync(
                E2eEnvironment.ScriptInstalledOpenCode, cancellationToken);

            var payload = await WaybarModule.RunAsync(
                new Dictionary<string, string>
                {
                    ["OPENCODE_GO_DatabasePath"] = databasePath,
                    ["OPENCODE_GO_CacheDirectory"] = workspace,
                },
                cancellationToken);

            output.WriteLine(payload.Json);
            output.WriteLine($"--- stderr ---{Environment.NewLine}{payload.StandardError}");

            // 1200 + 800 from the two opencode-go rows; the third row belongs to
            // another provider and must not be counted.
            Assert.Contains("Recent tokens: 2,000", payload.Tooltip, StringComparison.Ordinal);
            Assert.Equal("opencode-go", payload.Class);
            Assert.Equal("Go · 24%", payload.Text);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private static void SeedDatabase(string databasePath)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadWriteCreate");
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
              ('go-1', 'session-1', $recent, $recent,
               '{"providerID":"opencode-go","tokens":{"total":1200},"cost":0.12}'),
              ('go-2', 'session-1', $recent, $recent,
               '{"providerID":"opencode-go","tokens":{"total":800},"cost":0.08}'),
              ('other-1', 'session-1', $recent, $recent,
               '{"providerID":"other","tokens":{"total":999},"cost":99.0}');
            """;

        // Inside the module's seven-day window.
        command.Parameters.AddWithValue(
            "$recent",
            DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds());

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// A cache whose API reading is fresh but whose recorded database write time
    /// is stale, so the module refreshes from the database without calling the
    /// usage API — no credentials, no network.
    /// </summary>
    /// <summary>
    /// Two files, not one. The cache was split so each half has a single writer and
    /// needs no lock; priming it means writing both, and the history file is left
    /// stale on purpose so the module reads the seeded database.
    /// </summary>
    private static void PrimeCache(string cacheDirectory)
    {
        var window = """{"Status":"ok","Percent":24,"ResetsAt":"2026-09-01T00:00:00+00:00"}""";

        File.WriteAllText(Path.Combine(cacheDirectory, "windows.json"), $$"""
            {
              "Usage": { "Usage": { "Rolling": {{window}}, "Weekly": {{window}}, "Monthly": {{window}} } },
              "ApiRetrievedAt": "{{DateTimeOffset.UtcNow:O}}"
            }
            """);

        File.WriteAllText(Path.Combine(cacheDirectory, "history.json"), """
            {
              "RecentDays": [],
              "DatabaseLastWriteTime": null
            }
            """);
    }
}
