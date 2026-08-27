using System.ComponentModel;
using System.Diagnostics;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Processes;
using OpencodeGoWaybar.Models.Processes.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Processes;

public sealed partial class ProcessServiceTests
{
    [Fact]
    public async Task ShouldThrowProcessTableUnavailableExceptionOnIsOpenCodeRunningIfTableFailsAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var broker = CreateFailingBroker(new InvalidOperationException("process has exited"));
        var service = CreateService(broker, loggingBroker);

        // when and then
        await Assert.ThrowsAsync<ProcessTableUnavailableException>(() =>
            service.IsOpenCodeRunningAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<ProcessTableUnavailableException>());
    }

    [Fact]
    public async Task ShouldThrowProcessTableUnavailableExceptionOnIsOpenCodeRunningIfAccessIsDeniedAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var broker = CreateFailingBroker(new Win32Exception("access denied"));
        var service = CreateService(broker, loggingBroker);

        // when and then
        await Assert.ThrowsAsync<ProcessTableUnavailableException>(() =>
            service.IsOpenCodeRunningAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<ProcessTableUnavailableException>());
    }

    [Fact]
    public async Task ShouldThrowProcessServiceExceptionOnIsOpenCodeRunningIfServiceErrorOccursAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var broker = CreateFailingBroker(new FormatException("unexpected"));
        var service = CreateService(broker, loggingBroker);

        // when and then
        await Assert.ThrowsAsync<ProcessServiceException>(() =>
            service.IsOpenCodeRunningAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<ProcessServiceException>());
    }

    [Fact]
    public async Task ShouldLetCancellationPropagateUnwrappedAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var broker = CreateFailingBroker(new OperationCanceledException());
        var service = CreateService(broker, loggingBroker);

        // when and then
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.IsOpenCodeRunningAsync(CancellationToken.None).AsTask());
        await loggingBroker.DidNotReceive().LogErrorAsync(Arg.Any<Exception>());
    }

    private static IProcessBroker CreateBroker(params Process[] processes)
    {
        var broker = Substitute.For<IProcessBroker>();

        broker.GetProcessesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<Process>>(processes));

        return broker;
    }

    private static IProcessBroker CreateFailingBroker(Exception exception)
    {
        var broker = Substitute.For<IProcessBroker>();

        broker.GetProcessesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<IReadOnlyList<Process>>(exception));

        return broker;
    }
}
