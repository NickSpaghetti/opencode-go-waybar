using System.ComponentModel;
using OpencodeGoWaybar.Models.Processes.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal sealed partial class ProcessService
{
    private async ValueTask<bool> TryCatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await RetrieveIsOpenCodeRunningAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException exception)
        {
            throw await LogAndReturnAsync(new ProcessTableUnavailableException(exception));
        }
        catch (Win32Exception exception)
        {
            throw await LogAndReturnAsync(new ProcessTableUnavailableException(exception));
        }
        catch (ProcessResponseException exception)
        {
            throw await LogAndReturnAsync(exception);
        }
        catch (Exception exception)
        {
            throw await LogAndReturnAsync(new ProcessServiceException(exception));
        }
    }

    private async ValueTask<Exception> LogAndReturnAsync(Exception exception)
    {
        await _loggingBroker.LogErrorAsync(exception);
        return exception;
    }
}
