using OpencodeGoWaybar.Brokers.Usages;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.Usage;

internal sealed partial class UsageService : IUsageService
{
    private readonly IUsageBroker _usageBroker;
    private readonly ILoggingBroker _loggingBroker;
    private readonly OpenCodeGoSecrets _secrets;

    public UsageService(
        IUsageBroker usageBroker,
        ILoggingBroker loggingBroker,
        OpenCodeGoSecrets secrets)
    {
        _usageBroker = usageBroker;
        _loggingBroker = loggingBroker;
        _secrets = secrets;
    }

    public ValueTask<UsageResponse> RetrieveUsageAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(cancellationToken);
}
