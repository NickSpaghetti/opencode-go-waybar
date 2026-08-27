using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exposures;
using OpencodeGoWaybar.Ui.ViewModels;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.ViewModels;

public sealed class UsageWindowGaugeTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 19, 15, 17, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(UsageWindowStatus.Unknown, "UNKNOWN", "TextFaint")]
    [InlineData(UsageWindowStatus.Ok, "OK", "Ok")]
    [InlineData(UsageWindowStatus.Caution, "CAUTION", "Caution")]
    [InlineData(UsageWindowStatus.Throttled, "THROTTLED", "Caution")]
    [InlineData(UsageWindowStatus.Spent, "SPENT", "Danger")]
    [InlineData(UsageWindowStatus.RateLimited, "RATE LIMITED", "Danger")]
    public void ShouldMapEveryStatusToALabelAndAPaletteKey(
        UsageWindowStatus status,
        string expectedLabel,
        string expectedBrushKey)
    {
        // given
        var gauge = CreateGauge(status: status);

        // then
        Assert.Equal(expectedLabel, gauge.StatusLabel);
        Assert.Equal(expectedBrushKey, gauge.StatusBrushKey);
    }

    [Fact]
    public void ShouldShowADashRatherThanZeroWhenNoPercentWasReported()
    {
        // given a window the API said nothing numeric about
        var gauge = CreateGauge(percent: null, status: UsageWindowStatus.Unknown);

        // then the ring collapses but the text does not claim zero percent
        Assert.Equal(0d, gauge.Percent);
        Assert.Equal("—", gauge.PercentLabel);
    }

    [Fact]
    public void ShouldLabelAReportedPercent()
    {
        // given
        var gauge = CreateGauge(percent: 61);

        // then
        Assert.Equal(61d, gauge.Percent);
        Assert.Equal("61%", gauge.PercentLabel);
    }

    [Theory]
    // a countdown, and the short form the rings use without the verb
    [InlineData(4, 12, "resets in 4h 12m", "4h 12m")]
    [InlineData(0, 45, "resets in 45m", "45m")]
    public void ShouldCountDownToTheReset(
        int hours,
        int minutes,
        string expectedCountdown,
        string expectedShortCountdown)
    {
        // given
        var resetsAt = Now.AddHours(hours).AddMinutes(minutes);
        var gauge = CreateGauge(resetsAt: resetsAt);

        // then
        Assert.Equal(expectedCountdown, gauge.Countdown);
        Assert.Equal(expectedShortCountdown, gauge.CountdownShort);
    }

    [Fact]
    public void ShouldCountDownInDaysAndHoursOverALongerWindow()
    {
        // given
        var gauge = CreateGauge(resetsAt: Now.AddDays(2).AddHours(10));

        // then
        Assert.Equal("resets in 2d 10h", gauge.Countdown);
    }

    [Fact]
    public void ShouldDropTheHoursFromAWholeNumberOfDays()
    {
        // given
        var gauge = CreateGauge(resetsAt: Now.AddDays(27));

        // then
        Assert.Equal("resets in 27d", gauge.Countdown);
    }

    [Fact]
    public void ShouldReportAWindowThatIsAlreadyDueAsResetting()
    {
        // given a reset the clock has passed
        var gauge = CreateGauge(resetsAt: Now.AddMinutes(-1));

        // then
        Assert.Equal("resetting", gauge.Countdown);
    }

    [Fact]
    public void ShouldShowADashWhenNoResetTimeIsKnown()
    {
        // given
        var gauge = CreateGauge(resetsAt: null);

        // then
        Assert.Equal("—", gauge.Countdown);
        Assert.Equal("—", gauge.CountdownShort);
        Assert.Equal("—", gauge.ResetsAtUtc);
    }

    [Fact]
    public void ShouldRenderTheResetInstantAsUtc()
    {
        // given a reset expressed in a non-UTC offset
        var resetsAt = new DateTimeOffset(2026, 8, 19, 21, 29, 0, TimeSpan.FromHours(2));
        var gauge = CreateGauge(resetsAt: resetsAt);

        // then it is shown in UTC, not as supplied
        Assert.Equal("2026-08-19 19:29 UTC", gauge.ResetsAtUtc);
    }

    [Fact]
    public void ShouldShortenTheLabelForAWarningSentence()
    {
        // given the rolling window's display label
        var gauge = CreateGauge(label: "Rolling · 5h");

        // then the qualifier is dropped so a sentence reads properly
        Assert.Equal("Rolling", gauge.ShortLabel);
    }

    private static UsageWindowGauge CreateGauge(
        string label = "Rolling · 5h",
        int? percent = 61,
        UsageWindowStatus status = UsageWindowStatus.Ok,
        DateTimeOffset? resetsAt = null) =>
        new(new UsageWindowView(label, percent, status, resetsAt), Now);
}
