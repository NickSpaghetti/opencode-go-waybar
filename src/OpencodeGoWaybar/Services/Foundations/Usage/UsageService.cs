using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Brokers.Apis.Usage;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Usage;

internal sealed partial class UsageService : IUsageService
{
    private readonly IUsageBroker _usageBroker;
    private readonly ILoggingBroker _loggingBroker;
    private readonly IOptions<OpenCodeGoSecrets> _secrets;

    public UsageService(
        IUsageBroker usageBroker,
        ILoggingBroker loggingBroker,
        IOptions<OpenCodeGoSecrets> secrets)
    {
        _usageBroker = usageBroker;
        _loggingBroker = loggingBroker;
        _secrets = secrets;
    }

    public ValueTask<UsageResponse> RetrieveUsageAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(cancellationToken);
}
