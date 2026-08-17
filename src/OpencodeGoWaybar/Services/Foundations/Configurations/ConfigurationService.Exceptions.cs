using OpencodeGoWaybar.Services.Foundations.Configurations.Exceptions;
using Microsoft.Extensions.Options;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

internal sealed partial class ConfigurationService
{
    private T TryCatch<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (OptionsValidationException exception)
        {
            var validationException = new ConfigurationServiceException(exception);
            this._loggingBroker.LogErrorAsync(validationException).GetAwaiter().GetResult();
            throw validationException;
        }
        catch (ConfigurationServiceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var serviceException = new ConfigurationServiceException(exception);
            this._loggingBroker.LogErrorAsync(serviceException).GetAwaiter().GetResult();
            throw serviceException;
        }
    }
}
