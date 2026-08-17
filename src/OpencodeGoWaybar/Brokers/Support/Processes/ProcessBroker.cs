using System.Diagnostics;

namespace OpencodeGoWaybar.Brokers.Support.Processes;

internal sealed class ProcessBroker : IProcessBroker
{
    public ValueTask<IReadOnlyList<Process>> RetrieveProcessesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Process> processes = Process.GetProcesses();

        return ValueTask.FromResult(processes);
    }
}
