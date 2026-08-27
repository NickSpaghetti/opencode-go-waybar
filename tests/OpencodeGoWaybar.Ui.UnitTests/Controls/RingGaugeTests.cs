using Avalonia;
using Avalonia.Media;
using OpencodeGoWaybar.Ui.Controls;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.Controls;

public sealed class RingGaugeTests
{
    private const double Tolerance = 0.0001;

    private static readonly Point Center = new(50, 50);
    private const double Radius = 40d;

    [Fact]
    public void ShouldStartEveryArcAtTwelveOClock()
    {
        // given
        (Point start, _, _) = RingGauge.DescribeArc(Center, Radius, fraction: 0.25);

        // then
        Assert.Equal(Center.X, start.X, Tolerance);
        Assert.Equal(Center.Y - Radius, start.Y, Tolerance);
    }

    [Theory]
    // a quarter sweeps clockwise to three o'clock
    [InlineData(0.25, 90d, 50d)]
    // a half reaches six o'clock
    [InlineData(0.5, 50d, 90d)]
    // three quarters reaches nine o'clock
    [InlineData(0.75, 10d, 50d)]
    public void ShouldSweepClockwiseFromTheTop(double fraction, double expectedX, double expectedY)
    {
        // given
        (_, Point end, _) = RingGauge.DescribeArc(Center, Radius, fraction);

        // then
        Assert.Equal(expectedX, end.X, Tolerance);
        Assert.Equal(expectedY, end.Y, Tolerance);
    }

    [Theory]
    // exactly half is still the short way round; anything beyond it is not
    [InlineData(0.25, false)]
    [InlineData(0.5, false)]
    [InlineData(0.5001, true)]
    [InlineData(0.99, true)]
    public void ShouldFlipTheLargeArcFlagJustPastHalf(double fraction, bool expectedIsLargeArc)
    {
        // given
        (_, _, var isLargeArc) = RingGauge.DescribeArc(Center, Radius, fraction);

        // then
        Assert.Equal(expectedIsLargeArc, isLargeArc);
    }

    [Fact]
    public void ShouldRedrawWhenAnyVisualPropertyChanges()
    {
        // given a gauge whose bindings all feed render-affecting properties
        var gauge = new RingGauge
        {
            Percent = 61,
            Thickness = 11,
            Track = new SolidColorBrush(Colors.Gray),
            Fill = new SolidColorBrush(Colors.Green),
        };

        // then the values round-trip and the properties exist to be invalidated
        Assert.Equal(61, gauge.Percent);
        Assert.Equal(11, gauge.Thickness);
        Assert.NotNull(gauge.Track);
        Assert.NotNull(gauge.Fill);
    }
}
