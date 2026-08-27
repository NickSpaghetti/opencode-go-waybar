using System.Diagnostics;
using System.Text.Json.Nodes;

namespace OpencodeGoWaybar.TestSupport;

/// <summary>
/// An opencode process running as an ACP agent, spawned the way an editor
/// spawns it: `&lt;binary&gt; acp`, JSON-RPC over stdio. The initialize handshake
/// is performed on start so the agent is known to be live before a test looks
/// for it in the process table.
/// </summary>
internal sealed class OpenCodeAcpAgent : IDisposable
{
    private readonly Process _process;

    private OpenCodeAcpAgent(Process process, JsonNode initializeResult)
    {
        _process = process;
        InitializeResult = initializeResult;
    }

    public int Id => _process.Id;

    public JsonNode InitializeResult { get; }

    /// <summary>The name the operating system reports — what the module matches on.</summary>
    public string ProcessName
    {
        get
        {
            using var live = Process.GetProcessById(_process.Id);
            return live.ProcessName;
        }
    }

    public static async Task<OpenCodeAcpAgent> StartAsync(
        string binary,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(binary)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.Add("acp");

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{binary} acp'.");

        try
        {
            var result = await HandshakeAsync(process, cancellationToken);

            return new OpenCodeAcpAgent(process, result);
        }
        catch
        {
            Kill(process);
            process.Dispose();
            throw;
        }
    }

    private static async Task<JsonNode> HandshakeAsync(Process agent, CancellationToken cancellationToken)
    {
        var request = new JsonObject
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
                    ["title"] = "opencode-go-waybar E2E suite",
                    ["version"] = "1.0.0",
                },
            },
        };

        await agent.StandardInput.WriteLineAsync(request.ToJsonString());
        await agent.StandardInput.FlushAsync(cancellationToken);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));

        while (await agent.StandardOutput.ReadLineAsync(timeout.Token) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('{'))
            {
                continue;
            }

            var message = JsonNode.Parse(line);

            if (message?["id"]?.GetValue<int>() == 0 && message["result"] is { } result)
            {
                return result;
            }
        }

        throw new InvalidOperationException(
            $"'{agent.StartInfo.FileName} acp' exited before answering initialize: " +
            await agent.StandardError.ReadToEndAsync(cancellationToken));
    }

    /// <summary>Every process the operating system currently reports as opencode.</summary>
    public static IReadOnlyList<int> RunningOpenCodeProcessIds()
    {
        var processes = Process.GetProcesses();

        try
        {
            return processes
                .Where(process => process.ProcessName.Equals("opencode", StringComparison.OrdinalIgnoreCase))
                .Select(process => process.Id)
                .ToArray();
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    /// <summary>
    /// Kills every opencode process still running. An agent spawned by an
    /// editor outlives the editor — Neovim's `qall!` does not take its ACP
    /// child with it — so a test that starts one through a third party has to
    /// clean up explicitly, or it poisons whatever runs next.
    /// </summary>
    public static void KillAllRunning()
    {
        var processes = Process.GetProcesses();

        try
        {
            foreach (var process in processes)
            {
                if (process.ProcessName.Equals("opencode", StringComparison.OrdinalIgnoreCase))
                {
                    Kill(process);
                }
            }
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(TimeSpan.FromSeconds(10));
            }
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
    }

    public void Dispose()
    {
        Kill(_process);
        _process.Dispose();
    }
}
