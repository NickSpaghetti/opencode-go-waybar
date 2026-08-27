namespace OpencodeGoWaybar.Models.OpenCodeAuths.Exceptions;

/// <summary>Indicates that opencode's credential store could not be read.</summary>
internal sealed class OpenCodeAuthUnavailableException(Exception innerException)
    : Exception("The opencode credential store could not be read.", innerException);

/// <summary>Indicates that the credential store is present but unusable.</summary>
internal sealed class OpenCodeAuthResponseException(Exception innerException)
    : Exception("The opencode credential store is not valid JSON.", innerException);

/// <summary>Categorizes an unexpected credential-store failure.</summary>
internal sealed class OpenCodeAuthServiceException(Exception innerException)
    : Exception("The opencode auth service failed.", innerException);
