using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exposures;
using OpencodeGoWaybar.Ui.ViewModels;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.ViewModels;

public sealed partial class UsageWindowViewModelTests
{
    [Fact]
    public async Task ShouldProjectTheThreeWindowsInOrderAsync()
    {
        // given
        UsageWindowViewModel viewModel = CreateViewModel(CreateView());

        // when
        await viewModel.RefreshAsync();

        // then
        Assert.Equal(3, viewModel.Windows.Count);
        Assert.Equal("Rolling · 5h", viewModel.Windows[0].Label);
        Assert.Equal("Weekly", viewModel.Windows[1].Label);
        Assert.Equal("Monthly", viewModel.Windows[2].Label);
        Assert.Equal(61d, viewModel.Windows[0].Percent);
    }

    [Fact]
    public async Task ShouldOrderDaysNewestFirstAndSparklineOldestFirstAsync()
    {
        // given
        UsageWindowViewModel viewModel = CreateViewModel(CreateView());

        // when
        await viewModel.RefreshAsync();

        // then the table reads newest first
        Assert.Equal(new DateOnly(2026, 8, 19), viewModel.Days[0].Date);
        Assert.Equal(new DateOnly(2026, 8, 13), viewModel.Days[^1].Date);

        // and the sparkline runs left to right through time
        Assert.Equal(new DateOnly(2026, 8, 13), viewModel.DaysOldestFirst[0].Date);
        Assert.Equal(new DateOnly(2026, 8, 19), viewModel.DaysOldestFirst[^1].Date);
        Assert.Equal("13 Aug", viewModel.FirstDayLabel);
        Assert.Equal("19 Aug", viewModel.LastDayLabel);
    }

    [Fact]
    public async Task ShouldTotalTheWeekTheWayTheMockupDoesAsync()
    {
        // given
        UsageWindowViewModel viewModel = CreateViewModel(CreateView());

        // when
        await viewModel.RefreshAsync();

        // then
        Assert.Equal(842_113, viewModel.TotalTokens);
        Assert.Equal(12.40m, viewModel.TotalCost);
        Assert.Equal(198_402, viewModel.PeakTokens);
        Assert.Equal(120_302, viewModel.MeanTokens);
        Assert.Equal(14.72m, Math.Round(viewModel.CostPerMillion, 2));
    }

    [Fact]
    public async Task ShouldNotDivideByZeroTokensAsync()
    {
        // given a week with nothing recorded
        UsageWindowViewModel viewModel = CreateViewModel(CreateView(recentDays: []));

        // when
        await viewModel.RefreshAsync();

        // then
        Assert.Equal(0, viewModel.TotalTokens);
        Assert.Equal(0m, viewModel.CostPerMillion);
        Assert.Equal(0, viewModel.MeanTokens);
        Assert.Equal(0, viewModel.PeakTokens);
        Assert.Equal("—", viewModel.FirstDayLabel);
        Assert.Equal("—", viewModel.LastDayLabel);
    }

    [Fact]
    public async Task ShouldMarkTodaysRowAgainstTheClockAsync()
    {
        // given
        UsageWindowViewModel viewModel = CreateViewModel(CreateView());

        // when
        await viewModel.RefreshAsync();

        // then exactly one row is today, and it is the 19th
        UsageWindowRow today = Assert.Single(viewModel.Days, row => row.IsToday);
        Assert.Equal(new DateOnly(2026, 8, 19), today.Date);
    }

    [Fact]
    public async Task ShouldWarnAboutAThrottledWindowInTheMockupsWordsAsync()
    {
        // given the mockup's throttled rolling window
        var resetsAt = new DateTimeOffset(2026, 8, 19, 19, 29, 0, TimeSpan.Zero);

        UsageWindowViewModel viewModel = CreateViewModel(CreateView(
            rolling: CreateWindow("Rolling · 5h", 61, UsageWindowStatus.Throttled, resetsAt)));

        // when
        await viewModel.RefreshAsync();

        // then
        Assert.True(viewModel.HasWarning);
        Assert.Equal(
            "Rolling window throttled — new requests queue until 2026-08-19 19:29 UTC",
            viewModel.WarningText);
    }

    [Fact]
    public async Task ShouldWarnAboutARateLimitedWindowAsync()
    {
        // given
        var resetsAt = new DateTimeOffset(2026, 8, 19, 19, 29, 0, TimeSpan.Zero);

        UsageWindowViewModel viewModel = CreateViewModel(CreateView(
            weekly: CreateWindow("Weekly", 98, UsageWindowStatus.RateLimited, resetsAt)));

        // when
        await viewModel.RefreshAsync();

        // then
        Assert.Equal(
            "Weekly window rate-limited — resets 2026-08-19 19:29 UTC",
            viewModel.WarningText);
    }

    [Fact]
    public async Task ShouldPreferAFailureMessageOverAWindowWarningAsync()
    {
        // given a refresh that failed outright
        UsageWindowViewModel viewModel = CreateViewModel(CreateView(
            rolling: CreateWindow("Rolling · 5h", 61, UsageWindowStatus.Throttled),
            failureMessage: "No OpenCode Go API key configured"));

        // when
        await viewModel.RefreshAsync();

        // then the cause is shown, not a symptom derived from stale windows
        Assert.Equal("No OpenCode Go API key configured", viewModel.WarningText);
    }

    [Fact]
    public async Task ShouldNotWarnWhenEveryWindowIsHealthyAsync()
    {
        // given
        UsageWindowViewModel viewModel = CreateViewModel(CreateView());

        // when
        await viewModel.RefreshAsync();

        // then
        Assert.False(viewModel.HasWarning);
        Assert.Null(viewModel.WarningText);
    }

    [Fact]
    public async Task ShouldSayHowStaleTheSnapshotIsAsync()
    {
        // given an api read four seconds ago and a database written before that
        UsageWindowViewModel viewModel = CreateViewModel(CreateView(
            apiRetrievedAt: Now.AddSeconds(-4),
            databaseLastWriteTime: new DateTimeOffset(2026, 8, 19, 14, 22, 51, TimeSpan.Zero)));

        // when
        await viewModel.RefreshAsync();

        // then
        Assert.Equal("refreshed 4s ago · db 14:22:51Z", viewModel.RefreshedLabel);
    }

    [Fact]
    public void ShouldSayItHasNeverRefreshedBeforeTheFirstLoad()
    {
        // given
        UsageWindowViewModel viewModel = CreateViewModel(CreateView());

        // then
        Assert.Equal("never refreshed", viewModel.RefreshedLabel);
    }

    [Fact]
    public async Task ShouldRaisePropertyChangedSoTheWindowRedrawsAsync()
    {
        // given
        UsageWindowViewModel viewModel = CreateViewModel(CreateView());
        var changed = new List<string>();
        viewModel.PropertyChanged += (_, eventArgs) => changed.Add(eventArgs.PropertyName!);

        // when
        await viewModel.RefreshAsync();

        // then
        Assert.Contains(nameof(viewModel.RefreshedLabel), changed);
        Assert.Contains(nameof(viewModel.TotalTokens), changed);
        Assert.Contains(nameof(viewModel.HasWarning), changed);
    }

    [Fact]
    public async Task ShouldRefreshThroughTheCommandTheToolbarBindsAsync()
    {
        // given
        UsageWindowViewModel viewModel = CreateViewModel(CreateView());

        // when
        await viewModel.RefreshCommand.ExecuteAsync();

        // then
        Assert.Equal(3, viewModel.Windows.Count);
        Assert.Equal(842_113, viewModel.TotalTokens);
    }
}
