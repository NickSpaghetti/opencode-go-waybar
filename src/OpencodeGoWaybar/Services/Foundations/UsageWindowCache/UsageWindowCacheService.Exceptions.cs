using System.Text.Json;
using OpencodeGoWaybar.Models.Usages.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.UsageWindowCache;

internal sealed partial class UsageWindowCacheService
{
    private async ValueTask<T> TryCatchAsync<T>(Func<ValueTask<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw await LogAndReturnAsync(MapException(exception));
        }
    }

    private async ValueTask TryCatchAsync(Func<ValueTask> operation)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw await LogAndReturnAsync(MapException(exception));
        }
    }

    private static Exception MapException(Exception exception) => exception switch
    {
        CacheValidationException => exception,
        JsonException => new CacheResponseException(exception),
        IOException or UnauthorizedAccessException => new CacheUnavailableException(exception),
        _ => new CacheServiceException(exception),
    };

    private async ValueTask<Exception> LogAndReturnAsync(Exception exception)
    {
        await loggingBroker.LogErrorAsync(exception);

        return exception;
    }
}
