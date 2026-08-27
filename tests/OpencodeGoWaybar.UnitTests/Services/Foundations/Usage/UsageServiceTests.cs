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
    private static UsageService CreateService(
        IUsageBroker usageBroker,
        ILoggingBroker loggingBroker,
        string? apiKey = "test-key") =>
        new(usageBroker, loggingBroker, new OpenCodeGoSecrets { ApiKey = apiKey });

    private sealed class StubUsageApiBroker(
        Func<string, CancellationToken, ValueTask<UsageApiBrokerResponse>> call) : IUsageBroker
    {
        public string? ReceivedApiKey { get; private set; }

        public ValueTask<UsageApiBrokerResponse> GetUsageAsync(string apiKey, CancellationToken cancellationToken)
        {
            ReceivedApiKey = apiKey;

            return call(apiKey, cancellationToken);
        }
    }
}
