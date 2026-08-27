using System.Globalization;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exposures;

namespace OpencodeGoWaybar.Ui.ViewModels;

/// <summary>
/// One usage window, ready to bind. Everything is computed once in the
/// constructor from the exposed facts plus a supplied instant: a gauge never
/// reads the clock itself, which is what makes a countdown testable.
/// </summary>
public sealed class UsageWindowGauge
{
    private const string Dash = "—";
    private const string CountdownPrefix = "resets in ";

    public UsageWindowGauge(UsageWindowView window, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(window);

        Label = window.Label;
        ShortLabel = ToShortLabel(window.Label);
        Percent = window.Percent ?? 0d;
        PercentLabel = window.Percent is { } percent
            ? string.Create(CultureInfo.InvariantCulture, $"{percent}%")
            : Dash;
        Status = window.Status;
        StatusLabel = ToStatusLabel(window.Status);
        StatusBrushKey = ToBrushKey(window.Status);
        Countdown = ToCountdown(window.ResetsAt, utcNow);
        CountdownShort = ToShortCountdown(Countdown);
        ResetsAtUtc = ToUtcLabel(window.ResetsAt);
    }

    public string Label { get; }

    /// <summary>The label without its qualifier, so "Rolling · 5h" can start a sentence.</summary>
    public string ShortLabel { get; }

    /// <summary>Drives the ring geometry, so an unknown window draws nothing.</summary>
    public double Percent { get; }

    /// <summary>
    /// Shown instead of the number. An unreported percent must not render as
    /// "0%", which would read as a genuinely empty window.
    /// </summary>
    public string PercentLabel { get; }

    public UsageWindowStatus Status { get; }

    public string StatusLabel { get; }

    /// <summary>
    /// The palette key rather than a brush: a view model that held brushes could
    /// not survive a live theme change, and would drag drawing types in here.
    /// </summary>
    public string StatusBrushKey { get; }

    public string Countdown { get; }

    /// <summary>The countdown without the verb, for the rings' tighter layout.</summary>
    public string CountdownShort { get; }

    public string ResetsAtUtc { get; }

    private static string ToShortLabel(string label) =>
        label.Split('·')[0].Trim();

    private static string ToStatusLabel(UsageWindowStatus status) => status switch
    {
        UsageWindowStatus.Ok => "OK",
        UsageWindowStatus.Caution => "CAUTION",
        UsageWindowStatus.Throttled => "THROTTLED",
        UsageWindowStatus.Spent => "SPENT",
        UsageWindowStatus.RateLimited => "RATE LIMITED",
        _ => "UNKNOWN",
    };

    /// <summary>
    /// Six statuses collapse onto the three palette colours the design uses:
    /// approaching a limit and being throttled both read as caution, while being
    /// spent or refused both read as danger.
    /// </summary>
    private static string ToBrushKey(UsageWindowStatus status) => status switch
    {
        UsageWindowStatus.Ok => "Ok",
        UsageWindowStatus.Caution or UsageWindowStatus.Throttled => "Caution",
        UsageWindowStatus.Spent or UsageWindowStatus.RateLimited => "Danger",
        _ => "TextFaint",
    };

    private static string ToCountdown(DateTimeOffset? resetsAt, DateTimeOffset utcNow)
    {
        if (resetsAt is not { } instant)
        {
            return Dash;
        }

        TimeSpan remaining = instant - utcNow;

        return remaining <= TimeSpan.Zero
            ? "resetting"
            : CountdownPrefix + Duration.Humanise(remaining);
    }

    private static string ToShortCountdown(string countdown) =>
        countdown.StartsWith(CountdownPrefix, StringComparison.Ordinal)
            ? countdown[CountdownPrefix.Length..]
            : countdown;

    private static string ToUtcLabel(DateTimeOffset? resetsAt) =>
        resetsAt?.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)
        ?? Dash;
}
