using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Orchestrations.UsageWindows;

internal interface IUsageWindowsOrchestrationService
{
    ValueTask<UsageWindowSnapshot> RetrieveWindowsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
