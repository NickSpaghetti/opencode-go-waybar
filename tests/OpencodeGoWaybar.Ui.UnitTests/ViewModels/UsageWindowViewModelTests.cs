using NSubstitute;
using OpencodeGoWaybar.Exposers.Usages;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exposures;
using OpencodeGoWaybar.Ui.ViewModels;

namespace OpencodeGoWaybar.Ui.UnitTests.ViewModels;

public sealed partial class UsageWindowViewModelTests
{
    /// <summary>The instant the mockup was drawn at, so its numbers line up.</summary>
    private static readonly DateTimeOffset Now =
        new(2026, 8, 19, 15, 17, 0, TimeSpan.Zero);

    /// <summary>
    /// The seven days from the mockup. Its footer totals turn out to be
    /// internally consistent, so they double as expected values: 842,113 tokens,
    /// $12.40, a 198,402 peak and a 120,302 mean.
    /// </summary>
    private static readonly RecentUsageDay[] MockupDays =
    [
        new(new DateOnly(2026, 8, 19), 198_402, 2.94m),
        new(new DateOnly(2026, 8, 18), 24_118, 0.36m),
        new(new DateOnly(2026, 8, 17), 109_655, 1.62m),
        new(new DateOnly(2026, 8, 16), 160_730, 2.37m),
        new(new DateOnly(2026, 8, 15), 47_891, 0.71m),
        new(new DateOnly(2026, 8, 14), 122_404, 1.81m),
        new(new DateOnly(2026, 8, 13), 178_913, 2.59m),
    ];

    private static UsageWindowViewModel CreateViewModel(UsageView view) =>
        new(CreateExposer(view), new FixedTimeProvider(Now));

    private static IUsageExposer CreateExposer(UsageView view)
    {
        var usageExposer = Substitute.For<IUsageExposer>();

        usageExposer.ExposeUsageAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(view));

        return usageExposer;
    }

    private static UsageView CreateView(
        UsageWindowView? rolling = null,
        UsageWindowView? weekly = null,
        UsageWindowView? monthly = null,
        IReadOnlyList<RecentUsageDay>? recentDays = null,
        DateTimeOffset? apiRetrievedAt = null,
        DateTimeOffset? databaseLastWriteTime = null,
        string? failureMessage = null) =>
        new(ProcessIsActive: true,
            rolling ?? CreateWindow("Rolling · 5h", 61),
            weekly ?? CreateWindow("Weekly", 24),
            monthly ?? CreateWindow("Monthly", 12),
            recentDays ?? MockupDays,
            (recentDays ?? MockupDays).Sum(day => day.Tokens),
            IsRateLimited: false,
            apiRetrievedAt ?? Now.AddSeconds(-4),
            databaseLastWriteTime,
            failureMessage);

    private static UsageWindowView CreateWindow(
        string label,
        int? percent,
        UsageWindowStatus status = UsageWindowStatus.Ok,
        DateTimeOffset? resetsAt = null) =>
        new(label, percent, status, resetsAt);
}
