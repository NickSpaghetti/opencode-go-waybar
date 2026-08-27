using System.Diagnostics;

namespace OpencodeGoWaybar.Brokers.Processes;

/// <summary>Provides read access to the operating system process table.</summary>
internal interface IProcessBroker
{
    /// <summary>Retrieves every process currently known to the operating system.</summary>
    ValueTask<IReadOnlyList<Process>> GetProcessesAsync(CancellationToken cancellationToken);
}
