using System.Diagnostics;

namespace OpencodeGoWaybar.Brokers.Processes;

internal sealed class ProcessBroker : IProcessBroker
{
    private const string ProcFileSystem = "/proc";

    /// <summary>Hands the native process table to the broker-neighboring service.</summary>
    public ValueTask<IReadOnlyList<Process>> GetProcessesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Process> processes = Process.GetProcesses();

        return ValueTask.FromResult(processes);
    }

    /// <summary>
    /// Reads parentage out of procfs, the only place Linux publishes it. Entries
    /// vanish while the directory is being walked — a process that exits between
    /// the listing and the read — so a disappearing entry is skipped rather than
    /// failing the scan; it is a process that no longer exists to report on.
    /// </summary>
    public ValueTask<IReadOnlyDictionary<int, int>> GetParentProcessIdsAsync(CancellationToken cancellationToken)
    {
        var parentProcessIds = new Dictionary<int, int>();

        if (!Directory.Exists(ProcFileSystem))
        {
            return ValueTask.FromResult<IReadOnlyDictionary<int, int>>(parentProcessIds);
        }

        foreach (var directory in Directory.EnumerateDirectories(ProcFileSystem))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!int.TryParse(Path.GetFileName(directory), out var processId))
            {
                continue;
            }

            if (TryReadParentProcessId(directory, out var parentProcessId))
            {
                parentProcessIds[processId] = parentProcessId;
            }
        }

        return ValueTask.FromResult<IReadOnlyDictionary<int, int>>(parentProcessIds);
    }

    /// <summary>
    /// The parent identifier is the second field after the executable name in
    /// <c>/proc/[pid]/stat</c>. The name is parenthesised and may itself contain
    /// spaces or parentheses, so the fields are counted from the last closing
    /// parenthesis rather than by splitting the whole line.
    /// </summary>
    private static bool TryReadParentProcessId(string processDirectory, out int parentProcessId)
    {
        parentProcessId = 0;

        string statistics;

        try
        {
            statistics = File.ReadAllText(Path.Combine(processDirectory, "stat"));
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        var nameEnd = statistics.LastIndexOf(')');

        if (nameEnd < 0)
        {
            return false;
        }

        // What follows the name is "<state> <ppid> ...".
        var fields = statistics[(nameEnd + 1)..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return fields.Length >= 2 && int.TryParse(fields[1], out parentProcessId);
    }
}
