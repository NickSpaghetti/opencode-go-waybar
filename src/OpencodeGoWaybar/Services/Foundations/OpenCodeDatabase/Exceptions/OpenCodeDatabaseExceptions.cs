namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase.Exceptions;

/// <summary>Indicates that the OpenCode database schema is unsupported.</summary>
internal sealed class OpenCodeDatabaseSchemaException(Exception innerException)
    : Exception("The OpenCode database does not contain the expected message schema.", innerException);

/// <summary>Indicates that the OpenCode database could not be accessed.</summary>
internal sealed class OpenCodeDatabaseUnavailableException(Exception innerException)
    : Exception("The OpenCode database could not be read.", innerException);

/// <summary>Indicates that the database broker returned invalid usage data.</summary>
internal sealed class OpenCodeDatabaseResponseException(Exception innerException)
    : Exception("The OpenCode database broker returned invalid usage data.", innerException);

/// <summary>Categorizes an unexpected database-service failure.</summary>
internal sealed class OpenCodeDatabaseServiceException(Exception innerException)
    : Exception("The database service failed.", innerException);
