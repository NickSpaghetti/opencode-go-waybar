using System.Diagnostics;
using System.Text;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.AcceptanceTests;

/// <summary>
/// Layer 3 — VS Code as a process-tree probe rather than a driver. VS Code has
/// no first-party ACP support (microsoft/vscode#265496 is open) and the
/// third-party extensions spawn the agent from a webview, which cannot be
/// driven headlessly; the standalone CLI also refuses `code ext install`
/// without a desktop installation. What it can still prove is the inverse, and
/// that matters here: an editor merely being open must not make the module
/// appear.
/// </summary>
[Trait("Tier", "Acceptance")]
[Trait("Layer", "3")]
public sealed class VsCodeLayer3Tests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShouldStayHiddenWhenAnEditorIsOpenWithoutAnAgentAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;

        // when
        await AssertVsCodeCliIsUsableAsync(cancellationToken);

        // then
        Assert.Empty(OpenCodeAcpAgent.RunningOpenCodeProcessIds());

        // `code serve-web` is the closest a headless container gets to an open
        // editor: it downloads and runs the real VS Code server processes.
        using var editor = StartVsCodeServer();

        try
        {
            var banner = await WaitForServerAsync(editor, cancellationToken);
            output.WriteLine($"--- code serve-web ---{Environment.NewLine}{banner}");

            Assert.Contains("Web UI available", banner, StringComparison.Ordinal);

            var processes = await RunAsync("ps", ["-eo", "pid,comm,args"], cancellationToken);
            output.WriteLine($"--- process table with VS Code running ---{Environment.NewLine}{processes.StandardOutput}");

            var payload = await WaybarModule.RunAsync(cancellationToken);
            output.WriteLine(payload.Json);

            Assert.False(
                payload.Visible,
                $"An open editor with no opencode agent made the module visible. Payload: {payload.Json}");
        }
        finally
        {
            Kill(editor);
        }
    }

    /// <summary>A precondition on the image, not a test of the module.</summary>
    private async Task AssertVsCodeCliIsUsableAsync(CancellationToken cancellationToken)
    {
        var (exitCode, stdout, stderr) = await RunAsync("code", ["--version"], cancellationToken);

        output.WriteLine($"code --version → {stdout}{stderr}");

        Assert.True(exitCode == 0, $"The VS Code CLI is not usable in this image: {stderr}");
        Assert.False(string.IsNullOrWhiteSpace(stdout));
    }

    /// <summary>
    /// Reads the server's output until it reports itself ready. The first run
    /// downloads the VS Code server, so this waits on the banner rather than a
    /// fixed delay; the test's own deadline bounds it.
    /// </summary>
    private static async Task<string> WaitForServerAsync(Process editor, CancellationToken cancellationToken)
    {
        var banner = new StringBuilder();

        while (await editor.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            banner.AppendLine(line);

            if (line.Contains("Web UI available", StringComparison.Ordinal))
            {
                break;
            }
        }

        return banner.ToString();
    }

    private static Process StartVsCodeServer()
    {
        var startInfo = new ProcessStartInfo("code")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = E2eEnvironment.Workspace,
        };

        startInfo.ArgumentList.Add("serve-web");
        startInfo.ArgumentList.Add("--accept-server-license-terms");
        startInfo.ArgumentList.Add("--without-connection-token");
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add("127.0.0.1");
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add("9888");

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the VS Code server.");
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
    }

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunAsync(
        string fileName,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, await standardOutput, await standardError);
    }
}
