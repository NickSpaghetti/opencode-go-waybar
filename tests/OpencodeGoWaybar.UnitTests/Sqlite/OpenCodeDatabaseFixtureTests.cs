using System.Text.Json;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Sqlite;

public class OpenCodeDatabaseFixtureTests
{
    [Fact]
    public void FixtureCreatesMessageTable()
    {
        using var fixture = new OpenCodeDatabaseFixture();
        using var command = fixture.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='message'";
        var count = Convert.ToInt64(command.ExecuteScalar());
        Assert.Equal(1, count);
    }

    [Fact]
    public void FixtureInsertRoundTrip()
    {
        using var fixture = new OpenCodeDatabaseFixture();
        fixture.InsertMessage(
            id: "msg-1",
            sessionId: "session-1",
            timeCreatedMs: 1_700_000_000_000L,
            dataJson: "{\"providerID\":\"opencode-go\",\"tokens\":{\"total\":42},\"cost\":0.001}");

        using var command = fixture.CreateCommand();
        command.CommandText = "SELECT data FROM message WHERE id = 'msg-1'";
        var stored = (string?)command.ExecuteScalar();
        Assert.NotNull(stored);
        using var doc = JsonDocument.Parse(stored!);
        Assert.Equal("opencode-go", doc.RootElement.GetProperty("providerID").GetString());
    }

    [Fact]
    public void FixtureFiltersByProviderId()
    {
        using var fixture = new OpenCodeDatabaseFixture();
        fixture.InsertMessage("m1", "s1", 1_700_000_000_000L, "{\"providerID\":\"opencode-go\",\"tokens\":{\"total\":10},\"cost\":0.001}");
        fixture.InsertMessage("m2", "s1", 1_700_000_100_000L, "{\"providerID\":\"other\",\"tokens\":{\"total\":99},\"cost\":0.99}");
        fixture.InsertMessage("m3", "s1", 1_700_000_200_000L, "{\"providerID\":\"opencode-go\",\"tokens\":{\"total\":20},\"cost\":0.002}");

        using var command = fixture.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM message
            WHERE json_extract(data, '$.providerID') = 'opencode-go'
            """;
        var count = Convert.ToInt64(command.ExecuteScalar());
        Assert.Equal(2, count);
    }

    [Fact]
    public void FixtureAcceptsMissingTokensAndCostFields()
    {
        using var fixture = new OpenCodeDatabaseFixture();
        fixture.InsertMessage("m1", "s1", 1_700_000_000_000L, "{\"providerID\":\"opencode-go\"}");
        fixture.InsertMessage("m2", "s1", 1_700_000_100_000L, "{\"providerID\":\"opencode-go\",\"tokens\":{}}");

        using var command = fixture.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM message";
        var count = Convert.ToInt64(command.ExecuteScalar());
        Assert.Equal(2, count);
    }
}