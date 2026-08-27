using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Themes;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Themes;
using OpencodeGoWaybar.Services.Foundations.Themes;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Themes;

/// <summary>
/// The stylesheets here are the real files from a working Waybar config rather
/// than invented ones, because the shapes that break a naive parser are exactly
/// the shapes real configs have: a commented-out import sitting above the live
/// one, and semantic colour names instead of a plain foreground/background pair.
/// </summary>
public sealed partial class ThemeServiceTests
{
    private const string StylePath = "/waybar/style.css";

    /// <summary>
    /// The head of a real style.css. The first import is commented out and must
    /// stay that way: honouring it would repaint everything in the wrong palette
    /// with no error to show for it.
    /// </summary>
    private const string StyleCss = """
        /* @import "mocha.css"; */
        @import "honkadaloonga.css";

        /* ─────────────────────────────────────────────────────────────
           Compact Waybar · Catppuccin Mocha
           ───────────────────────────────────────────────────────────── */

        * {
            font-family: "MesloLGS Nerd Font Mono Bold";
            font-size: 14px;
            min-height: 0;
            font-weight: bold;
        }

        window#waybar {
            background-color: rgba(17, 17, 27, 0.90);
            color: @subtext1;
        }
        """;

    private const string HonkadaloongaCss = """
        @define-color base #111115;
        @define-color mantle #0e1216;
        @define-color crust #0d0d12;

        @define-color surface0 #1a202c;
        @define-color surface1 #252b36;
        @define-color surface2 #2e3540;

        @define-color overlay0 #31404c;
        @define-color overlay1 #435565;
        @define-color overlay2 #556e7e;

        @define-color text #c5f9ff;
        @define-color subtext2 #cdd6f4;
        @define-color subtext1 #99dcdc;
        @define-color subtext0 #6db4b4;

        @define-color teal #33a0a0;
        @define-color cyan #40e0d0;
        @define-color blue #3498db;
        @define-color sky #58c0ff;
        @define-color sapphire #3a91c7;
        @define-color lavender #6d93e8;

        @define-color rosewater #ffaa71;
        @define-color red #f35353;
        @define-color yellow #f9e2af;
        @define-color green #73c75f;
        """;

    /// <summary>Standard Catppuccin Mocha — the palette that must NOT win.</summary>
    private const string MochaCss = """
        @define-color base #1e1e2e;
        @define-color text #cdd6f4;
        @define-color overlay0 #6c7086;
        @define-color red #f38ba8;
        @define-color green #a6e3a1;
        @define-color yellow #f9e2af;
        @define-color sky #89dceb;
        """;

    private static ThemeService CreateService(
        IWaybarThemeBroker themeBroker,
        ILoggingBroker loggingBroker,
        string stylePath = StylePath)
    {
        var options = new OpenCodeGoOptions { WaybarStylePath = stylePath };

        return new ThemeService(themeBroker, loggingBroker, options);
    }

    /// <summary>
    /// The broker throws for a path it does not have, the way the real one does —
    /// deciding that a missing file means "no theme" is the service's job now.
    /// </summary>
    private static IWaybarThemeBroker CreateThemeBroker(Dictionary<string, string> styleSheets)
    {
        var themeBroker = Substitute.For<IWaybarThemeBroker>();

        themeBroker.ReadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var path = callInfo.ArgAt<string>(0);

                return styleSheets.TryGetValue(path, out var styleSheet)
                    ? ValueTask.FromResult(styleSheet!)
                    : throw new FileNotFoundException("no such stylesheet", path);
            });

        themeBroker.StyleSheetDirectoryExists(Arg.Any<string>()).Returns(true);

        themeBroker.WatchStyleSheets(Arg.Any<string>(), Arg.Any<Action>())
            .Returns(_ => Substitute.For<IDisposable>());

        return themeBroker;
    }

    private static IWaybarThemeBroker CreateRealisticThemeBroker() =>
        CreateThemeBroker(new Dictionary<string, string>
        {
            [StylePath] = StyleCss,
            ["/waybar/honkadaloonga.css"] = HonkadaloongaCss,
            ["/waybar/mocha.css"] = MochaCss,
        });

    private static ThemeColor Hex(string value) =>
        ThemeColor.FromRgb(
            Convert.ToByte(value.Substring(1, 2), 16),
            Convert.ToByte(value.Substring(3, 2), 16),
            Convert.ToByte(value.Substring(5, 2), 16));
}
