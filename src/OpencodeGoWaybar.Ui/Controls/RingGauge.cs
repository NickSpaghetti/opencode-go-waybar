using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace OpencodeGoWaybar.Ui.Controls;

/// <summary>
/// A percentage ring, drawn rather than composed. An arc is a few lines of
/// geometry and no control template can express one, so this stays a Control that
/// renders itself.
/// </summary>
public sealed class RingGauge : Control
{
    public static readonly StyledProperty<double> PercentProperty =
        AvaloniaProperty.Register<RingGauge, double>(nameof(Percent));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<RingGauge, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> TrackProperty =
        AvaloniaProperty.Register<RingGauge, IBrush?>(nameof(Track));

    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<RingGauge, double>(nameof(Thickness), 9d);

    static RingGauge() =>
        AffectsRender<RingGauge>(
            PercentProperty,
            FillProperty,
            TrackProperty,
            ThicknessProperty);

    public double Percent
    {
        get => GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Track
    {
        get => GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    public double Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);

        if (size <= 0)
        {
            return;
        }

        var thickness = Thickness;
        var radius = (size - thickness) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);

        if (radius <= 0)
        {
            return;
        }

        if (Track is { } track)
        {
            context.DrawEllipse(null, new Pen(track, thickness), center, radius, radius);
        }

        var fraction = Math.Clamp(Percent, 0, 100) / 100d;

        if (fraction <= 0 || Fill is not { } fill)
        {
            return;
        }

        var pen = new Pen(fill, thickness, lineCap: PenLineCap.Round);

        // A full ring has no arc endpoints to describe, and an ArcSegment whose
        // start equals its end draws nothing at all.
        if (fraction >= 1)
        {
            context.DrawEllipse(null, pen, center, radius, radius);

            return;
        }

        (Point start, Point end, var isLargeArc) = DescribeArc(center, radius, fraction);

        var figure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            Segments =
            [
                new ArcSegment
                {
                    Point = end,
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = isLargeArc,
                },
            ],
        };

        context.DrawGeometry(null, pen, new PathGeometry { Figures = [figure] });
    }

    /// <summary>
    /// The arc's endpoints, swept clockwise from twelve o'clock. Extracted so the
    /// geometry can be checked directly — notably that IsLargeArc flips exactly at
    /// half, which is the one value an arc renderer gets visibly wrong.
    /// </summary>
    internal static (Point Start, Point End, bool IsLargeArc) DescribeArc(
        Point center,
        double radius,
        double fraction)
    {
        var sweep = fraction * 2 * Math.PI;

        var start = new Point(center.X, center.Y - radius);

        var end = new Point(
            center.X + (radius * Math.Sin(sweep)),
            center.Y - (radius * Math.Cos(sweep)));

        return (start, end, fraction > 0.5);
    }
}
