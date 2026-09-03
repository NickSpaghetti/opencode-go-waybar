using System.Net.Sockets;
using System.Text;

namespace OpencodeGoWaybar.Brokers.Hyprland;

/// <summary>
/// Speaks Hyprland's IPC protocol directly over its UNIX sockets rather than
/// shelling out to <c>hyprctl</c> — the sockets are the same interface
/// <c>hyprctl</c> itself uses, and the module reads them often enough that a
/// process spawn per read would be a poor way to pay for it.
///
/// This file holds what is true of the compositor rather than of anything it
/// reports: where its sockets are, whether it is there at all, and the one
/// request the query commands are built on. Each collection lives in its own
/// partial alongside.
/// </summary>
internal sealed partial class HyprlandBroker : IHyprlandBroker
{
    private const string InstanceSignatureVariable = "HYPRLAND_INSTANCE_SIGNATURE";
    private const string RuntimeDirectoryVariable = "XDG_RUNTIME_DIR";
    private const string QuerySocketName = ".socket.sock";
    private const string EventSocketName = ".socket2.sock";

    public bool IsHyprlandPresent => ResolveQuerySocketPath() is not null;

    /// <summary>
    /// One command, one connection: write the request, read until the compositor
    /// closes its side. The <c>j/</c> prefix asks for JSON.
    /// </summary>
    private static async ValueTask<string?> RequestAsync(string command, CancellationToken cancellationToken)
    {
        var socketPath = ResolveQuerySocketPath();

        if (socketPath is null)
        {
            return null;
        }

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken);

        await using var stream = new NetworkStream(socket, ownsSocket: false);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(command), cancellationToken);

        using var reader = new StreamReader(stream, Encoding.UTF8);

        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string? ResolveQuerySocketPath() => ResolveSocketPath(QuerySocketName);

    private static string? ResolveEventSocketPath() => ResolveSocketPath(EventSocketName);

    /// <summary>
    /// Where this session's sockets live. Hyprland moved them under
    /// <c>$XDG_RUNTIME_DIR</c> in 0.40; the <c>/tmp</c> location is still checked
    /// so an older compositor is not silently treated as "not Hyprland".
    /// </summary>
    private static string? ResolveSocketPath(string socketName)
    {
        var instanceSignature = Environment.GetEnvironmentVariable(InstanceSignatureVariable);

        if (string.IsNullOrEmpty(instanceSignature))
        {
            return null;
        }

        var runtimeDirectory = Environment.GetEnvironmentVariable(RuntimeDirectoryVariable);

        string[] candidates = string.IsNullOrEmpty(runtimeDirectory)
            ? [Path.Combine("/tmp", "hypr", instanceSignature, socketName)]
            :
            [
                Path.Combine(runtimeDirectory, "hypr", instanceSignature, socketName),
                Path.Combine("/tmp", "hypr", instanceSignature, socketName),
            ];

        return Array.Find(candidates, File.Exists);
    }
}
