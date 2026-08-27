using System.Diagnostics;
using OpencodeGoWaybar.Models.Processes.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal sealed partial class ProcessService
{
    private static void ValidateProcesses(IReadOnlyList<Process>? processes)
    {
        if (processes is null)
        {
            throw new ProcessResponseException(
                new InvalidDataException("The process broker returned no process table."));
        }
    }
}
