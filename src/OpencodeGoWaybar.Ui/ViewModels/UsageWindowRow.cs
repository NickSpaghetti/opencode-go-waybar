using System.Globalization;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Ui.ViewModels;

/// <summary>
/// One day in the recent-usage table and the sparkline beside it. "Today" is
/// supplied rather than read from the clock so the highlight is testable.
/// </summary>
public sealed class UsageWindowRow
{
    public UsageWindowRow(RecentUsageDay day, long peakTokens, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(day);

        Date = day.Date;
        Tokens = day.Tokens;
        Cost = day.Cost;
        // A week with no usage has no peak to scale against, and dividing by it
        // would make every bar a NaN height.
        BarFraction = peakTokens == 0 ? 0d : (double)day.Tokens / peakTokens;
        IsToday = day.Date == today;
    }

    public DateOnly Date { get; }

    public long Tokens { get; }

    public decimal Cost { get; }

    /// <summary>This day's height as a share of the week's busiest day.</summary>
    public double BarFraction { get; }

    public bool IsToday { get; }

    /// <summary>Fixed format: a sortable column, not a localised date.</summary>
    public string DateLabel => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>The sparkline's axis ends, where space is tight.</summary>
    public string ShortDateLabel => Date.ToString("d MMM", CultureInfo.InvariantCulture);

    // Counts and money follow the reader's locale, unlike the date column.
    public string TokensLabel => Tokens.ToString("N0", CultureInfo.CurrentCulture);

    public string CostLabel => Cost.ToString("C2", CultureInfo.CurrentCulture);
}
