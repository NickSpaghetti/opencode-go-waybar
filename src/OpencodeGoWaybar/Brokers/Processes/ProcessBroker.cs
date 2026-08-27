using System.Diagnostics;

namespace OpencodeGoWaybar.Brokers.Processes;

internal sealed class ProcessBroker : IProcessBroker
{
    /// <summary>Hands the native process table to the broker-neighboring service.</summary>
    public ValueTask<IReadOnlyList<Process>> GetProcessesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Process> processes = Process.GetProcesses();

        return ValueTask.FromResult(processes);
    }
}
