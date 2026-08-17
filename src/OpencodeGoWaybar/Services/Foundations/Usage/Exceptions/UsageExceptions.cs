namespace OpencodeGoWaybar.Services.Foundations.Usage.Exceptions;

internal sealed class UsageCredentialsMissingException()
    : Exception("The OPENCODE_GO_API_KEY secret is not configured.");

internal sealed class UsageAuthenticationException(Exception innerException)
    : Exception("The OpenCode Go API rejected the configured credentials.", innerException);

internal sealed class UsageRateLimitedException(Exception innerException)
    : Exception("The OpenCode Go API rate-limited the request.", innerException);

internal sealed class UsageApiUnavailableException(Exception innerException)
    : Exception("The OpenCode Go API could not be reached.", innerException);

internal sealed class UsageApiResponseException(Exception innerException)
    : Exception("The OpenCode Go API returned an invalid response.", innerException);

internal sealed class UsageServiceException(Exception innerException)
    : Exception("The usage service failed.", innerException);

 
