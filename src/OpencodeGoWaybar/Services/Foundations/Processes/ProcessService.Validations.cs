using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using OpencodeGoWaybar.Models.Processes.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal sealed partial class ProcessService
{
    private static void ValidateProcesses([NotNull] IReadOnlyList<Process>? processes)
    {
        if (processes is null)
        {
            throw new ProcessResponseException(
                new InvalidDataException("The process broker returned no process table."));
        }
    }

    private static void ValidateParentProcessIds([NotNull] IReadOnlyDictionary<int, int>? parentProcessIds)
    {
        if (parentProcessIds is null)
        {
            throw new ProcessResponseException(
                new InvalidDataException("The process broker returned no process parentage."));
        }
    }
}
