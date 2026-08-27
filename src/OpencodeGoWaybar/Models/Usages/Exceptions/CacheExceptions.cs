namespace OpencodeGoWaybar.Models.Usages.Exceptions;

/// <summary>Indicates that the cache was asked to do something invalid.</summary>
internal sealed class CacheValidationException(Exception innerException)
    : Exception("The cache service received invalid input.", innerException);

/// <summary>Indicates that the cache file or its lock could not be accessed.</summary>
internal sealed class CacheUnavailableException(Exception innerException)
    : Exception("The usage cache could not be read or written.", innerException);

/// <summary>Indicates that the cache file is present but unreadable.</summary>
internal sealed class CacheResponseException(Exception innerException)
    : Exception("The usage cache does not contain valid state.", innerException);

/// <summary>Categorizes an unexpected cache-service failure.</summary>
internal sealed class CacheServiceException(Exception innerException)
    : Exception("The cache service failed.", innerException);
