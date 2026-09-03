namespace OpencodeGoWaybar.Exposers.Waybar;

/// <summary>
/// The timings the watch loop runs on, gathered in one place so a test can drive
/// the loop in milliseconds instead of waiting out the real cadence. Nothing in
/// the module configures these — they are not user-facing knobs, and a bar that
/// re-rendered on a schedule the user chose would be back to polling.
/// </summary>
/// <param name="Debounce">
/// How long a burst of events is allowed to settle before the placement is
/// re-read. Moving a window emits several events at once, and a fast run through
/// several workspaces emits a burst per switch. This also sets the ceiling on how
/// often a noisy compositor can cost a placement query, so it is a little longer
/// than a debounce alone would need to be.
/// </param>
/// <param name="Tick">
/// The floor under everything the compositor cannot report: OpenCode exiting
/// inside a terminal that stays open, and a cached API answer going stale.
/// </param>
/// <param name="ReconnectDelay">How long to wait before re-attaching after the stream drops.</param>
/// <param name="IdleReconnectDelay">How long to wait between attempts when there is no compositor.</param>
/// <param name="RenderTimeout">The budget for one render, matching the one-shot module's.</param>
/// <param name="ShutdownGrace">
/// How long to let the producers unwind before returning anyway. Waybar
/// restarts this module by killing it, so a shutdown that could block on a
/// socket read that is not coming back would strand the process.
/// </param>
internal sealed record WaybarStreamCadence(
    TimeSpan Debounce,
    TimeSpan Tick,
    TimeSpan ReconnectDelay,
    TimeSpan IdleReconnectDelay,
    TimeSpan RenderTimeout,
    TimeSpan ShutdownGrace)
{
    public static WaybarStreamCadence Default { get; } =
        new(
            Debounce: TimeSpan.FromMilliseconds(120),
            Tick: TimeSpan.FromSeconds(5),
            ReconnectDelay: TimeSpan.FromSeconds(2),
            IdleReconnectDelay: TimeSpan.FromSeconds(30),
            RenderTimeout: TimeSpan.FromSeconds(10),
            ShutdownGrace: TimeSpan.FromSeconds(2));
}
