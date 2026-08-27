using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using OpencodeGoWaybar.Ui.Converters;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.Converters;

public sealed class PaletteKeyConverterTests
{
    [Theory]
    [InlineData(true, "BarFillToday")]
    [InlineData(false, "BarFill")]
    [InlineData(null, "BarFill")]
    public void ShouldPickOutTodaysColumn(bool? isToday, string expectedKey) =>
        Assert.Equal(expectedKey, BarBrushConverter.ToBarKey(isToday));

    [Theory]
    // the saturated ring colours are unreadable as small text, so Ok and Caution
    // map to softer variants; Danger red already reads well
    [InlineData("Ok", "OkText")]
    [InlineData("Caution", "CautionText")]
    [InlineData("Danger", "Danger")]
    [InlineData("TextFaint", "TextPrimary")]
    [InlineData(null, "TextPrimary")]
    public void ShouldTintTheNumberInsideTheRing(string? brushKey, string expectedKey) =>
        Assert.Equal(expectedKey, RingTextConverter.ToTextKey(brushKey));

    [Fact]
    public void ShouldResolveABrushFromTheResourceDictionary()
    {
        // given
        var expected = new SolidColorBrush(Colors.Red);
        var resources = new ResourceDictionary { ["Danger"] = expected };

        // when
        IBrush? resolved = PaletteLookup.ResolveBrush(resources, ThemeVariant.Dark, "Danger");

        // then
        Assert.Same(expected, resolved);
    }

    [Fact]
    public void ShouldResolveABrushDeclaredForTheActiveThemeVariant()
    {
        // given
        var dark = new SolidColorBrush(Colors.Black);
        var resources = new ResourceDictionary();
        resources.ThemeDictionaries[ThemeVariant.Dark] =
            new ResourceDictionary { ["WindowBg"] = dark };

        // when
        IBrush? resolved = PaletteLookup.ResolveBrush(resources, ThemeVariant.Dark, "WindowBg");

        // then
        Assert.Same(dark, resolved);
    }

    [Fact]
    public void ShouldResolveNoBrushWhenTheKeyIsAbsentOrNotABrush()
    {
        // given a dictionary holding a non-brush under a palette key
        var resources = new ResourceDictionary { ["Mono"] = new FontFamily("monospace") };

        // then
        Assert.Null(PaletteLookup.ResolveBrush(resources, ThemeVariant.Dark, "Absent"));
        Assert.Null(PaletteLookup.ResolveBrush(resources, ThemeVariant.Dark, "Mono"));
        Assert.Null(PaletteLookup.ResolveBrush(resources, ThemeVariant.Dark, null));
        Assert.Null(PaletteLookup.ResolveBrush(null, ThemeVariant.Dark, "Danger"));
    }
}
