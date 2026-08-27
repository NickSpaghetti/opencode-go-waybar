using System.Globalization;
using OpencodeGoWaybar.Ui.Converters;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.Converters;

public sealed class BarHeightConverterTests
{
    [Theory]
    [InlineData(null, 0d)]
    [InlineData(0d, 0d)]
    [InlineData(-0.2, 0d)]
    [InlineData(0.5, 22d)]
    [InlineData(1d, 44d)]
    // a fraction above one is a data error, not a taller bar
    [InlineData(1.5, 44d)]
    public void ShouldScaleAFractionIntoTheTrackHeight(double? fraction, double expectedHeight) =>
        Assert.Equal(expectedHeight, BarHeightConverter.ToHeight(fraction, trackHeight: 44d));

    [Fact]
    public void ShouldGiveATinyFractionAVisibleSliver()
    {
        // given a day with real but small usage, which would round to nothing
        var height = BarHeightConverter.ToHeight(0.01, trackHeight: 44d);

        // then it still draws, because an empty column reads as "no data"
        Assert.Equal(BarHeightConverter.MinimumVisibleHeight, height);
        Assert.True(height > 0);
    }

    [Fact]
    public void ShouldTreatANotANumberFractionAsNothing() =>
        Assert.Equal(0d, BarHeightConverter.ToHeight(double.NaN, trackHeight: 44d));

    [Theory]
    [InlineData("44", 44d)]
    [InlineData("104", 104d)]
    [InlineData("44.5", 44.5)]
    public void ShouldReadTheTrackHeightFromTheConverterParameter(string parameter, double expected) =>
        Assert.Equal(expected, BarHeightConverter.ToTrackHeight(parameter));

    [Fact]
    public void ShouldReadTheTrackHeightAsADoubleWhenSuppliedDirectly() =>
        Assert.Equal(44d, BarHeightConverter.ToTrackHeight(44d));

    [Fact]
    public void ShouldReadNoTrackHeightFromAnUnusableParameter()
    {
        Assert.Equal(0d, BarHeightConverter.ToTrackHeight(null));
        Assert.Equal(0d, BarHeightConverter.ToTrackHeight("not a number"));
    }

    [Fact]
    public void ShouldParseTheConverterParameterIndependentlyOfTheCurrentCulture()
    {
        // given a culture where the decimal separator is a comma
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            // when and then — XAML always writes the invariant form
            Assert.Equal(44.5, BarHeightConverter.ToTrackHeight("44.5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
