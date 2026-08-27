using NSubstitute;
using OpencodeGoWaybar.Exposers.Themes;
using OpencodeGoWaybar.Models.Themes;
using OpencodeGoWaybar.Services.Foundations.Themes;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Exposers.Themes;

/// <summary>
/// The exposer is pure mapping (§3.0.0.0), so there is very little to test: that
/// both routines reach the service and nothing else happens on the way. Deciding
/// whether a palette changed moved down to the service, and its tests moved with
/// it.
/// </summary>
public sealed class ThemeExposerTests
{
    [Fact]
    public async Task ShouldExposeThePaletteTheServiceProducesAsync()
    {
        // given
        ThemePalette palette = CreatePalette();
        var themeService = Substitute.For<IThemeService>();
        themeService.RetrievePaletteAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<ThemePalette?>(palette));

        // when
        ThemePalette? exposed = await new ThemeExposer(themeService)
            .ExposePaletteAsync(CancellationToken.None);

        // then
        Assert.Same(palette, exposed);
    }

    [Fact]
    public async Task ShouldExposeNoPaletteWhenThereIsNoStyleSheetAsync()
    {
        // given
        var themeService = Substitute.For<IThemeService>();
        themeService.RetrievePaletteAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<ThemePalette?>(null));

        // when
        ThemePalette? exposed = await new ThemeExposer(themeService)
            .ExposePaletteAsync(CancellationToken.None);

        // then
        Assert.Null(exposed);
    }

    [Fact]
    public void ShouldHandTheConsumersCallbackStraightToTheService()
    {
        // given
        var themeService = Substitute.For<IThemeService>();
        Action<ThemePalette> onChanged = _ => { };

        // when
        new ThemeExposer(themeService).WatchPalette(onChanged);

        // then the very same delegate arrives, unwrapped
        themeService.Received(1).WatchPalette(onChanged);
    }

    private static ThemePalette CreatePalette()
    {
        ThemeColor color = ThemeColor.FromRgb(0x11, 0x11, 0x15);

        return new ThemePalette
        {
            IsLight = false,
            MonoFontFamily = "MesloLGS Nerd Font Mono",
            WindowBg = color,
            ChromeBg = color,
            ToolbarBg = color,
            RowAltBg = color,
            TrackBg = color,
            Hairline = color,
            RowHairline = color,
            TextPrimary = color,
            TextBody = color,
            TextMuted = color,
            TextFaint = color,
            AccentLine = color,
            AccentText = color,
            BarFill = color,
            BarFillToday = color,
            Ok = color,
            Caution = color,
            Danger = color,
            OkText = color,
            CautionText = color,
            WarnBg = color,
            WarnBorder = color,
            WarnText = color,
        };
    }
}
