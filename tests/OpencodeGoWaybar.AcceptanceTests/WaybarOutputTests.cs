using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.AcceptanceTests;

/// <summary>
/// The module's observable contract: the shipped NativeAOT binary, launched the
/// way Waybar launches it, against the real process table. No doubles, no
/// overrides — if these pass, the thing that ships behaves.
/// </summary>
[Trait("Tier", "Acceptance")]
[Trait("Layer", "1")]
public sealed class WaybarOutputTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShouldReportHiddenWhenOpenCodeIsNotRunningAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();

        // then
        Assert.Empty(OpenCodeAcpAgent.RunningOpenCodeProcessIds());

        var payload = await WaybarModule.RunAsync(timeout.Token);
        output.WriteLine(payload.Json);

        Assert.False(payload.Visible);
        Assert.Equal("hidden", payload.Class);
        Assert.Equal(string.Empty, payload.Text);
    }

    [Theory]
    [InlineData("install-script")]
    [InlineData("npm")]
    public async Task ShouldBecomeVisibleWhileAnAcpAgentIsRunningAsync(string installMethod)
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;
        var binary = BinaryFor(installMethod);

        // when
        var before = await WaybarModule.RunAsync(cancellationToken);
        // then
        Assert.False(before.Visible);

        using (var agent = await OpenCodeAcpAgent.StartAsync(binary, cancellationToken))
        {
            output.WriteLine($"{installMethod}: pid={agent.Id} name={agent.ProcessName}");

            var during = await WaybarModule.RunAsync(cancellationToken);
            output.WriteLine(during.Json);

            Assert.True(
                during.Visible,
                $"The module stayed hidden while '{binary} acp' (pid {agent.Id}, " +
                $"process name '{agent.ProcessName}') was running. Payload: {during.Json}");
        }

        var after = await WaybarModule.RunAsync(cancellationToken);
        Assert.False(after.Visible);
    }

    private static string BinaryFor(string installMethod) => installMethod switch
    {
        "install-script" => E2eEnvironment.ScriptInstalledOpenCode,
        "npm" => E2eEnvironment.NpmInstalledOpenCode,
        _ => throw new ArgumentOutOfRangeException(nameof(installMethod), installMethod, null),
    };
}
