using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpencodeGoWaybar.Brokers.Hyprland;

internal sealed partial class HyprlandBroker
{
    /// <summary>
    /// The event socket is not the query socket with a different command on it:
    /// nothing is ever written, and the compositor holds the connection open and
    /// pushes lines until it goes away. That is why this does not go through
    /// <c>RequestAsync</c>.
    /// </summary>
    public async IAsyncEnumerable<string> StreamEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var socketPath = ResolveEventSocketPath();

        if (socketPath is null)
        {
            yield break;
        }

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken);

        await using var stream = new NetworkStream(socket, ownsSocket: false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        // Ends when the compositor closes the socket — on a Hyprland restart, say.
        // Reconnecting is the caller's decision, not the broker's.
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            yield return line;
        }
    }
}
