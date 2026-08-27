using System.Text.Json;
using System.Text.Json.Serialization;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Aggregations.Usage;
using OpencodeGoWaybar.Models.Usages.Exceptions;

namespace OpencodeGoWaybar.Exposers.Waybar;

internal sealed class WaybarExposer(IUsageAggregationService aggregationService) : IWaybarExposer
{
    public async ValueTask<string> ExposeAsync(CancellationToken cancellationToken)
    {
        WaybarOutput output;

        try
        {
            WaybarStatus status = await aggregationService.RetrieveStatusAsync(cancellationToken);

            output = status.ProcessIsActive
                ? CreateVisibleOutput(status.Usage, status.IsRateLimited)
                : new WaybarOutput("", "", "hidden", false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The refresh outlived the budget Main allows it.
            output = CreateFailureOutput(
                new TimeoutException("The usage refresh exceeded the ten-second limit."));
        }
        catch (Exception exception)
        {
            output = CreateFailureOutput(exception);
        }

        return JsonSerializer.Serialize(output, WaybarJsonContext.Default.WaybarOutput);
    }

    /// <summary>
    /// Waybar always needs a payload, so a failure is rendered rather than
    /// thrown — the same job a controller does mapping an exception to a status.
    /// </summary>
    private static WaybarOutput CreateFailureOutput(Exception exception) =>
        new("Go · unavailable", DescribeFailure(exception), "error", true);

    /// <summary>
    /// What went wrong, in the API's own words where it supplied them. Only the
    /// parsed type and message are rendered — never an exception message or a
    /// raw response body, either of which could carry request details.
    /// </summary>
    private static string DescribeFailure(Exception exception)
    {
        if (exception is IUsageApiFailure { ApiError: { } apiError })
        {
            var described = Sanitize(apiError.Message) ?? Sanitize(apiError.Type);

            if (described is not null)
            {
                var qualifier = Sanitize(apiError.Type);

                return qualifier is null || qualifier == described
                    ? $"OpenCode Go: {described}"
                    : $"OpenCode Go: {described} ({qualifier})";
            }
        }

        return exception switch
        {
            UsageCredentialsMissingException => "No OpenCode Go API key configured",
            UsageAuthenticationException => "OpenCode Go rejected the API key",
            UsageRateLimitedException => "OpenCode Go is rate limiting requests",
            UsageApiUnavailableException => "OpenCode Go could not be reached",
            UsageApiResponseException => "OpenCode Go returned unexpected data",
            TimeoutException => "OpenCode Go usage refresh timed out",
            _ => "OpenCode Go usage unavailable",
        };
    }

    /// <summary>
    /// Keeps a hostile or oversized body from reshaping the tooltip: the value
    /// is collapsed onto one line and truncated.
    /// </summary>
    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var collapsed = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return collapsed.Length <= 120 ? collapsed : collapsed[..117] + "...";
    }

    private static WaybarOutput CreateVisibleOutput(UsageSnapshot? snapshot, bool isRateLimited)
    {
        var usage = snapshot?.Usage?.Usage;
        var weeklyPercent = usage?.Weekly.Percent;

        if (usage is null || weeklyPercent is null)
        {
            return new WaybarOutput("Go · unavailable", "OpenCode Go usage unavailable", "opencode-go", true);
        }

        var recentTokens = snapshot?.RecentDays.Sum(day => day.Tokens) ?? 0;

        // The weekly status rides on the weekly line rather than repeating the
        // window name; the other two windows only appear when unhealthy.
        var tooltip = string.Join(
            '\n',
            [
                $"Weekly: {weeklyPercent}%{DescribeStatus(usage.Weekly)}",
                $"Recent tokens: {recentTokens:N0}",
                .. UnhealthyNotices(usage),
            ]);

        // The percent alone cannot distinguish a week that is merely spent from
        // one the API is actively refusing, so the class carries that instead —
        // it is what a Waybar theme can style on. The verdict is read off the
        // snapshot rather than recomputed: one authoritative classifier, so a
        // change to it cannot silently miss the bar's colour.
        var cssClass = isRateLimited ? "opencode-go-rate-limited" : "opencode-go";

        return new WaybarOutput($"Go · {weeklyPercent}%", tooltip, cssClass, true);
    }

    /// <summary>
    /// The rolling and monthly windows, when the API did not call them healthy —
    /// a throttled five-hour window matters even though the bar shows the week.
    /// </summary>
    private static IEnumerable<string> UnhealthyNotices(Usage usage) =>
        new[] { ("Rolling", usage.Rolling), ("Monthly", usage.Monthly) }
            .Where(window => !IsHealthy(window.Item2.Status))
            .Select(window => $"{window.Item1}: {window.Item2.Status}{DescribeReset(window.Item2)}");

    private static string DescribeStatus(UsageWindow window) =>
        IsHealthy(window.Status) ? string.Empty : $" — {window.Status}{DescribeReset(window)}";

    private static string DescribeReset(UsageWindow window) =>
        window.ResetsAt is { } resetsAt
            ? $" (resets {resetsAt.UtcDateTime:yyyy-MM-dd HH:mm} UTC)"
            : string.Empty;

    private static bool IsHealthy(string status) =>
        status.Equals("ok", StringComparison.OrdinalIgnoreCase);
}

internal sealed record WaybarOutput(string Text, string Tooltip, string Class, bool Visible);

[JsonSerializable(typeof(WaybarOutput))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class WaybarJsonContext : JsonSerializerContext
{
}
