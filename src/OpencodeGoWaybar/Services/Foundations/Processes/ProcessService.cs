using System.Diagnostics;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Processes;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Processes;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

/// <summary>
/// Abstracts the native <see cref="Process"/> table handed over by the process
/// broker into the local answers the rest of the system needs: whether OpenCode
/// is running, and what spawned each session that is.
/// </summary>
internal sealed partial class ProcessService : IProcessService
{
    private const string OpenCodeProcessName = "opencode";

    /// <summary>
    /// Stops a lineage walk that a malformed parent map could otherwise send
    /// round forever. Real chains are a handful of processes deep; nothing
    /// legitimate approaches this.
    /// </summary>
    private const int MaxLineageDepth = 64;

    private readonly IProcessBroker _processBroker;
    private readonly ILoggingBroker _loggingBroker;
    private readonly OpenCodeGoOptions _options;

    public ProcessService(
        IProcessBroker processBroker,
        ILoggingBroker loggingBroker,
        OpenCodeGoOptions options)
    {
        _processBroker = processBroker;
        _loggingBroker = loggingBroker;
        _options = options;
    }

    public ValueTask<bool> IsOpenCodeRunningAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(RetrieveIsOpenCodeRunningAsync, cancellationToken);

    public ValueTask<IReadOnlyList<OpenCodeProcessLineage>> RetrieveOpenCodeLineagesAsync(
        CancellationToken cancellationToken) =>
        TryCatchAsync(RetrieveOpenCodeLineagesCoreAsync, cancellationToken);

    private async ValueTask<bool> RetrieveIsOpenCodeRunningAsync(CancellationToken cancellationToken)
    {
        if (_options.ProcessPresentOverride is { } processIsPresent)
        {
            return processIsPresent;
        }

        var processes = await _processBroker.GetProcessesAsync(cancellationToken);
        ValidateProcesses(processes);

        try
        {
            return processes.Any(IsOpenCodeProcess);
        }
        finally
        {
            DisposeProcesses(processes);
        }
    }

    private async ValueTask<IReadOnlyList<OpenCodeProcessLineage>> RetrieveOpenCodeLineagesCoreAsync(
        CancellationToken cancellationToken)
    {
        var processes = await _processBroker.GetProcessesAsync(cancellationToken);
        ValidateProcesses(processes);

        int[] openCodeProcessIds;

        try
        {
            openCodeProcessIds = processes.Where(IsOpenCodeProcess).Select(process => process.Id).ToArray();
        }
        finally
        {
            DisposeProcesses(processes);
        }

        if (openCodeProcessIds.Length == 0)
        {
            return [];
        }

        var parentProcessIds = await _processBroker.GetParentProcessIdsAsync(cancellationToken);
        ValidateParentProcessIds(parentProcessIds);

        return Array.ConvertAll(
            openCodeProcessIds,
            processId => new OpenCodeProcessLineage(
                processId,
                WalkLineage(processId, parentProcessIds)));
    }

    /// <summary>
    /// Climbs from a process to init, nearest ancestor first. The walk stops at
    /// pid 1 — nothing above it can own a window — and refuses to revisit a
    /// process, so a cycle ends the chain instead of spinning.
    /// </summary>
    private static IReadOnlyList<int> WalkLineage(
        int processId,
        IReadOnlyDictionary<int, int> parentProcessIds)
    {
        var lineage = new List<int> { processId };
        var visited = new HashSet<int> { processId };
        var current = processId;

        while (lineage.Count < MaxLineageDepth
            && parentProcessIds.TryGetValue(current, out var parentProcessId)
            && parentProcessId > 1
            && visited.Add(parentProcessId))
        {
            lineage.Add(parentProcessId);
            current = parentProcessId;
        }

        return lineage;
    }

    private static bool IsOpenCodeProcess(Process process) =>
        process.ProcessName.Equals(OpenCodeProcessName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Releases the native process objects the broker leaked into this service.
    /// The broker cannot do this itself without taking on flow control.
    /// </summary>
    private static void DisposeProcesses(IReadOnlyList<Process> processes)
    {
        foreach (var process in processes)
        {
            process.Dispose();
        }
    }
}
