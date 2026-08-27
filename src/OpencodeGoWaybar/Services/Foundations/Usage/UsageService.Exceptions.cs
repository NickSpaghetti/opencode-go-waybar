using System.Net;
using System.Text.Json;
using OpencodeGoWaybar.Brokers.Usages;
using System.Text.Json.Nodes;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Usage;

internal sealed partial class UsageService
{
    private async ValueTask<UsageResponse> TryCatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            ValidateApiKey();
            var response = await _usageBroker.GetUsageAsync(_secrets.ApiKey!, cancellationToken);
            ThrowForFailureStatus(response);
            var usage = JsonSerializer.Deserialize(response.Body, UsageJsonContext.Default.UsageResponse)
                ?? throw new InvalidOperationException("The usage API returned an empty response.");
            ValidateResponse(usage);
            return usage;
        }
        catch (Exception exception) when (exception is IUsageApiFailure)
        {
            throw await LogAndReturnAsync(exception);
        }
        catch (HttpRequestException exception)
        {
            // No status: the request never reached the API.
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

    /// <summary>
    /// Turns a failed response into a local exception that carries whatever the
    /// API said about it, so the reason can reach the bar instead of being
    /// flattened into "unavailable".
    /// </summary>
    private static void ThrowForFailureStatus(UsageApiBrokerResponse response)
    {
        if ((int)response.StatusCode < 400)
        {
            return;
        }

        var apiError = ParseApiError(response.Body);

        var inner = new HttpRequestException(
            $"The usage API returned {(int)response.StatusCode}.",
            inner: null,
            response.StatusCode);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new UsageAuthenticationException(inner, apiError),
            HttpStatusCode.TooManyRequests =>
                new UsageRateLimitedException(inner, apiError),
            _ => (Exception)new UsageApiUnavailableException(inner, apiError),
        };
    }

    /// <summary>
    /// Reads the error body defensively. The published contract types it as a
    /// free-form object, and the two shapes seen in practice disagree: the live
    /// API nests {"error":{"type","message"}} while the recorded rate-limit
    /// fixture uses a flat {"error":"...","message":"..."}.
    /// </summary>
    private static UsageApiError? ParseApiError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        JsonNode? document;

        try
        {
            document = JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }

        if (document is null)
        {
            return null;
        }

        var error = document["error"];

        var (type, message) = error switch
        {
            JsonObject nested => (Text(nested["type"]), Text(nested["message"])),
            not null => (Text(error), Text(document["message"])),
            _ => (Text(document["type"]), Text(document["message"])),
        };

        return type is null && message is null ? null : new UsageApiError(type, message);
    }

    private static string? Text(JsonNode? node)
    {
        var value = node?.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? node.GetValue<string>()
            : null;

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
