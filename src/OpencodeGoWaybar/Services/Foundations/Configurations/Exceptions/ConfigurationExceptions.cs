namespace OpencodeGoWaybar.Services.Foundations.Configurations.Exceptions;

internal sealed class ConfigurationServiceException(Exception innerException)
    : Exception("The configuration service failed.", innerException);
