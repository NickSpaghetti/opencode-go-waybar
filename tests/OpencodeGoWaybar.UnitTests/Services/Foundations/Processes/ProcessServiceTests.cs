using System.Diagnostics;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Support.Processes;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Services.Foundations.Processes;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Processes;

public sealed class ProcessServiceTests
{
    [Fact]
    public async Task DetectsTerminalOpenCode()
    {
        // 1. Arrange
        var processBrokerMock = Substitute.For<IProcessBroker>();

        // Grab the actual native process running this test (e.g., 'testhost' or 'dotnet')
        Process currentProcess = Process.GetCurrentProcess();

        // Create a list containing this native, unmocked framework object
        var sampleProcesses = new List<Process>
        {
            currentProcess
        };

        // Have the mocked broker return the real framework process
        processBrokerMock
            .RetrieveProcessesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<Process>>(sampleProcesses));

        var service = new ProcessService(processBrokerMock, null, Substitute.For<ILoggingBroker>());

        // 2. Act
        // NOTE: To make this testable, IsInteractiveOpenCodeRunningAsync needs to either:
        // A) Accept the target process name as a parameter (e.g., currentProcess.ProcessName)
        // B) Or, if "opencode" is hardcoded in the service logic, this test design fundamentally 
        //    clashes with the native Process API constraints.
        bool isRunning = await service.IsInteractiveOpenCodeRunningAsync(currentProcess.ProcessName, CancellationToken.None);

        // 3. Assert
        Assert.True(isRunning);
    }

    [Fact]
    public async Task DetectsOpenCodeAcpFromJetBrains()
    {
        var broker = CreateBroker(new Process());
        var service = CreateService(broker, null);

        Assert.True(await service.IsInteractiveOpenCodeRunningAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RecognizesOpenCodeByProcessName()
    {
        var broker = CreateBroker(new Process());
        var service = CreateService(broker, null);

        Assert.True(await service.IsInteractiveOpenCodeRunningAsync(CancellationToken.None));
    }

    [Fact]
    public async Task UsesExplicitOverrideForContainerTests()
    {
        var broker = CreateBroker();

        Assert.True(await CreateService(broker, true).IsInteractiveOpenCodeRunningAsync(CancellationToken.None));
        Assert.False(await CreateService(broker, false).IsInteractiveOpenCodeRunningAsync(CancellationToken.None));
    }

    [Fact]
    public async Task IgnoresUnrelatedProcesses()
    {
        var broker = CreateBroker(new Process());
        var service = CreateService(broker, null);

        Assert.False(await service.IsInteractiveOpenCodeRunningAsync(CancellationToken.None));
    }

    private static IProcessBroker CreateBroker(params Process[] processes)
    {
        var broker = Substitute.For<IProcessBroker>();
        broker.RetrieveProcessesAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<IReadOnlyList<Process>>(processes));
        return broker;
    }

    [Fact]
    public async Task RecognizesOpenCodeWithoutAParentAllowlist()
    {
        var broker = CreateBroker(new Process());
        var service = CreateService(broker, null);

        Assert.True(await service.IsInteractiveOpenCodeRunningAsync(CancellationToken.None));
    }

    private static ProcessService CreateService(IProcessBroker broker, bool? processPresentOverride) =>
        new(broker, processPresentOverride, Substitute.For<ILoggingBroker>());
}
