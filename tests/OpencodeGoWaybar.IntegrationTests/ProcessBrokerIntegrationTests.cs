using System.Diagnostics;
using OpencodeGoWaybar.Brokers.Processes;
using Xunit;

namespace OpencodeGoWaybar.IntegrationTests;

[Trait("Tier", "Integration")]
public sealed class ProcessBrokerIntegrationTests
{
    [Fact]
    public async Task ShouldRetrieveTheRunningProcessTableAsync()
    {
        // given
        var broker = new ProcessBroker();

        // when
        var processes = await broker.GetProcessesAsync(CancellationToken.None);

        // then
        Assert.NotEmpty(processes);
    }

    [Fact]
    public async Task ShouldSurfaceTheCurrentProcessByNameAsync()
    {
        // given
        var broker = new ProcessBroker();
        using var currentProcess = Process.GetCurrentProcess();

        // when
        var processes = await broker.GetProcessesAsync(CancellationToken.None);

        // then
        var actual = Assert.Single(processes, process => process.Id == currentProcess.Id);
        Assert.Equal(currentProcess.ProcessName, actual.ProcessName);
        Assert.False(string.IsNullOrWhiteSpace(actual.ProcessName));
    }
}
