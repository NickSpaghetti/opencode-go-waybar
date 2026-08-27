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
    public async Task ShouldThrowProcessResponseExceptionOnIsOpenCodeRunningIfBrokerReturnsNothingAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var broker = Substitute.For<IProcessBroker>();
        broker.GetProcessesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<Process>>(null!));
        var service = CreateService(broker, loggingBroker);

        // when and then
        await Assert.ThrowsAsync<ProcessResponseException>(() =>
            service.IsOpenCodeRunningAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<ProcessResponseException>());
    }
}
