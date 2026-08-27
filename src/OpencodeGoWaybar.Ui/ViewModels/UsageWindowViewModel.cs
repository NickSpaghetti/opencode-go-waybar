using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using OpencodeGoWaybar.Exposers.Usages;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exposures;

namespace OpencodeGoWaybar.Ui.ViewModels;

/// <summary>
/// Projects one exposed usage view into everything the three windows bind.
///
/// Its only dependencies are an exposer and a clock. There is no broker and no
/// service here: retrieval, caching and the health policy all sit behind
/// IUsageExposer, which leaves this class doing nothing but presentation.
/// </summary>
public sealed class UsageWindowViewModel : INotifyPropertyChanged
{
    private readonly IUsageExposer usageExposer;
    private readonly TimeProvider timeProvider;

    private UsageView? view;
    private IReadOnlyList<UsageWindowRow> daysOldestFirst = [];

    public UsageWindowViewModel(IUsageExposer usageExposer, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(usageExposer);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.usageExposer = usageExposer;
        this.timeProvider = timeProvider;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<UsageWindowGauge> Windows { get; } = [];

    /// <summary>Newest first, the way the table reads.</summary>
    public ObservableCollection<UsageWindowRow> Days { get; } = [];

    /// <summary>Oldest first, so the sparkline runs left to right through time.</summary>
    public IReadOnlyList<UsageWindowRow> DaysOldestFirst => this.daysOldestFirst;

    public AsyncRelayCommand RefreshCommand { get; }

    public string FirstDayLabel =>
        this.daysOldestFirst.Count == 0 ? Dash : this.daysOldestFirst[0].ShortDateLabel;

    public string LastDayLabel =>
        this.daysOldestFirst.Count == 0 ? Dash : this.daysOldestFirst[^1].ShortDateLabel;

    public long TotalTokens => Days.Sum(row => row.Tokens);

    public decimal TotalCost => Days.Sum(row => row.Cost);

    public long PeakTokens => Days.Count == 0 ? 0 : Days.Max(row => row.Tokens);

    public long MeanTokens => Days.Count == 0
        ? 0
        : (long)Math.Round((double)TotalTokens / Days.Count);

    public decimal CostPerMillion => TotalTokens == 0
        ? 0m
        : TotalCost / TotalTokens * 1_000_000m;

    public string? WarningText { get; private set; }

    public bool HasWarning => WarningText is not null;

    /// <summary>
    /// How stale the numbers are. The database time is shown alongside because
    /// the two move independently: recent-day totals can be fresher than the API
    /// window percentages, or the other way round.
    /// </summary>
    public string RefreshedLabel
    {
        get
        {
            if (this.view?.ApiRetrievedAt is not { } retrievedAt)
            {
                return "never refreshed";
            }

            var age = Duration.Humanise(this.timeProvider.GetUtcNow() - retrievedAt);

            var databaseTime = this.view.DatabaseLastWriteTime is { } lastWrite
                ? lastWrite.UtcDateTime.ToString(
                    " · 'db' HH:mm:ss'Z'",
                    CultureInfo.InvariantCulture)
                : string.Empty;

            return $"refreshed {age} ago{databaseTime}";
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        this.view = await this.usageExposer.ExposeUsageAsync(cancellationToken);

        Rebuild();
    }

    private const string Dash = "—";

    private void Rebuild()
    {
        Windows.Clear();
        Days.Clear();
        this.daysOldestFirst = [];
        WarningText = null;

        if (this.view is not { } current)
        {
            RaiseAll();

            return;
        }

        DateTimeOffset utcNow = this.timeProvider.GetUtcNow();
        DateOnly today = DateOnly.FromDateTime(utcNow.UtcDateTime);

        Windows.Add(new UsageWindowGauge(current.Rolling, utcNow));
        Windows.Add(new UsageWindowGauge(current.Weekly, utcNow));
        Windows.Add(new UsageWindowGauge(current.Monthly, utcNow));

        var peakTokens = current.RecentDays.Count == 0
            ? 0
            : current.RecentDays.Max(day => day.Tokens);

        foreach (RecentUsageDay day in current.RecentDays.OrderByDescending(day => day.Date))
        {
            Days.Add(new UsageWindowRow(day, peakTokens, today));
        }

        this.daysOldestFirst = [.. Days.Reverse()];
        WarningText = BuildWarning(current);

        RaiseAll();
    }

    /// <summary>
    /// One line, for the strip above the totals. A failed refresh outranks any
    /// window state: the windows would be describing a snapshot that never
    /// arrived. Caution deliberately does not raise a banner — approaching a
    /// limit is what the coloured ring is already saying.
    /// </summary>
    private string? BuildWarning(UsageView current)
    {
        if (current.FailureMessage is { Length: > 0 } failureMessage)
        {
            return failureMessage;
        }

        foreach (UsageWindowGauge gauge in Windows)
        {
            switch (gauge.Status)
            {
                case UsageWindowStatus.RateLimited:
                    return $"{gauge.ShortLabel} window rate-limited — resets {gauge.ResetsAtUtc}";

                case UsageWindowStatus.Throttled:
                    return $"{gauge.ShortLabel} window throttled — "
                        + $"new requests queue until {gauge.ResetsAtUtc}";

                case UsageWindowStatus.Spent:
                    return $"{gauge.ShortLabel} window spent at {gauge.PercentLabel} — "
                        + $"resets {gauge.ResetsAtUtc}";

                default:
                    continue;
            }
        }

        return null;
    }

    /// <summary>
    /// The collections raise their own changes; every computed property is
    /// re-read from scratch, so they are announced together after a rebuild.
    /// </summary>
    private void RaiseAll()
    {
        string[] names =
        [
            nameof(DaysOldestFirst), nameof(FirstDayLabel), nameof(LastDayLabel),
            nameof(TotalTokens), nameof(TotalCost), nameof(PeakTokens),
            nameof(MeanTokens), nameof(CostPerMillion),
            nameof(WarningText), nameof(HasWarning), nameof(RefreshedLabel),
        ];

        foreach (var name in names)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
