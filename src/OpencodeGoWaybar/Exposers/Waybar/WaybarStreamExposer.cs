using System.Runtime.CompilerServices;
using System.Threading.Channels;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Hyprland;
using OpencodeGoWaybar.Models.Hyprland.Exceptions;
using OpencodeGoWaybar.Services.Foundations.Hyprland;

namespace OpencodeGoWaybar.Exposers.Waybar;

/// <summary>
/// The long-running form of the module. Polling every few seconds was adequate
/// while visibility only changed when OpenCode started or stopped; once the bar
/// began following the focused workspace, the same delay landed on an action the
/// user takes constantly, and a poll fast enough to hide it would spend nearly
/// all of its work discovering that nothing had changed.
///
/// So the render is driven by the compositor instead, through two wake-ups.
///
/// An event wake-up means Hyprland reported something — anything. Rather than
/// judge the event by name, which would mean hard-coding a list Hyprland offers
/// no way to obtain, it re-reads the placement and compares. Most events change
/// nothing the module reads and stop here, including the several a second a
/// terminal emits while animating its title.
///
/// A tick wake-up always renders. It covers what the compositor cannot report:
/// OpenCode exiting inside a terminal that stays open, and a cached API answer
/// going stale.
/// </summary>
internal sealed class WaybarStreamExposer(
    IWaybarExposer waybarExposer,
    IHyprlandService hyprlandService,
    ILoggingBroker loggingBroker,
    WaybarStreamCadence? cadence = null) : IWaybarStreamExposer
{
    private readonly WaybarStreamCadence _cadence = cadence ?? WaybarStreamCadence.Default;

    public async IAsyncEnumerable<string> ExposeStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var wakeUps = new WakeUpSignal();

        using var producers = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task eventPump = PumpEventsAsync(wakeUps, producers.Token);
        Task tickPump = PumpTicksAsync(wakeUps, producers.Token);

        try
        {
            string? lastPayload = null;
            HyprlandPlacement? lastPlacement = null;
            var isFirstPass = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                // A tick is never skipped. Anything else has to justify itself by
                // having actually moved something.
                var tickIsDue = wakeUps.ConsumeTickDue();
                var mustRender = isFirstPass || tickIsDue;

                HyprlandPlacement? placement = await TryRetrievePlacementAsync(cancellationToken);

                if (!mustRender && placement is not null && lastPlacement is not null
                    && placement.Matches(lastPlacement))
                {
                    // Nothing the module reads has changed, so the expensive part —
                    // the cache file, opencode's database, sometimes the API — is
                    // not touched at all.
                    if (!await WaitForWakeUpAsync(wakeUps, cancellationToken))
                    {
                        yield break;
                    }

                    continue;
                }

                lastPlacement = placement ?? lastPlacement;
                isFirstPass = false;

                var payload = await RenderAsync(cancellationToken);

                // Waybar redraws whatever it is handed, so an unchanged payload is
                // pure churn: a workspace switch between two workspaces that both
                // lack a session moves the placement without moving the answer.
                if (payload != lastPayload)
                {
                    lastPayload = payload;

                    yield return payload;
                }

                if (!await WaitForWakeUpAsync(wakeUps, cancellationToken))
                {
                    yield break;
                }
            }
        }
        finally
        {
            await producers.CancelAsync();

            // The pumps hold nothing the caller can see; they are awaited only so
            // that returning from here normally means they have actually stopped.
            // The wait is bounded because it cannot be allowed to fail that way:
            // a pump parked on a read that never returns would otherwise keep the
            // process alive after Waybar has asked it to go.
            await Task.WhenAny(
                Task.WhenAll(eventPump, tickPump),
                Task.Delay(_cadence.ShutdownGrace, CancellationToken.None))
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The current placement, or null when it could not be read. Null means "no
    /// opinion", which the caller treats as a reason to render: a compositor that
    /// will not answer must not be able to wedge the bar on a stale payload.
    /// </summary>
    private async ValueTask<HyprlandPlacement?> TryRetrievePlacementAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await hyprlandService.RetrievePlacementAsync(cancellationToken);
        }
        catch (Exception exception) when (exception
            is HyprlandUnavailableException
            or HyprlandResponseException
            or HyprlandServiceException)
        {
            // Already logged by the service.
            return null;
        }
    }

    /// <summary>
    /// Blocks until something asks for a render, then lets the burst behind it
    /// settle and takes the whole burst as a single wake-up.
    /// </summary>
    private async ValueTask<bool> WaitForWakeUpAsync(
        WakeUpSignal wakeUps,
        CancellationToken cancellationToken)
    {
        try
        {
            await wakeUps.WaitAsync(cancellationToken);
            await Task.Delay(_cadence.Debounce, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        wakeUps.Drain();

        return true;
    }

    private async ValueTask<string> RenderAsync(CancellationToken cancellationToken)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(_cadence.RenderTimeout);

        // The exposer renders a failure rather than throwing it, so a bad poll
        // costs this payload and not the process.
        return await waybarExposer.ExposeAsync(budget.Token);
    }

    /// <summary>
    /// Follows the compositor for as long as it is there, re-attaching when the
    /// stream ends. Hyprland closes it on restart, and the module has to survive
    /// that: a bar that stopped updating until the next login would be worse than
    /// the polling this replaced.
    /// </summary>
    private async Task PumpEventsAsync(WakeUpSignal wakeUps, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var sawEvents = false;

            try
            {
                await foreach (var _ in hyprlandService.StreamEventsAsync(cancellationToken))
                {
                    sawEvents = true;
                    wakeUps.SignalEvent();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                await loggingBroker.LogErrorAsync(exception);
            }

            // A stream that ends without ever yielding means there is no compositor
            // to follow — another window manager, or none. That is a supported
            // state, so it waits longer between attempts and asks for nothing; the
            // tick alone keeps the module correct.
            TimeSpan delay = sawEvents ? _cadence.ReconnectDelay : _cadence.IdleReconnectDelay;

            if (!await DelayAsync(delay, cancellationToken))
            {
                return;
            }

            if (sawEvents)
            {
                // Hyprland restarting can leave a different layout behind, so
                // re-attaching is itself worth a look.
                wakeUps.SignalEvent();
            }
        }
    }

    private async Task PumpTicksAsync(WakeUpSignal wakeUps, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_cadence.Tick);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                wakeUps.SignalTick();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async ValueTask<bool> DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// One "render again" nudge, plus a separate latch for whether a tick is owed.
    ///
    /// The nudge is a one-slot channel that drops writes when full: every wake-up
    /// says the same thing, so a backlog of them carries no more information than
    /// a single one, and a producer must never block on a slow render.
    ///
    /// The tick cannot ride in that channel. Under a stream of events the slot is
    /// almost always occupied, so a tick written into it would be dropped — and
    /// the one wake-up that must never be dropped is the one that refreshes usage
    /// and notices OpenCode exiting. It gets its own latch, which a dropped nudge
    /// cannot clear.
    /// </summary>
    private sealed class WakeUpSignal
    {
        private readonly Channel<byte> _nudges = Channel.CreateBounded<byte>(
            new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

        private int _tickIsDue;

        public void SignalEvent() => _nudges.Writer.TryWrite(0);

        public void SignalTick()
        {
            Interlocked.Exchange(ref _tickIsDue, 1);
            _nudges.Writer.TryWrite(0);
        }

        public bool ConsumeTickDue() => Interlocked.Exchange(ref _tickIsDue, 0) == 1;

        public ValueTask<byte> WaitAsync(CancellationToken cancellationToken) =>
            _nudges.Reader.ReadAsync(cancellationToken);

        public void Drain()
        {
            while (_nudges.Reader.TryRead(out _))
            {
            }
        }
    }
}
