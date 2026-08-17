using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

internal sealed partial class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationBroker _configurationBroker;
    private readonly IValidateOptions<OpenCodeGoOptions> _validator;
    private readonly ILoggingBroker _loggingBroker;

    public ConfigurationService(
        IConfigurationBroker configurationBroker,
        IValidateOptions<OpenCodeGoOptions> validator,
        ILoggingBroker loggingBroker)
    {
        this._configurationBroker = configurationBroker;
        this._validator = validator;
        this._loggingBroker = loggingBroker;
    }
    public IOptions<OpenCodeGoOptions> RetrieveOptions(string? configPath = null)
    {
        return TryCatch(() => RetrieveOptionsCore(configPath));
    }

    public IOptions<OpenCodeGoSecrets> RetrieveSecrets(string? configPath = null)
    {
        return TryCatch(() => RetrieveSecretsCore(configPath));
    }

    private IOptions<OpenCodeGoOptions> RetrieveOptionsCore(string? configPath)
    {
        var configuration = _configurationBroker.Build(configPath);
        try
        {
            var options = new OpenCodeGoOptions();
            configuration.Bind(options);
            ValidateOptions(options);
            return Options.Create(options);
        }
        finally
        {
            (configuration as IDisposable)?.Dispose();
        }
    }

    private IOptions<OpenCodeGoSecrets> RetrieveSecretsCore(string? configPath)
    {
        var configuration = _configurationBroker.Build(configPath);
        try
        {
            return Options.Create(new OpenCodeGoSecrets
            {
                ApiKey = configuration[OpenCodeGoOptions.ApiKeyEnvironmentVariable],
            });
        }
        finally
        {
            (configuration as IDisposable)?.Dispose();
        }
    }

    private void ValidateOptions(OpenCodeGoOptions options)
    {
        var result = _validator.Validate(Options.DefaultName, options);
        if (result.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(OpenCodeGoOptions), result.Failures!);
        }
    }
}
