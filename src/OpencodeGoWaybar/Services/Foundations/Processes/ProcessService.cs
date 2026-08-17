using OpencodeGoWaybar.Brokers.Support.Processes;
using OpencodeGoWaybar.Brokers.Support.Logging;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal sealed partial class ProcessService(
    IProcessBroker processBroker,
    bool? processPresentOverride,
    ILoggingBroker loggingBroker) : IProcessService
{
    public ValueTask<bool> IsInteractiveOpenCodeRunningAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(cancellationToken);

    private static bool IsOpenCodeProcess(ProcessInfo process) =>
        process.Name.Equals("opencode", StringComparison.OrdinalIgnoreCase);
}
