namespace OpencodeGoWaybar.Brokers.Hyprland;

internal partial interface IHyprlandBroker
{
    /// <summary>
    /// The compositor's event stream, one raw <c>name&gt;&gt;payload</c> line at a
    /// time, ending when Hyprland closes the connection or is absent altogether.
    /// This is a second socket from the query one: it is never asked anything, it
    /// only reports what happened.
    /// </summary>
    IAsyncEnumerable<string> StreamEventsAsync(CancellationToken cancellationToken);
}
