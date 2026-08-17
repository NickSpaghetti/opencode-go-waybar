using System.Diagnostics;

namespace OpencodeGoWaybar.Brokers.Support.Processes;

internal interface IProcessBroker
{
    ValueTask<IReadOnlyList<Process>> RetrieveProcessesAsync(CancellationToken cancellationToken);
}
