namespace OpencodeGoWaybar.Models.Configurations.Exceptions;

/// <summary>Categorizes an unexpected secrets-service failure.</summary>
internal sealed class SecretsServiceException(Exception innerException)
    : Exception("The secrets service failed.", innerException);
