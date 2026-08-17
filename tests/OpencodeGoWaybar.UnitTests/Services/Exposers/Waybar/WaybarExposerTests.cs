using System.Text.Json;
using OpencodeGoWaybar.Brokers.Apis.Usage;
using OpencodeGoWaybar.Models.Processings.Usage;
using OpencodeGoWaybar.Services.Exposers.Waybar;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Exposers.Waybar;

public sealed class WaybarExposerTests
{
    [Fact]
    public async Task EmitsHiddenJsonWhenNoProcessIsActive()
    {
        var exposer = new WaybarExposer();

        var json = await exposer.ExposeAsync(false, null, null, CancellationToken.None);
        using var document = JsonDocument.Parse(json);

        Assert.False(document.RootElement.GetProperty("visible").GetBoolean());
        Assert.Equal("hidden", document.RootElement.GetProperty("class").GetString());
    }

    [Fact]
    public async Task EmitsWeeklyUsageAndTooltip()
    {
        var usage = new UsageResponse(new Usage(
            new UsageWindow("ok", 10, DateTimeOffset.UtcNow),
            new UsageWindow("ok", 42, DateTimeOffset.UtcNow),
            new UsageWindow("ok", 20, DateTimeOffset.UtcNow)));
        var snapshot = new UsageSnapshot(
            usage,
            new[] { new RecentUsageDay(new DateOnly(2026, 8, 16), 100, 0.2m) },
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var exposer = new WaybarExposer();

        var json = await exposer.ExposeAsync(true, snapshot, null, CancellationToken.None);
        using var document = JsonDocument.Parse(json);

        Assert.Equal("Go · 42%", document.RootElement.GetProperty("text").GetString());
        Assert.Contains("Weekly: 42%", document.RootElement.GetProperty("tooltip").GetString(), StringComparison.Ordinal);
        Assert.Equal("opencode-go", document.RootElement.GetProperty("class").GetString());
    }

    [Fact]
    public async Task EmitsSafeErrorJson()
    {
        var exposer = new WaybarExposer();

        var json = await exposer.ExposeAsync(true, null, new InvalidOperationException("secret-key-not-output"), CancellationToken.None);

        Assert.DoesNotContain("secret-key-not-output", json, StringComparison.Ordinal);
        Assert.Contains("error", json, StringComparison.Ordinal);
    }
}
