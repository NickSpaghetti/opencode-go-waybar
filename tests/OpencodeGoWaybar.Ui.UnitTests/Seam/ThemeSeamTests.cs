using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using OpencodeGoWaybar.Configurations;
using OpencodeGoWaybar.Exposers.Themes;
using OpencodeGoWaybar.Models.Themes;
using OpencodeGoWaybar.Ui.Converters;
using OpencodeGoWaybar.Ui.Theming;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.Seam;

/// <summary>
/// The whole seam in one pass: configuration picks up a stylesheet path, the
/// broker reads it, the service parses it, the exposer publishes it, and the
/// applier turns it into brushes. Every layer between the UI and the filesystem
/// is real here — only the stylesheet is a fixture.
///
/// This is the closest thing to the running window that can be checked without a
/// display attached.
/// </summary>
[Collection("ThemeSeam")]
public sealed class ThemeSeamTests : IDisposable
{
    private const string StyleCss = """
        /* @import "mocha.css"; */
        @import "honkadaloonga.css";

        * {
            font-family: "MesloLGS Nerd Font Mono Bold";
            font-size: 14px;
        }
        """;

    private const string HonkadaloongaCss = """
        @define-color base #111115;
        @define-color surface0 #1a202c;
        @define-color overlay0 #31404c;
        @define-color overlay2 #556e7e;
        @define-color text #c5f9ff;
        @define-color subtext1 #99dcdc;
        @define-color subtext0 #6db4b4;
        @define-color sky #58c0ff;
        @define-color sapphire #3a91c7;
        @define-color green #73c75f;
        @define-color yellow #f9e2af;
        @define-color red #f35353;
        """;

    private const string MochaCss = "@define-color base #1e1e2e;\n@define-color text #cdd6f4;";

    private readonly string directoryPath = Path.Combine(
        Path.GetTempPath(),
        $"opencode-go-seam-{Guid.NewGuid():N}");

    private readonly string? previousStylePath =
        Environment.GetEnvironmentVariable("OPENCODE_GO_WaybarStylePath");

    public ThemeSeamTests()
    {
        Directory.CreateDirectory(this.directoryPath);
        File.WriteAllText(Path.Combine(this.directoryPath, "style.css"), StyleCss);
        File.WriteAllText(Path.Combine(this.directoryPath, "honkadaloonga.css"), HonkadaloongaCss);
        File.WriteAllText(Path.Combine(this.directoryPath, "mocha.css"), MochaCss);

        Environment.SetEnvironmentVariable(
            "OPENCODE_GO_WaybarStylePath",
            Path.Combine(this.directoryPath, "style.css"));
    }

    [Fact]
    public async Task ShouldPaintTheWindowInTheBarsOwnColoursAsync()
    {
        // given the real composition, reading the real files
        using var serviceProvider = UsageComposition.BuildServiceProvider();
        var themeExposer = serviceProvider.GetRequiredService<IThemeExposer>();

        var resources = new ResourceDictionary();
        resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            ["WindowBg"] = new SolidColorBrush(Colors.Magenta),
        };

        // when
        ThemePalette? palette = await themeExposer.ExposePaletteAsync(CancellationToken.None);
        Assert.NotNull(palette);
        PaletteApplier.Apply(resources, palette);

        // then the live import won and the shipped fallback lost
        AssertBrush(resources, "WindowBg", "#111115");
        AssertBrush(resources, "ChromeBg", "#1a202c");
        AssertBrush(resources, "Hairline", "#31404c");
        AssertBrush(resources, "TextPrimary", "#c5f9ff");
        AssertBrush(resources, "TextBody", "#99dcdc");
        AssertBrush(resources, "TextFaint", "#556e7e");
        AssertBrush(resources, "AccentText", "#58c0ff");
        AssertBrush(resources, "Ok", "#73c75f");
        AssertBrush(resources, "Caution", "#f9e2af");
        AssertBrush(resources, "Danger", "#f35353");

        // and the bar's font came along
        var mono = Assert.IsType<FontFamily>(resources["Mono"]);
        Assert.Contains("MesloLGS Nerd Font Mono Bold", mono.ToString(), StringComparison.Ordinal);

        Assert.False(palette.IsLight);
    }

    private static void AssertBrush(IResourceDictionary resources, string key, string expectedHex)
    {
        IBrush? resolved = PaletteLookup.ResolveBrush(resources, ThemeVariant.Dark, key);
        var brush = Assert.IsType<SolidColorBrush>(resolved);

        var actualHex =
            $"#{brush.Color.R:x2}{brush.Color.G:x2}{brush.Color.B:x2}";

        Assert.Equal(expectedHex, actualHex);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(
            "OPENCODE_GO_WaybarStylePath",
            this.previousStylePath);

        if (Directory.Exists(this.directoryPath))
        {
            Directory.Delete(this.directoryPath, recursive: true);
        }
    }
}
