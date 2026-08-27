using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Configurations.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Secrets;

internal sealed partial class SecretsService
{
    private OpenCodeGoSecrets TryCatch(Func<OpenCodeGoSecrets> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception exception)
        {
            var secretsServiceException = new SecretsServiceException(exception);
            loggingBroker.LogError(secretsServiceException);

            throw secretsServiceException;
        }
    }
}
