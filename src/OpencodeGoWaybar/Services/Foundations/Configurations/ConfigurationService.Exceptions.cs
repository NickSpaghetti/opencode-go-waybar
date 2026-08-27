using OpencodeGoWaybar.Models.Configurations.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

internal sealed partial class ConfigurationService
{
    private T TryCatch<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (InvalidOpenCodeGoOptionsException exception)
        {
            var validationException = new ConfigurationServiceException(exception);
            _loggingBroker.LogError(validationException);
            throw validationException;
        }
        catch (ConfigurationServiceException exception)
        {
            _loggingBroker.LogError(exception);
            throw;
        }
        catch (Exception exception)
        {
            var serviceException = new ConfigurationServiceException(exception);
            _loggingBroker.LogError(serviceException);
            throw serviceException;
        }
    }
}
