using Microsoft.Extensions.Configuration;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Secrets;

/// <summary>
/// Reads the API key out of the configuration sources. Split from
/// ConfigurationService so each service returns a single entity contract
/// (The Standard 2.0.2.3); both sit over the same configuration broker, the way
/// one storage broker serves many foundation services.
/// </summary>
internal sealed partial class SecretsService(
    IConfigurationBroker configurationBroker,
    ILoggingBroker loggingBroker) : ISecretsService
{
    public OpenCodeGoSecrets RetrieveSecrets(string? configPath = null) =>
        TryCatch(() => RetrieveSecretsCore(configPath));

    private OpenCodeGoSecrets RetrieveSecretsCore(string? configPath)
    {
        var configuration = configurationBroker.Build(
            ExpandHomeRelativePath(configPath ?? OpenCodeGoOptions.DefaultConfigPath));

        try
        {
            return new OpenCodeGoSecrets
            {
                ApiKey = configuration[OpenCodeGoOptions.ApiKeyEnvironmentVariable],
            };
        }
        finally
        {
            (configuration as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Turns the documented `~/...` default into a real path; nothing else in
    /// the process expands it.
    /// </summary>
    private static string ExpandHomeRelativePath(string path)
    {
        if (!path.StartsWith("~/", StringComparison.Ordinal))
        {
            return path;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return string.IsNullOrEmpty(home) ? path : Path.Combine(home, path[2..]);
    }
}
