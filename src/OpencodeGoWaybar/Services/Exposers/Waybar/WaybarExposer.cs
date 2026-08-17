using System.Text.Json;
using System.Text.Json.Serialization;
using OpencodeGoWaybar.Models.Processings.Usage;

namespace OpencodeGoWaybar.Services.Exposers.Waybar;

internal sealed class WaybarExposer : IWaybarExposer
{
    public ValueTask<string> ExposeAsync(
        bool processIsActive,
        UsageSnapshot? snapshot,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var output = !processIsActive
            ? new WaybarOutput("", "", "hidden", false)
            : exception is not null
                ? new WaybarOutput("Go · unavailable", "OpenCode Go usage unavailable", "error", true)
                : CreateVisibleOutput(snapshot);

        return ValueTask.FromResult(JsonSerializer.Serialize(output, WaybarJsonContext.Default.WaybarOutput));
    }

    private static WaybarOutput CreateVisibleOutput(UsageSnapshot? snapshot)
    {
        var weeklyPercent = snapshot?.Usage?.Usage.Weekly.Percent;
        var recentTokens = snapshot?.RecentDays.Sum(day => day.Tokens) ?? 0;
        var tooltip = weeklyPercent is null
            ? "OpenCode Go usage unavailable"
            : $"Weekly: {weeklyPercent}%\nRecent tokens: {recentTokens:N0}";
        var text = weeklyPercent is null ? "Go · unavailable" : $"Go · {weeklyPercent}%";

        return new WaybarOutput(text, tooltip, "opencode-go", true);
    }
}

internal sealed record WaybarOutput(string Text, string Tooltip, string Class, bool Visible);

[JsonSerializable(typeof(WaybarOutput))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class WaybarJsonContext : JsonSerializerContext
{
}
