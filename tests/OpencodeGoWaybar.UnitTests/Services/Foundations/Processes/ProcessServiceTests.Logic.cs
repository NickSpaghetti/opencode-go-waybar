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
    public async Task ShouldReturnFalseWhenNoOpenCodeProcessIsRunningAsync()
    {
        // 1. Arrange
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var broker = CreateBroker(Process.GetCurrentProcess());
        var service = CreateService(broker, loggingBroker);

        // 2. Act
        // when
        var actual = await service.IsOpenCodeRunningAsync(CancellationToken.None);

        // 3. Assert
        // then
        Assert.False(actual);
        await loggingBroker.DidNotReceive().LogErrorAsync(Arg.Any<Exception>());
    }

    [Fact]
    public async Task ShouldReturnFalseWhenTheProcessTableIsEmptyAsync()
    {
        // given
        var service = CreateService(CreateBroker(), Substitute.For<ILoggingBroker>());

        // when and then
        Assert.False(await service.IsOpenCodeRunningAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ShouldHonourTheConfiguredProcessOverrideAsync()
    {
        // given
        var broker = CreateBroker();

        // when and then
        Assert.True(await CreateService(broker, Substitute.For<ILoggingBroker>(), processPresentOverride: true)
            .IsOpenCodeRunningAsync(CancellationToken.None));

        Assert.False(await CreateService(broker, Substitute.For<ILoggingBroker>(), processPresentOverride: false)
            .IsOpenCodeRunningAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ShouldNotReadTheProcessTableWhenOverriddenAsync()
    {
        // given
        var broker = CreateBroker();
        var service = CreateService(broker, Substitute.For<ILoggingBroker>(), processPresentOverride: true);

        // when
        await service.IsOpenCodeRunningAsync(CancellationToken.None);

        await broker.DidNotReceive().GetProcessesAsync(Arg.Any<CancellationToken>());
    }
}
