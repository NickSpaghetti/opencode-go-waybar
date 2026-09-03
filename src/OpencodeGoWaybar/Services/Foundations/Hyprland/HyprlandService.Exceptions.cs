using System.Net.Sockets;
using System.Text.Json;
using OpencodeGoWaybar.Models.Hyprland.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Hyprland;

internal sealed partial class HyprlandService
{
    /// <summary>
    /// One mapping for both reads. They differ only in their return type, and
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
        catch (SocketException exception)
        {
            throw await LogAndReturnAsync(new HyprlandUnavailableException(exception));
        }
        catch (IOException exception)
        {
            throw await LogAndReturnAsync(new HyprlandUnavailableException(exception));
        }
        catch (JsonException exception)
        {
            throw await LogAndReturnAsync(new HyprlandResponseException(exception));
        }
        catch (HyprlandResponseException exception)
        {
            throw await LogAndReturnAsync(exception);
        }
        catch (Exception exception)
        {
            throw await LogAndReturnAsync(new HyprlandServiceException(exception));
        }
    }

    private async ValueTask<Exception> LogAndReturnAsync(Exception exception)
    {
        await _loggingBroker.LogErrorAsync(exception);

        return exception;
    }
}
