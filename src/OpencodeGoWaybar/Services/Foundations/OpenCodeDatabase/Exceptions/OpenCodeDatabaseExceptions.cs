namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase.Exceptions;

internal sealed class OpenCodeDatabaseSchemaException(Exception innerException)
    : Exception("The OpenCode database does not contain the expected message schema.", innerException);

internal sealed class OpenCodeDatabaseUnavailableException(Exception innerException)
    : Exception("The OpenCode database could not be read.", innerException);

internal sealed class OpenCodeDatabaseResponseException(Exception innerException)
    : Exception("The OpenCode database broker returned invalid usage data.", innerException);

internal sealed class OpenCodeDatabaseServiceException(Exception innerException)
    : Exception("The database service failed.", innerException);
