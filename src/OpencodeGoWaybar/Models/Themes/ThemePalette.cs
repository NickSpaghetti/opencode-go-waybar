namespace OpencodeGoWaybar.Models.Themes;

/// <summary>
/// Every colour role a detail window needs, resolved from the bar's own
/// stylesheet. The role names match the resource keys the window declares, so a
/// consumer assigns them one for one rather than interpreting them.
///
/// A role the stylesheet does not name is derived from the background and
/// foreground pair rather than left empty, because most hand-written Waybar
/// themes define a dozen colours and no hairline.
/// </summary>
public sealed record ThemePalette
{
    public required bool IsLight { get; init; }

    /// <summary>
    /// The bar's own monospace family, when its stylesheet names one. Null lets
    /// the consumer keep its built-in default.
    /// </summary>
    public required string? MonoFontFamily { get; init; }

    public required ThemeColor WindowBg { get; init; }
    public required ThemeColor ChromeBg { get; init; }
    public required ThemeColor ToolbarBg { get; init; }
    public required ThemeColor RowAltBg { get; init; }
    public required ThemeColor TrackBg { get; init; }
    public required ThemeColor Hairline { get; init; }
    public required ThemeColor RowHairline { get; init; }

    public required ThemeColor TextPrimary { get; init; }
    public required ThemeColor TextBody { get; init; }
    public required ThemeColor TextMuted { get; init; }
    public required ThemeColor TextFaint { get; init; }

    public required ThemeColor AccentLine { get; init; }
    public required ThemeColor AccentText { get; init; }
    public required ThemeColor BarFill { get; init; }
    public required ThemeColor BarFillToday { get; init; }

    public required ThemeColor Ok { get; init; }
    public required ThemeColor Caution { get; init; }
    public required ThemeColor Danger { get; init; }
    public required ThemeColor OkText { get; init; }
    public required ThemeColor CautionText { get; init; }

    public required ThemeColor WarnBg { get; init; }
    public required ThemeColor WarnBorder { get; init; }
    public required ThemeColor WarnText { get; init; }
}
