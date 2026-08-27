using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Models.Usages.Exceptions;

/// <summary>Indicates that the OpenCode Go API key was not supplied.</summary>
internal sealed class UsageCredentialsMissingException()
    : Exception("The OPENCODE_GO_API_KEY secret is not configured.");

/// <summary>Indicates that the upstream API rejected the configured credentials.</summary>
internal sealed class UsageAuthenticationException(Exception innerException, UsageApiError? apiError = null)
    : Exception("The OpenCode Go API rejected the configured credentials.", innerException), IUsageApiFailure
{
    public UsageApiError? ApiError { get; } = apiError;
}

/// <summary>Indicates that the upstream API rejected the request rate.</summary>
internal sealed class UsageRateLimitedException(Exception innerException, UsageApiError? apiError = null)
    : Exception("The OpenCode Go API rate-limited the request.", innerException), IUsageApiFailure
{
    public UsageApiError? ApiError { get; } = apiError;
}

/// <summary>Indicates that the upstream API could not be reached.</summary>
internal sealed class UsageApiUnavailableException(Exception innerException, UsageApiError? apiError = null)
    : Exception("The OpenCode Go API could not be reached.", innerException), IUsageApiFailure
{
    public UsageApiError? ApiError { get; } = apiError;
}

/// <summary>Indicates that the upstream API returned an unusable response.</summary>
internal sealed class UsageApiResponseException(Exception innerException)
    : Exception("The OpenCode Go API returned an invalid response.", innerException);

/// <summary>Categorizes an unexpected usage-service failure.</summary>
internal sealed class UsageServiceException(Exception innerException)
    : Exception("The usage service failed.", innerException);
