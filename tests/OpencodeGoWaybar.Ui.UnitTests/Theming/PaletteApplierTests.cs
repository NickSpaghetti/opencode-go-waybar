using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using OpencodeGoWaybar.Models.Themes;
using OpencodeGoWaybar.Ui.Converters;
using OpencodeGoWaybar.Ui.Theming;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.Theming;

public sealed class PaletteApplierTests
{
    [Fact]
    public void ShouldSetEveryPaletteKeyTheWindowsBind()
    {
        // given
        var resources = new ResourceDictionary();

        // when
        PaletteApplier.Apply(resources, CreatePalette());

        // then no role is left for Palette.axaml to supply by accident
        foreach (var key in PaletteApplier.ResourceKeys)
        {
            Assert.True(resources.ContainsKey(key), $"missing palette key: {key}");
            Assert.IsType<SolidColorBrush>(resources[key]);
        }
    }

    [Fact]
    public void ShouldMapEveryChannelIncludingAlpha()
    {
        // given
        var resources = new ResourceDictionary();

        // when
        PaletteApplier.Apply(resources, CreatePalette());

        // then
        var windowBg = Assert.IsType<SolidColorBrush>(resources["WindowBg"]);
        Assert.Equal(Color.FromArgb(255, 0x11, 0x11, 0x15), windowBg.Color);

        var textPrimary = Assert.IsType<SolidColorBrush>(resources["TextPrimary"]);
        Assert.Equal(Color.FromArgb(127, 0xc5, 0xf9, 0xff), textPrimary.Color);
    }

    [Fact]
    public void ShouldOverrideTheFallbackDeclaredInAThemeDictionary()
    {
        // given the fallback that Themes/Palette.axaml provides
        var resources = new ResourceDictionary();
        resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            ["WindowBg"] = new SolidColorBrush(Colors.Red),
        };

        // when
        PaletteApplier.Apply(resources, CreatePalette());

        // then the bar's own colour wins, not the shipped default
        IBrush? resolved = PaletteLookup.ResolveBrush(resources, ThemeVariant.Dark, "WindowBg");
        var brush = Assert.IsType<SolidColorBrush>(resolved);
        Assert.Equal(Color.FromArgb(255, 0x11, 0x11, 0x15), brush.Color);
    }

    [Fact]
    public void ShouldSetTheMonoFontFamilyWithAMonospaceFallback()
    {
        // given
        var resources = new ResourceDictionary();

        // when
        PaletteApplier.Apply(resources, CreatePalette());

        // then the bar's family is named first, but a machine without it still
        // lands on something monospaced
        var mono = Assert.IsType<FontFamily>(resources["Mono"]);
        Assert.Contains("MesloLGS Nerd Font Mono", mono.Name, StringComparison.Ordinal);
        Assert.Contains("monospace", mono.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldLeaveTheMonoFontFamilyAloneWhenTheStyleSheetNamesNone()
    {
        // given
        var resources = new ResourceDictionary
        {
            ["Mono"] = new FontFamily("Shipped Default"),
        };

        // when
        PaletteApplier.Apply(resources, CreatePalette(monoFontFamily: null));

        // then
        var mono = Assert.IsType<FontFamily>(resources["Mono"]);
        Assert.Equal("Shipped Default", mono.Name);
    }

    [Fact]
    public void ShouldRejectMissingArguments()
    {
        // given
        var resources = new ResourceDictionary();

        // when and then
        Assert.Throws<ArgumentNullException>(() => PaletteApplier.Apply(null!, CreatePalette()));
        Assert.Throws<ArgumentNullException>(() => PaletteApplier.Apply(resources, null!));
    }

    /// <summary>
    /// Built from the palette the real honkadaloonga.css produces, so the values
    /// asserted here are the ones the window will actually paint.
    /// </summary>
    private static ThemePalette CreatePalette(string? monoFontFamily = "MesloLGS Nerd Font Mono")
    {
        ThemeColor surface = ThemeColor.FromRgb(0x1a, 0x20, 0x2c);

        return new ThemePalette
        {
            IsLight = false,
            MonoFontFamily = monoFontFamily,
            WindowBg = ThemeColor.FromRgb(0x11, 0x11, 0x15),
            ChromeBg = surface,
            ToolbarBg = surface,
            RowAltBg = surface,
            TrackBg = surface,
            Hairline = ThemeColor.FromRgb(0x31, 0x40, 0x4c),
            RowHairline = ThemeColor.FromRgb(0x43, 0x55, 0x65),
            // deliberately translucent, to prove alpha survives the conversion
            TextPrimary = new ThemeColor(0xc5, 0xf9, 0xff, 127),
            TextBody = ThemeColor.FromRgb(0x99, 0xdc, 0xdc),
            TextMuted = ThemeColor.FromRgb(0x6d, 0xb4, 0xb4),
            TextFaint = ThemeColor.FromRgb(0x55, 0x6e, 0x7e),
            AccentLine = ThemeColor.FromRgb(0x3a, 0x91, 0xc7),
            AccentText = ThemeColor.FromRgb(0x58, 0xc0, 0xff),
            BarFill = ThemeColor.FromRgb(0x3a, 0x91, 0xc7),
            BarFillToday = ThemeColor.FromRgb(0x58, 0xc0, 0xff),
            Ok = ThemeColor.FromRgb(0x73, 0xc7, 0x5f),
            Caution = ThemeColor.FromRgb(0xf9, 0xe2, 0xaf),
            Danger = ThemeColor.FromRgb(0xf3, 0x53, 0x53),
            OkText = ThemeColor.FromRgb(0x8a, 0xd0, 0x7c),
            CautionText = ThemeColor.FromRgb(0xfa, 0xe7, 0xbe),
            WarnBg = ThemeColor.FromRgb(0x2c, 0x2a, 0x27),
            WarnBorder = ThemeColor.FromRgb(0x4a, 0x46, 0x3a),
            WarnText = ThemeColor.FromRgb(0xfa, 0xe9, 0xc4),
        };
    }
}
