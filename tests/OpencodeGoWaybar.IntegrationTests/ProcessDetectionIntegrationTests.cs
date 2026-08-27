using Microsoft.Extensions.Logging.Abstractions;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Processes;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace OpencodeGoWaybar.IntegrationTests;

/// <summary>
/// The real <see cref="ProcessBroker"/> wired to the real <see cref="ProcessService"/>,
/// in-process, against the real operating system process table.
///
/// This is where the positive detection path belongs. A unit test cannot reach
/// it — a native <see cref="System.Diagnostics.Process"/> cannot be fabricated
/// with a chosen name — and driving it through the shipped binary is far more
/// machinery than the question needs.
/// </summary>
[Trait("Tier", "Integration")]
public sealed class ProcessDetectionIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ShouldReportNotRunningWhenNoAgentIsPresentAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();

        // then
        Assert.Empty(OpenCodeAcpAgent.RunningOpenCodeProcessIds());

        Assert.False(await CreateService().IsOpenCodeRunningAsync(timeout.Token));
    }

    [Theory]
    [InlineData("install-script")]
    [InlineData("npm")]
    public async Task ShouldDetectARunningAcpAgentAsync(string installMethod)
    {
        // given
        using var timeout = E2eTimeout.Create();
        var cancellationToken = timeout.Token;
        var service = CreateService();

        // when and then
        Assert.False(await service.IsOpenCodeRunningAsync(cancellationToken));

        using (var agent = await OpenCodeAcpAgent.StartAsync(BinaryFor(installMethod), cancellationToken))
        {
            output.WriteLine($"{installMethod}: pid={agent.Id} name={agent.ProcessName}");

            Assert.True(
                await service.IsOpenCodeRunningAsync(cancellationToken),
                $"The service missed '{agent.ProcessName}' (pid {agent.Id}).");
        }

        Assert.False(await service.IsOpenCodeRunningAsync(cancellationToken));
    }

    [Fact]
    public async Task ShouldHonourTheOverrideOverTheRealProcessTableAsync()
    {
        // given
        using var timeout = E2eTimeout.Create();

        // when and then
        Assert.True(await CreateService(processPresentOverride: true)
            .IsOpenCodeRunningAsync(timeout.Token));
    }

    private static ProcessService CreateService(bool? processPresentOverride = null) =>
        new(
            new ProcessBroker(),
            new LoggingBroker(NullLogger<LoggingBroker>.Instance),
            new OpenCodeGoOptions { ProcessPresentOverride = processPresentOverride });

    private static string BinaryFor(string installMethod) => installMethod switch
    {
        "install-script" => E2eEnvironment.ScriptInstalledOpenCode,
        "npm" => E2eEnvironment.NpmInstalledOpenCode,
        _ => throw new ArgumentOutOfRangeException(nameof(installMethod), installMethod, null),
    };
}
