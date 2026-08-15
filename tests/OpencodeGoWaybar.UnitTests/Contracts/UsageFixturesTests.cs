using System.Text.Json;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Contracts;

public class UsageFixturesTests
{
    private static string FixturesDir => Path.Combine(FindRepoRoot(), "contracts", "fixtures");

    [Fact]
    public void SuccessFixtureExists()
    {
        Assert.True(File.Exists(Path.Combine(FixturesDir, "usage-success.json")));
    }

    [Fact]
    public void PartialFixtureExists()
    {
        Assert.True(File.Exists(Path.Combine(FixturesDir, "usage-partial.json")));
    }

    [Fact]
    public void RateLimitedFixtureExists()
    {
        Assert.True(File.Exists(Path.Combine(FixturesDir, "usage-rate-limited.json")));
    }

    [Fact]
    public void SuccessFixtureHasUsageWindows()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixturesDir, "usage-success.json")));
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("usage", out var usage));
        Assert.Equal(JsonValueKind.Object, usage.ValueKind);
        foreach (var window in new[] { "rolling", "weekly", "monthly" })
        {
            Assert.True(usage.TryGetProperty(window, out var element));
            Assert.True(element.TryGetProperty("status", out _));
            Assert.True(element.TryGetProperty("percent", out _));
            Assert.True(element.TryGetProperty("resetsAt", out _));
        }
    }

    [Fact]
    public void PartialFixtureHasMissingRollingWindow()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixturesDir, "usage-partial.json")));
        var rolling = doc.RootElement.GetProperty("usage").GetProperty("rolling");
        Assert.Equal("no_api_key", rolling.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, rolling.GetProperty("percent").ValueKind);
    }

    [Fact]
    public void FixturesDoNotContainApiKeys()
    {
        foreach (var file in Directory.EnumerateFiles(FixturesDir, "*.json"))
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("Bearer ", content);
            Assert.DoesNotContain("sk-", content);
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "OpencodeGoWaybar.sln")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new InvalidOperationException("Could not locate repository root from " + AppContext.BaseDirectory);
        }

        return current.FullName;
    }
}