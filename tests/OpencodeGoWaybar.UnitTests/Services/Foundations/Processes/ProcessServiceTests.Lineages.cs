using System.Diagnostics;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Processes;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Processes;

/// <summary>
/// The lineage read, as far as a unit test can reach it. A native
/// <see cref="Process"/> cannot be given a chosen name, so the branch that finds
/// an OpenCode process and climbs from it is proved against a real agent in
/// ProcessLineageIntegrationTests; what belongs here is the empty case and the
/// work it must not do.
/// </summary>
public sealed partial class ProcessServiceTests
{
    [Fact]
    public async Task ShouldReturnNoLineagesWhenNoOpenCodeProcessIsRunningAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var broker = CreateBroker(Process.GetCurrentProcess());
        var service = CreateService(broker, loggingBroker);

        // when
        var lineages = await service.RetrieveOpenCodeLineagesAsync(CancellationToken.None);

        // then
        Assert.Empty(lineages);
        await loggingBroker.DidNotReceive().LogErrorAsync(Arg.Any<Exception>());
    }

    /// <summary>
    /// Reading parentage means walking every entry in procfs. With no session to
    /// place, that walk buys nothing — and this runs on every Waybar poll.
    /// </summary>
    [Fact]
    public async Task ShouldNotReadProcessParentageWhenThereIsNoSessionToPlaceAsync()
    {
        // given
        var broker = CreateBroker(Process.GetCurrentProcess());
        var service = CreateService(broker, Substitute.For<ILoggingBroker>());

        // when
        await service.RetrieveOpenCodeLineagesAsync(CancellationToken.None);

        // then
        await broker.DidNotReceive().GetParentProcessIdsAsync(Arg.Any<CancellationToken>());
    }
}
