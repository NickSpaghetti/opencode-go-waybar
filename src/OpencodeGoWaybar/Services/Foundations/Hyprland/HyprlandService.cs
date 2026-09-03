using System.Runtime.CompilerServices;
using OpencodeGoWaybar.Brokers.Hyprland;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Hyprland;

namespace OpencodeGoWaybar.Services.Foundations.Hyprland;

/// <summary>
/// Turns the compositor's IPC payloads into the local answers the visibility rule
/// needs: which workspace is focused, where every window sits, and a stream that
/// says something changed.
///
/// "Hyprland is not running" is modelled as an absent answer rather than an
/// exception. It is the normal state on any other compositor, and a Waybar module
/// that threw there would report a failure for a machine that is working fine.
/// </summary>
internal sealed partial class HyprlandService : IHyprlandService
{
    private readonly IHyprlandBroker _hyprlandBroker;
    private readonly ILoggingBroker _loggingBroker;

    public HyprlandService(
        IHyprlandBroker hyprlandBroker,
        ILoggingBroker loggingBroker)
    {
        _hyprlandBroker = hyprlandBroker;
        _loggingBroker = loggingBroker;
    }

    public ValueTask<int?> RetrieveActiveWorkspaceIdAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(RetrieveActiveWorkspaceIdCoreAsync, cancellationToken);

    public ValueTask<IReadOnlyList<HyprlandWindow>> RetrieveWindowsAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(RetrieveWindowsCoreAsync, cancellationToken);

    public ValueTask<HyprlandPlacement> RetrievePlacementAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(RetrievePlacementCoreAsync, cancellationToken);

    /// <summary>
    /// Passes every event through untouched. See the interface for why there is
    /// no filter here; the short version is that the set of names to filter on
    /// cannot be obtained from Hyprland, so hard-coding one would decide, at
    /// authoring time, which future compositor changes this module is allowed to
    /// notice.
    /// </summary>
    public async IAsyncEnumerable<string> StreamEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_hyprlandBroker.IsHyprlandPresent)
        {
            yield break;
        }

        await foreach (var line in _hyprlandBroker.StreamEventsAsync(cancellationToken))
        {
            // Each line is "name>>payload". Only the name is surfaced: the payload
            // is the part whose shape differs per event and changes between
            // versions, and nothing here needs it.
            var separator = line.IndexOf(">>", StringComparison.Ordinal);

            yield return separator < 0 ? line : line[..separator];
        }
    }

    private async ValueTask<int?> RetrieveActiveWorkspaceIdCoreAsync(CancellationToken cancellationToken)
    {
        if (!_hyprlandBroker.IsHyprlandPresent)
        {
            return null;
        }

        HyprlandWorkspace? workspace = await _hyprlandBroker.GetActiveWorkspaceAsync(cancellationToken);

        return workspace?.Id;
    }

    private async ValueTask<IReadOnlyList<HyprlandWindow>> RetrieveWindowsCoreAsync(
        CancellationToken cancellationToken)
    {
        if (!_hyprlandBroker.IsHyprlandPresent)
        {
            return [];
        }

        IReadOnlyList<HyprlandClient>? clients = await _hyprlandBroker.GetClientsAsync(cancellationToken);
        ValidateClients(clients);

        // A client with no workspace is not misread as workspace zero — it is a
        // window Hyprland has not placed, and it cannot answer the question.
        return clients
            .Where(client => client.Workspace is not null)
            .Select(client => new HyprlandWindow(client.Pid, client.Workspace!.Id))
            .ToArray();
    }

    /// <summary>
    /// Two reads rather than one: Hyprland has no single call for both, so a
    /// workspace switch landing between them yields a torn reading. That is
    /// tolerable here because this value is only ever compared — a torn reading
    /// differs from the last one, which triggers the render it was going to
    /// trigger anyway, and the next reading is whole.
    /// </summary>
    private async ValueTask<HyprlandPlacement> RetrievePlacementCoreAsync(CancellationToken cancellationToken)
    {
        if (!_hyprlandBroker.IsHyprlandPresent)
        {
            return HyprlandPlacement.None;
        }

        var activeWorkspaceId = await RetrieveActiveWorkspaceIdCoreAsync(cancellationToken);
        IReadOnlyList<HyprlandWindow> windows = await RetrieveWindowsCoreAsync(cancellationToken);

        return new HyprlandPlacement(activeWorkspaceId, windows);
    }
}
