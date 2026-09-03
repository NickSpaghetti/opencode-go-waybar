using System.ComponentModel;
using OpencodeGoWaybar.Models.Processes.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal sealed partial class ProcessService
{
    /// <summary>
    /// One mapping for every read. They differ only in their return type, and
    /// duplicating the ladder per method would mean two places to keep in step.
    /// </summary>
    private async ValueTask<T> TryCatchAsync<T>(
        Func<CancellationToken, ValueTask<T>> returningFunction,
        CancellationToken cancellationToken)
    {
        try
        {
            return await returningFunction(cancellationToken);
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
