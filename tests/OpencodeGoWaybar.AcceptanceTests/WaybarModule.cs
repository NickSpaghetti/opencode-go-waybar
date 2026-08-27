using System.Diagnostics;
using System.Text.Json.Nodes;

using OpencodeGoWaybar.TestSupport;

namespace OpencodeGoWaybar.AcceptanceTests;

/// <summary>Runs the Waybar module the way Waybar does and parses its JSON line.</summary>
internal static class WaybarModule
{
    public static async Task<WaybarPayload> RunAsync(CancellationToken cancellationToken = default) =>
        await RunAsync(environment: null, cancellationToken);

    /// <param name="environment">
    /// Variables to set for this run; an empty value unsets the variable, which
    /// is how a test forces the module down a particular path.
    /// </param>
    public static async Task<WaybarPayload> RunAsync(
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken = default)
    {
        // The shipped NativeAOT binary, launched exactly as Waybar launches it.
        var startInfo = new ProcessStartInfo(E2eEnvironment.ModuleBinary)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // The override would defeat the point of an end-to-end run: these tests
        // exist to prove the real process table drives the output.
        startInfo.Environment.Remove("OPENCODE_GO_PROCESS_PRESENT");

        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrEmpty(value))
            {
                startInfo.Environment.Remove(name);
            }
            else
            {
                startInfo.Environment[name] = value;
            }
        }

        using var module = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{E2eEnvironment.ModuleBinary}'.");

        var standardOutput = module.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = module.StandardError.ReadToEndAsync(cancellationToken);

        await module.WaitForExitAsync(cancellationToken);

        var output = await standardOutput;
        var error = await standardError;

        var jsonLine = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault(line => line.StartsWith('{'));

        if (jsonLine is null)
        {
            throw new InvalidOperationException(
                $"The module emitted no JSON payload.{Environment.NewLine}" +
                $"exit code: {module.ExitCode}{Environment.NewLine}" +
                $"stdout: {output}{Environment.NewLine}stderr: {error}");
        }

        return new WaybarPayload(module.ExitCode, jsonLine, JsonNode.Parse(jsonLine)!, output, error);
    }
}

internal sealed record WaybarPayload(
    int ExitCode,
    string Json,
    JsonNode Node,
    string StandardOutput,
    string StandardError)
{
    public bool Visible => Node["visible"]!.GetValue<bool>();

    public string Class => Node["class"]!.GetValue<string>();

    public string Text => Node["text"]!.GetValue<string>();

    public string Tooltip => Node["tooltip"]!.GetValue<string>();
}
