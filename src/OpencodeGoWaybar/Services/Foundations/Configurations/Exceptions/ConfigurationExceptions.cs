namespace OpencodeGoWaybar.Services.Foundations.Configurations.Exceptions;

/// <summary>
/// Categorizes a configuration failure after the foundation has logged it.
/// The inner exception retains the source-specific binding or provider detail.
/// </summary>
internal sealed class ConfigurationServiceException(Exception innerException)
    : Exception("The configuration service failed.", innerException);
