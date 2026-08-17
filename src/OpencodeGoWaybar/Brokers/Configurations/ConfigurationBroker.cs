using Microsoft.Extensions.Configuration;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Brokers.Configurations;

internal sealed class ConfigurationBroker : IConfigurationBroker
{
    public IConfigurationRoot Build(string? configPath)
    {
        var builder = new ConfigurationBuilder();

        if (configPath is not null && File.Exists(configPath))
        {
            builder.AddJsonFile(configPath, optional: true);
        }

#if DEBUG
        builder.AddUserSecrets<OpenCodeGoOptions>(optional: true);
#endif

        builder.AddEnvironmentVariables(prefix: OpenCodeGoOptions.EnvironmentVariablePrefix);
        builder.AddEnvironmentVariables();

        return builder.Build();
    }
}
