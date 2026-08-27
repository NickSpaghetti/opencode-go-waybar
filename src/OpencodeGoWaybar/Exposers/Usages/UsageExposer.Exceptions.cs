using OpencodeGoWaybar.Models.Usages.Exceptions;

namespace OpencodeGoWaybar.Exposers.Usages;

internal sealed partial class UsageExposer
{
    /// <summary>
    /// What went wrong, in the API's own words where it supplied them. Only the
    /// parsed type and message are rendered — never an exception message or a
    /// raw response body, either of which could carry request details.
    ///
    /// This deliberately duplicates the Waybar exposer's mapping rather than
    /// sharing it. The two are independent exposure flows, and The Standard
    /// prefers duplication over the horizontal entanglement a shared helper
    /// would create (0.2.0.0.1.0); either surface must be free to reword its own
    /// failures without disturbing the other.
    /// </summary>
    private static string DescribeFailure(Exception exception)
    {
        if (exception is IUsageApiFailure { ApiError: { } apiError })
        {
            var described = Sanitize(apiError.Message) ?? Sanitize(apiError.Type);

            if (described is not null)
            {
                var qualifier = Sanitize(apiError.Type);

                return qualifier is null || qualifier == described
                    ? $"OpenCode Go: {described}"
                    : $"OpenCode Go: {described} ({qualifier})";
            }
        }

        return exception switch
        {
            UsageCredentialsMissingException => "No OpenCode Go API key configured",
            UsageAuthenticationException => "OpenCode Go rejected the API key",
            UsageRateLimitedException => "OpenCode Go is rate limiting requests",
            UsageApiUnavailableException => "OpenCode Go could not be reached",
            UsageApiResponseException => "OpenCode Go returned unexpected data",
            TimeoutException => "OpenCode Go usage refresh timed out",
            _ => "OpenCode Go usage unavailable",
        };
    }

    /// <summary>
    /// Keeps a hostile or oversized body from reshaping the message: the value is
    /// collapsed onto one line and truncated.
    /// </summary>
    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var collapsed = string.Join(
            ' ',
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return collapsed.Length <= 120 ? collapsed : collapsed[..117] + "...";
    }
}
