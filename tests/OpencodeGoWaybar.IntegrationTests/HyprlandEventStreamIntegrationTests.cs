using Microsoft.Extensions.Logging.Abstractions;
using OpencodeGoWaybar.Brokers.Hyprland;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Services.Foundations.Hyprland;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.IntegrationTests;

/// <summary>
/// The event socket against the live compositor. Watch mode rests on this: if the
/// stream cannot be opened, or closes the moment it is, the module silently falls
/// back to its slow tick and the instant response it exists to provide is gone
/// with nothing failing anywhere to say so.
///
/// What these cases assert is that the connection is real and stays open. They do
/// not provoke events and then wait for them: driving the compositor from a test
/// means dispatching through whatever front end the machine happens to have, and
/// a test that only passes when someone switches workspaces by hand is worse than
/// no test at all. The wake-up-to-payload path is proved deterministically in
/// WaybarStreamExposerTests, where the event stream is a channel the test writes.
/// </summary>
[Trait("Tier", "Integration")]
public sealed class HyprlandEventStreamIntegrationTests(ITestOutputHelper output)
{
    private static bool IsHyprlandRunning => new HyprlandBroker().IsHyprlandPresent;

    [Fact]
    public async Task ShouldYieldNothingWhenHyprlandIsAbsentAsync()
    {
        if (IsHyprlandRunning)
        {
            output.WriteLine("Hyprland is running; the absent-compositor path cannot be reached here.");

            return;
        }

        // given
        using var timeout = E2eTimeout.Create();

        // when
        var events = new List<string>();

        await foreach (var name in CreateService().StreamEventsAsync(timeout.Token))
        {
            events.Add(name);
        }

        // then the stream ends rather than hanging, so watch mode falls through to
        // its tick instead of waiting forever on a socket that is not there
        Assert.Empty(events);
    }

    /// <summary>
    /// A wrong socket path or a rejected connection shows up here as a stream that
    /// finishes immediately — the same shape as "no compositor", which is exactly
    /// why it needs its own assertion on a machine where a compositor is running.
    /// </summary>
    [Fact]
    public async Task ShouldHoldTheEventSocketOpenOnTheLiveCompositorAsync()
    {
        if (!IsHyprlandRunning)
        {
            output.WriteLine("No Hyprland socket for this session.");

            return;
        }

        // given
        using var listenWindow = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var completedOnItsOwn = true;

        // when
        try
        {
            await foreach (var name in CreateService().StreamEventsAsync(listenWindow.Token))
            {
                output.WriteLine($"observed: {name}");
            }
        }
        catch (OperationCanceledException)
        {
            // Still listening when the window closed, which is the healthy outcome.
            completedOnItsOwn = false;
        }

        // then
        Assert.False(
            completedOnItsOwn,
            "The event stream ended by itself; the module would never hear a workspace change.");
    }

    private static HyprlandService CreateService() =>
        new(new HyprlandBroker(), new LoggingBroker(NullLogger<LoggingBroker>.Instance));
}
