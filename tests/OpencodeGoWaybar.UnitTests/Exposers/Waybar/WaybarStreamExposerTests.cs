using System.Threading.Channels;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Exposers.Waybar;
using OpencodeGoWaybar.Models.Hyprland;
using OpencodeGoWaybar.Models.Hyprland.Exceptions;
using OpencodeGoWaybar.Services.Foundations.Hyprland;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Exposers.Waybar;

/// <summary>
/// The watch loop, driven at millisecond cadence instead of its real one. What
/// matters here is not that a payload is correct — the exposer beneath decides
/// that — but that the loop renders when something moved, stays quiet when
/// nothing did, and keeps running through the ways the compositor can let it down.
///
/// The event stream is a channel the test writes to and the placement is a value
/// the test sets, so "Hyprland said something happened" and "something actually
/// changed" are separate lines of test code. They have to be: the whole design
/// rests on those two being independent, because the module cannot tell from an
/// event's name whether it mattered.
/// </summary>
public sealed class WaybarStreamExposerTests
{
    private static readonly WaybarStreamCadence FastCadence =
        new(
            Debounce: TimeSpan.FromMilliseconds(10),
            Tick: TimeSpan.FromMilliseconds(50),
            ReconnectDelay: TimeSpan.FromMilliseconds(10),
            IdleReconnectDelay: TimeSpan.FromMilliseconds(20),
            RenderTimeout: TimeSpan.FromSeconds(5),
            ShutdownGrace: TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// The same cadence with the tick pushed out of reach, for the cases that ask
    /// what events alone do. A tick renders unconditionally and would otherwise
    /// supply the very render those tests are asserting does not happen.
    /// </summary>
    private static readonly WaybarStreamCadence EventsOnlyCadence =
        FastCadence with { Tick = TimeSpan.FromMinutes(1) };

    private static readonly HyprlandPlacement OnWorkspaceThree =
        new(3, [new HyprlandWindow(200, 3)]);

    private static readonly HyprlandPlacement OnWorkspaceSeven =
        new(7, [new HyprlandWindow(200, 3)]);

    [Fact]
    public async Task ShouldEmitAPayloadBeforeAnythingHappensAsync()
    {
        // given a compositor that never says anything
        using var events = new EventStream();
        var exposer = CreateExposer(events, new PlacementSource(), CreateWaybarExposer("first"));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // when
        var payloads = await TakeAsync(exposer, count: 1, cancellation.Token);

        // then the bar is painted at startup rather than staying blank until the
        // first event, which might be minutes away
        Assert.Equal(["first"], payloads);
    }

    [Fact]
    public async Task ShouldRenderWhenAnEventMovedThePlacementAsync()
    {
        // given
        using var events = new EventStream();
        var placements = new PlacementSource(OnWorkspaceThree);
        var exposer = CreateExposer(events, placements, CreateWaybarExposer("hidden", "visible"));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // when the focus moves, under an event whose name means nothing to anyone
        var payloads = await TakeAsync(
            exposer,
            count: 2,
            cancellation.Token,
            afterFirst: () =>
            {
                placements.Set(OnWorkspaceSeven);
                events.Publish("an event nobody has a list of");
            });

        // then
        Assert.Equal(["hidden", "visible"], payloads);
    }

    /// <summary>
    /// This is what replaces the old list of event names. A terminal animating its
    /// title emits several events a second and moves nothing; none of them may
    /// reach the cache file, opencode's database, or the API.
    /// </summary>
    [Fact]
    public async Task ShouldNotRenderWhenAnEventMovedNothingAsync()
    {
        // given a placement that never changes
        using var events = new EventStream();
        var placements = new PlacementSource(OnWorkspaceThree);
        var waybarExposer = CreateWaybarExposer("first", "second");
        var exposer = CreateExposer(events, placements, waybarExposer, EventsOnlyCadence);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var payloads = new List<string>();

        // when a stream of events arrives that changes nothing
        Task consuming = ConsumeAsync(exposer, payloads, cancellation.Token);

        await Task.Delay(TimeSpan.FromMilliseconds(80), CancellationToken.None);

        for (var index = 0; index < 10; index++)
        {
            events.Publish("windowtitle");
            await Task.Delay(TimeSpan.FromMilliseconds(15), CancellationToken.None);
        }

        await cancellation.CancelAsync();
        await consuming;

        // then only the opening render happened. The placement was re-read, which
        // is the cheap half; the expensive half was never reached.
        Assert.Equal(["first"], payloads);
        await waybarExposer.Received(1).ExposeAsync(Arg.Any<CancellationToken>());
        Assert.True(placements.Reads > 1, "The placement should have been re-read for the events.");
    }

    [Fact]
    public async Task ShouldCoalesceABurstOfEventsIntoOneRenderAsync()
    {
        // given a move that emits several events at once
        using var events = new EventStream();
        var placements = new PlacementSource(OnWorkspaceThree);
        var exposer = CreateExposer(events, placements, CreateWaybarExposer("a", "b", "c", "d", "e"));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // when
        var payloads = await TakeAsync(
            exposer,
            count: 2,
            cancellation.Token,
            afterFirst: () =>
            {
                placements.Set(OnWorkspaceSeven);

                for (var index = 0; index < 5; index++)
                {
                    events.Publish("movewindow");
                }
            });

        // then the burst costs one render, so what follows "a" is the exposer's
        // second answer rather than its sixth
        Assert.Equal(["a", "b"], payloads);
    }

    [Fact]
    public async Task ShouldNotEmitWhenARereadProducesTheSamePayloadAsync()
    {
        // given a layout that keeps moving between workspaces that all lack a
        // session, so the answer never changes even though the placement does
        using var events = new EventStream();
        var placements = new PlacementSource(OnWorkspaceThree);
        var exposer = CreateExposer(events, placements, CreateWaybarExposer("hidden"), EventsOnlyCadence);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var payloads = new List<string>();

        // when
        Task consuming = ConsumeAsync(exposer, payloads, cancellation.Token);

        await Task.Delay(TimeSpan.FromMilliseconds(80), CancellationToken.None);

        for (var index = 0; index < 4; index++)
        {
            placements.Set(new HyprlandPlacement(index, [new HyprlandWindow(200, 3)]));
            events.Publish("workspace");
            await Task.Delay(TimeSpan.FromMilliseconds(40), CancellationToken.None);
        }

        await cancellation.CancelAsync();
        await consuming;

        // then the bar is handed one payload, not five identical ones
        Assert.Equal(["hidden"], payloads);
    }

    [Fact]
    public async Task ShouldKeepRenderingOnItsTickWhenThereIsNoCompositorAsync()
    {
        // given a stream that ends at once, as it does off Hyprland
        using var events = new EventStream();
        events.Complete();

        var exposer = CreateExposer(
            events,
            new PlacementSource(HyprlandPlacement.None),
            CreateWaybarExposer("first", "second"));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // when
        var payloads = await TakeAsync(exposer, count: 2, cancellation.Token);

        // then process starts and stops are still noticed, just not instantly —
        // and the placement never moved, so only the tick can explain this
        Assert.Equal(["first", "second"], payloads);
    }

    /// <summary>
    /// The tick carries the usage refresh and notices OpenCode exiting, so a
    /// chattering compositor must not be able to crowd it out. It travels on its
    /// own latch for exactly this reason.
    /// </summary>
    [Fact]
    public async Task ShouldStillRenderOnTickWhileEventsAreStreamingAsync()
    {
        // given constant events that move nothing
        using var events = new EventStream();
        var placements = new PlacementSource(OnWorkspaceThree);
        var exposer = CreateExposer(events, placements, CreateWaybarExposer("first", "second"));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var noise = new CancellationTokenSource();

        Task noiseMaker = Task.Run(
            async () =>
            {
                while (!noise.IsCancellationRequested)
                {
                    events.Publish("windowtitle");
                    await Task.Delay(TimeSpan.FromMilliseconds(5), CancellationToken.None);
                }
            },
            CancellationToken.None);

        // when
        var payloads = await TakeAsync(exposer, count: 2, cancellation.Token);

        await noise.CancelAsync();
        await noiseMaker;

        // then the tick got through
        Assert.Equal(["first", "second"], payloads);
    }

    [Fact]
    public async Task ShouldRenderWhenThePlacementCannotBeReadAsync()
    {
        // given a compositor that will not answer
        using var events = new EventStream();
        var placements = new PlacementSource(OnWorkspaceThree);
        placements.Fail(new HyprlandUnavailableException(new IOException("socket closed")));

        var exposer = CreateExposer(events, placements, CreateWaybarExposer("first", "second"));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // when
        var payloads = await TakeAsync(
            exposer,
            count: 2,
            cancellation.Token,
            afterFirst: () => events.Publish("workspace"));

        // then not knowing is a reason to render, never a reason to sit on a stale
        // payload
        Assert.Equal(["first", "second"], payloads);
    }

    [Fact]
    public async Task ShouldReattachAfterTheCompositorDropsTheStreamAsync()
    {
        // given a stream that yields once and then ends, as on a Hyprland restart
        var streams = new Queue<EventStream>();
        var first = new EventStream();
        var second = new EventStream();
        streams.Enqueue(first);
        streams.Enqueue(second);

        var placements = new PlacementSource(OnWorkspaceThree);
        var hyprlandService = Substitute.For<IHyprlandService>();

        hyprlandService.StreamEventsAsync(Arg.Any<CancellationToken>())
            .Returns(call => (streams.Count > 0 ? streams.Dequeue() : new EventStream())
                .ReadAllAsync(call.Arg<CancellationToken>()));

        hyprlandService.RetrievePlacementAsync(Arg.Any<CancellationToken>())
            .Returns(_ => placements.ReadAsync());

        var exposer = new WaybarStreamExposer(
            CreateWaybarExposer("first", "second", "third"),
            hyprlandService,
            Substitute.For<ILoggingBroker>(),
            FastCadence);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // when the first stream ends, the loop must attach to the next one
        var payloads = await TakeAsync(
            exposer,
            count: 2,
            cancellation.Token,
            afterFirst: () =>
            {
                placements.Set(OnWorkspaceSeven);
                first.Publish("workspace");
                first.Complete();
            });

        // then
        Assert.Equal(["first", "second"], payloads);

        first.Dispose();
        second.Dispose();
    }

    [Fact]
    public async Task ShouldStopWhenCancelledAsync()
    {
        // given
        using var events = new EventStream();
        var exposer = CreateExposer(events, new PlacementSource(), CreateWaybarExposer("only"));

        using var cancellation = new CancellationTokenSource();

        var payloads = new List<string>();

        // when
        Task consuming = Task.Run(
            async () =>
            {
                await foreach (var payload in exposer.ExposeStreamAsync(cancellation.Token))
                {
                    payloads.Add(payload);
                    await cancellation.CancelAsync();
                }
            },
            CancellationToken.None);

        // then the loop unwinds rather than hanging on its own producers
        await consuming.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.Equal(["only"], payloads);
    }

    private static Task ConsumeAsync(
        IWaybarStreamExposer exposer,
        List<string> payloads,
        CancellationToken cancellationToken) =>
        Task.Run(
            async () =>
            {
                await foreach (var payload in exposer.ExposeStreamAsync(cancellationToken))
                {
                    payloads.Add(payload);
                }
            },
            CancellationToken.None);

    private static async Task<List<string>> TakeAsync(
        IWaybarStreamExposer exposer,
        int count,
        CancellationToken cancellationToken,
        Action? afterFirst = null)
    {
        var payloads = new List<string>();

        await foreach (var payload in exposer.ExposeStreamAsync(cancellationToken))
        {
            payloads.Add(payload);

            if (payloads.Count == 1)
            {
                afterFirst?.Invoke();
            }

            if (payloads.Count >= count)
            {
                break;
            }
        }

        return payloads;
    }

    private static WaybarStreamExposer CreateExposer(
        EventStream events,
        PlacementSource placements,
        IWaybarExposer waybarExposer,
        WaybarStreamCadence? cadence = null)
    {
        var hyprlandService = Substitute.For<IHyprlandService>();

        hyprlandService.StreamEventsAsync(Arg.Any<CancellationToken>())
            .Returns(call => events.ReadAllAsync(call.Arg<CancellationToken>()));

        hyprlandService.RetrievePlacementAsync(Arg.Any<CancellationToken>())
            .Returns(_ => placements.ReadAsync());

        return new WaybarStreamExposer(
            waybarExposer,
            hyprlandService,
            Substitute.For<ILoggingBroker>(),
            cadence ?? FastCadence);
    }

    /// <summary>
    /// Renders the given payloads in order, then repeats the last — so a test says
    /// how many distinct answers the module has, and an extra render is a repeat
    /// rather than an index out of range.
    /// </summary>
    private static IWaybarExposer CreateWaybarExposer(params string[] payloads)
    {
        var waybarExposer = Substitute.For<IWaybarExposer>();
        var index = 0;

        waybarExposer.ExposeAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var payload = payloads[Math.Min(index, payloads.Length - 1)];
                index++;

                return ValueTask.FromResult(payload);
            });

        return waybarExposer;
    }

    /// <summary>What the compositor currently reports, as the test decides it.</summary>
    private sealed class PlacementSource(HyprlandPlacement? initial = null)
    {
        private HyprlandPlacement _current = initial ?? HyprlandPlacement.None;
        private Exception? _failure;
        private int _reads;

        public int Reads => Volatile.Read(ref _reads);

        public void Set(HyprlandPlacement placement) => Volatile.Write(ref _current, placement);

        public void Fail(Exception failure) => _failure = failure;

        public ValueTask<HyprlandPlacement> ReadAsync()
        {
            Interlocked.Increment(ref _reads);

            return _failure is null
                ? ValueTask.FromResult(Volatile.Read(ref _current))
                : ValueTask.FromException<HyprlandPlacement>(_failure);
        }
    }

    /// <summary>A compositor event stream the test drives by hand.</summary>
    private sealed class EventStream : IDisposable
    {
        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

        public void Publish(string eventName) => _channel.Writer.TryWrite(eventName);

        public void Complete() => _channel.Writer.TryComplete();

        public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);

        public void Dispose() => Complete();
    }
}
