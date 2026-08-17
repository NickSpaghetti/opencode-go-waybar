using System.Net;
using System.Text.Json;
using OpencodeGoWaybar.Brokers.Apis.Usage;
using OpencodeGoWaybar.Services.Foundations.Usage.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Usage;

internal sealed partial class UsageService
{
    private async ValueTask<UsageResponse> TryCatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            ValidateApiKey();
            var response = await _usageBroker.GetUsageAsync(_secrets.Value.ApiKey!, cancellationToken);
            ThrowForFailureStatus(response.StatusCode);
            var usage = JsonSerializer.Deserialize(response.Body, UsageJsonContext.Default.UsageResponse)
                ?? throw new InvalidOperationException("The usage API returned an empty response.");
            ValidateResponse(usage);
            return usage;
        }
        catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw await LogAndReturnAsync(new UsageAuthenticationException(exception));
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw await LogAndReturnAsync(new UsageRateLimitedException(exception));
        }
        catch (HttpRequestException exception)
        {
            throw await LogAndReturnAsync(new UsageApiUnavailableException(exception));
        }
        catch (JsonException exception)
        {
            throw await LogAndReturnAsync(new UsageApiResponseException(exception));
        }
        catch (InvalidOperationException exception)
        {
            throw await LogAndReturnAsync(new UsageApiResponseException(exception));
        }
        catch (UsageCredentialsMissingException exception)
        {
            throw await LogAndReturnAsync(exception);
        }
        catch (UsageApiResponseException exception)
        {
            throw await LogAndReturnAsync(exception);
        }
        catch (Exception exception)
        {
            throw await LogAndReturnAsync(new UsageServiceException(exception));
        }
    }

    private async ValueTask<Exception> LogAndReturnAsync(Exception exception)
    {
        await _loggingBroker.LogErrorAsync(exception);
        return exception;
    }

    private static void ThrowForFailureStatus(HttpStatusCode statusCode)
    {
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new HttpRequestException("The usage API rejected the request.", null, statusCode);
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException("The usage API rate-limited the request.", null, statusCode);
        }

        if ((int)statusCode >= 400)
        {
            throw new HttpRequestException("The usage API returned an error.", null, statusCode);
        }
    }
}
