using System.Globalization;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Ui.ViewModels;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.ViewModels;

public sealed class UsageWindowRowTests
{
    private static readonly DateOnly Today = new(2026, 8, 19);

    [Fact]
    public void ShouldScaleTheBarAgainstThePeakDay()
    {
        // given a day at half the week's peak
        var row = new UsageWindowRow(
            new RecentUsageDay(new DateOnly(2026, 8, 18), Tokens: 50_000, Cost: 1m),
            peakTokens: 100_000,
            today: Today);

        // then
        Assert.Equal(0.5, row.BarFraction);
    }

    [Fact]
    public void ShouldGiveThePeakDayAFullBar()
    {
        // given
        var row = new UsageWindowRow(
            new RecentUsageDay(Today, Tokens: 198_402, Cost: 2.94m),
            peakTokens: 198_402,
            today: Today);

        // then
        Assert.Equal(1d, row.BarFraction);
    }

    [Fact]
    public void ShouldNotDivideByAPeakOfZero()
    {
        // given a week with no recorded usage at all
        var row = new UsageWindowRow(
            new RecentUsageDay(Today, Tokens: 0, Cost: 0m),
            peakTokens: 0,
            today: Today);

        // then
        Assert.Equal(0d, row.BarFraction);
    }

    [Theory]
    [InlineData(19, true)]
    [InlineData(18, false)]
    public void ShouldPickOutTodayAgainstTheSuppliedDate(int day, bool expectedIsToday)
    {
        // given
        var row = new UsageWindowRow(
            new RecentUsageDay(new DateOnly(2026, 8, day), Tokens: 1, Cost: 0m),
            peakTokens: 1,
            today: Today);

        // then
        Assert.Equal(expectedIsToday, row.IsToday);
    }

    [Fact]
    public void ShouldLabelTheDateTokensAndCost()
    {
        // given a culture so the assertions are about formatting, not locale
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        try
        {
            var row = new UsageWindowRow(
                new RecentUsageDay(new DateOnly(2026, 8, 19), Tokens: 198_402, Cost: 2.94m),
                peakTokens: 198_402,
                today: Today);

            // then
            Assert.Equal("2026-08-19", row.DateLabel);
            Assert.Equal("198,402", row.TokensLabel);
            Assert.Equal("$2.94", row.CostLabel);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ShouldLabelTheDayForASparklineAxis()
    {
        // given
        var row = new UsageWindowRow(
            new RecentUsageDay(new DateOnly(2026, 8, 13), Tokens: 1, Cost: 0m),
            peakTokens: 1,
            today: Today);

        // then
        Assert.Equal("13 Aug", row.ShortDateLabel);
    }
}
