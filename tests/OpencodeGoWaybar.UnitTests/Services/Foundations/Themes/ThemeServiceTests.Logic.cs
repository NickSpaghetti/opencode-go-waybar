using OpencodeGoWaybar.Brokers.Themes;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Themes;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Themes;

public sealed partial class ThemeServiceTests
{
    [Fact]
    public async Task ShouldRetrieveNoPaletteWhenTheStyleSheetIsAbsentAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker([]);

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then
        Assert.Null(palette);
        await loggingBroker.DidNotReceive().LogErrorAsync(Arg.Any<Exception>());
    }

    [Fact]
    public async Task ShouldIgnoreACommentedOutImportAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateRealisticThemeBroker();

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then the live import won, and the commented one was never even read
        Assert.NotNull(palette);
        Assert.Equal(Hex("#111115"), palette.WindowBg);
        Assert.NotEqual(Hex("#1e1e2e"), palette.WindowBg);
        await themeBroker.DidNotReceive()
            .ReadTextAsync("/waybar/mocha.css", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldMapNamedRolesFromTheActivePaletteAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateRealisticThemeBroker();

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then
        Assert.NotNull(palette);
        Assert.False(palette.IsLight);
        Assert.Equal(Hex("#111115"), palette.WindowBg);
        Assert.Equal(Hex("#1a202c"), palette.ChromeBg);
        Assert.Equal(Hex("#252b36"), palette.RowAltBg);
        Assert.Equal(Hex("#2e3540"), palette.TrackBg);
        Assert.Equal(Hex("#31404c"), palette.Hairline);
        Assert.Equal(Hex("#435565"), palette.RowHairline);
        Assert.Equal(Hex("#c5f9ff"), palette.TextPrimary);
        Assert.Equal(Hex("#99dcdc"), palette.TextBody);
        Assert.Equal(Hex("#6db4b4"), palette.TextMuted);
        Assert.Equal(Hex("#556e7e"), palette.TextFaint);
        Assert.Equal(Hex("#58c0ff"), palette.AccentText);
        Assert.Equal(Hex("#3a91c7"), palette.AccentLine);
        Assert.Equal(Hex("#73c75f"), palette.Ok);
        Assert.Equal(Hex("#f9e2af"), palette.Caution);
        Assert.Equal(Hex("#f35353"), palette.Danger);
    }

    [Fact]
    public async Task ShouldReadTheMonoFontFamilyFromTheStyleSheetAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateRealisticThemeBroker();

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then
        Assert.NotNull(palette);
        Assert.Equal("MesloLGS Nerd Font Mono Bold", palette.MonoFontFamily);
    }

    [Fact]
    public async Task ShouldLetTheImportingSheetOverrideAnImportedDefinitionAsync()
    {
        // given an import that says black and a host sheet that says white
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] = "@import \"palette.css\";\n@define-color base #ffffff;",
            ["/waybar/palette.css"] = "@define-color base #000000;\n@define-color text #cccccc;",
        });

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then the cascade puts imports first, so the host sheet wins
        Assert.NotNull(palette);
        Assert.Equal(Hex("#ffffff"), palette.WindowBg);
        Assert.True(palette.IsLight);
    }

    [Fact]
    public async Task ShouldResolveNestedImportsAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] = "@import \"one.css\";",
            ["/waybar/one.css"] = "@import \"two.css\";",
            ["/waybar/two.css"] = "@define-color base #102030;\n@define-color text #f0f0f0;",
        });

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then
        Assert.NotNull(palette);
        Assert.Equal(Hex("#102030"), palette.WindowBg);
    }

    [Fact]
    public async Task ShouldNotLoopOnACircularImportAsync()
    {
        // given two sheets that import each other
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] = "@import \"loop.css\";\n@define-color base #111111;",
            ["/waybar/loop.css"] = "@import \"style.css\";\n@define-color text #eeeeee;",
        });

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then it terminates with both sheets applied
        Assert.NotNull(palette);
        Assert.Equal(Hex("#111111"), palette.WindowBg);
        Assert.Equal(Hex("#eeeeee"), palette.TextPrimary);
    }

    [Fact]
    public async Task ShouldParseRgbAndRgbaValuesAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] =
                "@define-color base rgb(17, 17, 27);\n" +
                "@define-color text rgba(197, 249, 255, 0.5);",
        });

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then
        Assert.NotNull(palette);
        Assert.Equal(new ThemeColor(17, 17, 27, 255), palette.WindowBg);
        Assert.Equal(new ThemeColor(197, 249, 255, 127), palette.TextPrimary);
    }

    [Fact]
    public async Task ShouldAcceptForegroundAndBackgroundKeyNamesAsync()
    {
        // given a theme that names the pair the way Omarchy's colors.toml does
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] =
                "@define-color background #1a1b26;\n" +
                "@define-color foreground #a9b1d6;",
        });

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then
        Assert.NotNull(palette);
        Assert.Equal(Hex("#1a1b26"), palette.WindowBg);
        Assert.Equal(Hex("#a9b1d6"), palette.TextPrimary);
    }

    [Fact]
    public async Task ShouldTreatALightBackgroundAsALightPaletteAsync()
    {
        // given catppuccin-latte's pair
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] = "@define-color base #eff1f5;\n@define-color text #4c4f69;",
        });

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then
        Assert.NotNull(palette);
        Assert.True(palette.IsLight);
    }

    [Fact]
    public async Task ShouldDeriveMissingRolesFromTheBackgroundAndForegroundPairAsync()
    {
        // given the minimum a theme can define
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] = "@define-color base #111115;\n@define-color text #c5f9ff;",
        });

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then a hairline exists and sits between the two, rather than matching
        // either one or coming out unset
        Assert.NotNull(palette);
        Assert.NotEqual(palette.WindowBg, palette.Hairline);
        Assert.NotEqual(palette.TextPrimary, palette.Hairline);
        Assert.InRange(
            palette.Hairline.Luminance,
            palette.WindowBg.Luminance,
            palette.TextPrimary.Luminance);

        // and the text ramp fades from primary toward the background
        Assert.True(palette.TextBody.Luminance > palette.TextMuted.Luminance);
        Assert.True(palette.TextMuted.Luminance > palette.TextFaint.Luminance);
    }

    [Fact]
    public async Task ShouldIgnoreDefinitionsInsideBlockCommentsAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] =
                "/* @define-color base #ff0000;\n   spanning two lines */\n" +
                "@define-color base #00ff00;\n@define-color text #ffffff;",
        });

        // when
        ThemePalette? palette = await CreateService(themeBroker, loggingBroker)
            .RetrievePaletteAsync(CancellationToken.None);

        // then
        Assert.NotNull(palette);
        Assert.Equal(Hex("#00ff00"), palette.WindowBg);
    }

    [Fact]
    public async Task ShouldRaiseOnlyWhenTheStyleSheetsActuallyChangeThePaletteAsync()
    {
        // given a subscription and a handle on the broker's raw change signal
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var styleSheets = new Dictionary<string, string>
        {
            [StylePath] = "@define-color base #111115;\n@define-color text #c5f9ff;",
        };

        Action? raiseRawChange = null;
        var themeBroker = CreateThemeBroker(styleSheets);
        themeBroker.WatchStyleSheets(Arg.Any<string>(), Arg.Any<Action>())
            .Returns(callInfo =>
            {
                raiseRawChange = callInfo.ArgAt<Action>(1);

                return Substitute.For<IDisposable>();
            });

        using var service = CreateService(themeBroker, loggingBroker);
        await service.RetrievePaletteAsync(CancellationToken.None);

        var delivered = new List<ThemePalette>();
        service.WatchPalette(delivered.Add);
        Assert.NotNull(raiseRawChange);

        // when one save arrives as four filesystem events
        for (var burst = 0; burst < 4; burst++)
        {
            raiseRawChange();
        }

        await WaitForAsync(() => delivered.Count > 0);
        await Task.Delay(150);

        // then nothing is delivered, because nothing changed
        Assert.Empty(delivered);

        // and when the stylesheet genuinely changes
        styleSheets[StylePath] = "@define-color base #222226;\n@define-color text #c5f9ff;";
        raiseRawChange();

        await WaitForAsync(() => delivered.Count > 0);

        // then exactly one palette arrives
        Assert.Single(delivered);
        Assert.Equal(Hex("#222226"), delivered[0].WindowBg);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);

        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }
    }
}
