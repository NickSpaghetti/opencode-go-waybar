using System.Diagnostics;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Brokers.Support.Processes;
using OpencodeGoWaybar.Services.Foundations.Processes.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal sealed partial class ProcessService
{
    private async ValueTask<bool> TryCatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (processPresentOverride.HasValue)
            {
                return processPresentOverride.Value;
            }

            var processes = await processBroker.RetrieveProcessesAsync(cancellationToken);
            
            return processes.Any(p => IsOpenCodeProcess(new ProcessInfo(p.Id, p.ProcessName)));
        }
        catch (ProcessServiceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var serviceException = new ProcessServiceException(exception);
            await loggingBroker.LogErrorAsync(serviceException);
            throw serviceException;
        }
    }
}
