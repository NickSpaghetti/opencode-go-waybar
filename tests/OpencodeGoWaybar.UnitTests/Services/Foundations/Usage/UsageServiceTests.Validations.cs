using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Usages;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Foundations.Usage;
using OpencodeGoWaybar.Models.Usages.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Usage;

public sealed partial class UsageServiceTests
{
    [Fact]
    public async Task ShouldThrowUsageCredentialsMissingExceptionOnRetrieveUsageIfKeyIsAbsentAndLogItAsync()
    {
        // given
        var broker = new StubUsageApiBroker((_, _) => throw new InvalidOperationException());
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new UsageService(broker, loggingBroker, new OpenCodeGoSecrets());

        // when and then
        await Assert.ThrowsAsync<UsageCredentialsMissingException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());
        // when
        Assert.Null(broker.ReceivedApiKey);
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<Exception>());
    }

    [Fact]
    public async Task ShouldThrowUsageApiResponseExceptionOnRetrieveUsageIfResponseIsMalformedAndLogItAsync()
    {
        // given
        var broker = new StubUsageApiBroker((_, _) => ValueTask.FromResult(new UsageApiBrokerResponse(
            System.Net.HttpStatusCode.OK,
            """
            {"usage":{"rolling":{"status":"ok","percent":null,"resetsAt":null},"weekly":{"status":"ok","percent":20,"resetsAt":"2026-08-17T00:00:00Z"},"monthly":{"status":"ok","percent":30,"resetsAt":"2026-09-15T00:00:00Z"}}}
            """)));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new UsageService(broker, loggingBroker, new OpenCodeGoSecrets { ApiKey = "test-key" });

        // when and then
        await Assert.ThrowsAsync<UsageApiResponseException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<UsageApiResponseException>());
    }
}
