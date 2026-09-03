using System.Diagnostics;

namespace OpencodeGoWaybar.Brokers.Processes;

/// <summary>Provides read access to the operating system process table.</summary>
internal interface IProcessBroker
{
    /// <summary>Retrieves every process currently known to the operating system.</summary>
    ValueTask<IReadOnlyList<Process>> GetProcessesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves each process identifier mapped to its parent's. Reported
    /// separately from the process table because <see cref="Process"/> carries no
    /// parentage, and the mapping is what turns a background process into the
    /// window that owns it.
    /// </summary>
    ValueTask<IReadOnlyDictionary<int, int>> GetParentProcessIdsAsync(CancellationToken cancellationToken);
}
