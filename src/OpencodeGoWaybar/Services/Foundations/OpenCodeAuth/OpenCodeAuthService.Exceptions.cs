using System.Text.Json;
using OpencodeGoWaybar.Models.OpenCodeAuths.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeAuth;

internal sealed partial class OpenCodeAuthService
{
    private string? TryCatch(Func<string?> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // opencode writes the store only once a provider is connected.
            return null;
        }
        catch (Exception exception) when (exception is OpenCodeAuthUnavailableException
            or OpenCodeAuthResponseException)
        {
            // Already local — raised by validation or an earlier mapping.
            throw LogAndReturn(exception);
        }
        catch (JsonException exception)
        {
            throw LogAndReturn(new OpenCodeAuthResponseException(exception));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw LogAndReturn(new OpenCodeAuthUnavailableException(exception));
        }
        catch (Exception exception)
        {
            throw LogAndReturn(new OpenCodeAuthServiceException(exception));
        }
    }

    private Exception LogAndReturn(Exception exception)
    {
        loggingBroker.LogError(exception);

        return exception;
    }
}
