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
    public async Task ShouldRetrieveUsageAsync()
    {
        // given
        var broker = new StubUsageApiBroker((_, _) => ValueTask.FromResult(new UsageApiBrokerResponse(
            System.Net.HttpStatusCode.OK,
            """
            {"usage":{"rolling":{"status":"ok","percent":10,"resetsAt":"2026-08-15T19:29:58Z"},"weekly":{"status":"ok","percent":20,"resetsAt":"2026-08-17T00:00:00Z"},"monthly":{"status":"ok","percent":30,"resetsAt":"2026-09-15T00:00:00Z"}}}
            """)));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var secrets = new OpenCodeGoSecrets { ApiKey = "test-key" };
        var foundation = new UsageService(broker, loggingBroker, secrets);

        // when
        var actual = await foundation.RetrieveUsageAsync(CancellationToken.None);

        // when
        Assert.Equal(20, actual.Usage.Weekly.Percent);
        // when
        Assert.Equal("test-key", broker.ReceivedApiKey);
    }
}
