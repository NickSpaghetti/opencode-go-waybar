#:property TargetFramework=net10.0
#:property Nullable=enable
#:property InvariantGlobalization=true

// Synthetic ACP client (e2e layer 1), written as a .NET file-based app so it
// runs straight from source with `dotnet run AcpClient.cs` — no csproj, no
// Node, nothing in the image the SDK does not already provide.
//
// It spawns an opencode binary as `<bin> acp` and performs the Agent Client
// Protocol initialization handshake over stdio exactly the way Zed, JetBrains
// and the Neovim plugins do: newline-delimited JSON-RPC 2.0 on the child's
// stdin/stdout. See https://agentclientprotocol.com/protocol/initialization
//
// Emits one JSON object on stdout for the E2E suite to assert on:
//   {"pid":1234,"initialize":{ ...agent result... }}
//
// Usage:
//   dotnet run AcpClient.cs -- --bin <path> [--hold-ms 0] [--timeout-ms 30000]
//
// --hold-ms keeps the agent alive after the handshake so the caller can observe
// it in the process table while it is still running.

using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

var arguments = args;

string? ReadArgument(string name)
{
    var index = Array.IndexOf(arguments, name);

    return index == -1 || index + 1 >= arguments.Length
        ? null
        : arguments[index + 1];
}

var binary = ReadArgument("--bin");
var holdMilliseconds = int.Parse(ReadArgument("--hold-ms") ?? "0");
var timeoutMilliseconds = int.Parse(ReadArgument("--timeout-ms") ?? "30000");

if (string.IsNullOrWhiteSpace(binary))
{
    Console.Error.WriteLine("AcpClient: --bin <path to opencode> is required.");
    return 2;
}

var startInfo = new ProcessStartInfo(binary)
{
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};

startInfo.ArgumentList.Add("acp");

Process? agent;

try
{
    agent = Process.Start(startInfo);
}
catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
{
    Console.Error.WriteLine($"AcpClient: could not spawn '{binary} acp': {exception.Message}");
    return 1;
}

if (agent is null)
{
    Console.Error.WriteLine($"AcpClient: could not spawn '{binary} acp'.");
    return 1;
}

using var _ = agent;

var standardError = agent.StandardError.ReadToEndAsync();

int Fail(string message)
{
    TryKillAgent();

    var diagnostics = standardError.IsCompletedSuccessfully && standardError.Result.Length > 0
        ? $"{Environment.NewLine}--- agent stderr ---{Environment.NewLine}{standardError.Result}"
        : string.Empty;

    Console.Error.WriteLine($"AcpClient: {message}{diagnostics}");

    return 1;
}

void TryKillAgent()
{
    try
    {
        if (!agent.HasExited)
        {
            agent.Kill(entireProcessTree: true);
        }
    }
    catch (InvalidOperationException ex)
    {
        // The agent already exited; nothing to kill.
        Console.Error.WriteLine(ex);
    }
}

// Built as a JsonObject rather than an anonymous type: file-based apps ship
// with reflection-based serialization disabled, and this keeps the request
// AOT-clean and symmetric with how the response is read below.
var initializeRequest = new JsonObject
{
    ["jsonrpc"] = "2.0",
    ["id"] = 0,
    ["method"] = "initialize",
    ["params"] = new JsonObject
    {
        ["protocolVersion"] = 1,
        ["clientCapabilities"] = new JsonObject
        {
            ["fs"] = new JsonObject { ["readTextFile"] = true, ["writeTextFile"] = true },
            ["terminal"] = true,
        },
        ["clientInfo"] = new JsonObject
        {
            ["name"] = "opencode-go-waybar-e2e",
            ["title"] = "opencode-go-waybar E2E probe",
            ["version"] = "1.0.0",
        },
    },
};

await agent.StandardInput.WriteLineAsync(initializeRequest.ToJsonString());
await agent.StandardInput.FlushAsync();

using var timeout = new CancellationTokenSource(timeoutMilliseconds);

try
{
    while (await agent.StandardOutput.ReadLineAsync(timeout.Token) is { } line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            continue;
        }

        JsonNode? message;

        try
        {
            message = JsonNode.Parse(line);
        }
        catch (JsonException)
        {
            continue; // Not a JSON-RPC frame — ignore banner noise.
        }

        if (message?["id"]?.GetValue<int>() != 0)
        {
            continue; // A notification, or a response to something else.
        }

        if (message["error"] is { } error)
        {
            return Fail($"agent returned an error: {error.ToJsonString()}");
        }

        var report = new JsonObject
        {
            ["pid"] = agent.Id,
            ["initialize"] = message["result"]?.DeepClone(),
        };

        Console.WriteLine(report.ToJsonString());

        await Task.Delay(holdMilliseconds);
        TryKillAgent();

        return 0;
    }
}
catch (OperationCanceledException)
{
    return Fail($"no initialize response within {timeoutMilliseconds}ms.");
}

return Fail($"agent exited before answering initialize (exit code {agent.ExitCode}).");
