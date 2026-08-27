using System.Diagnostics;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Processes;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

/// <summary>
/// Abstracts the native <see cref="Process"/> table handed over by the process
/// broker into the single local answer the rest of the system needs: whether
/// OpenCode is running.
/// </summary>
internal sealed partial class ProcessService : IProcessService
{
    private const string OpenCodeProcessName = "opencode";

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
        TryCatchAsync(cancellationToken);

    private async ValueTask<bool> RetrieveIsOpenCodeRunningAsync(CancellationToken cancellationToken)
    {
        if (_options.ProcessPresentOverride is bool processIsPresent)
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
