using Microsoft.Extensions.Configuration;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Brokers.Configurations;

internal sealed class ConfigurationBroker : IConfigurationBroker
{
    /// <summary>
    /// Assembles the configuration sources. Every source is optional, so a file
    /// that does not exist is the provider's concern rather than a decision
    /// taken here; the service supplies which path to use. User secrets are
    /// added unconditionally — the shipped binary reads them too, rather than
    /// behaving differently from the source you can see.
    /// </summary>
    public IConfigurationRoot Build(string configPath)
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonFile(configPath, optional: true);
        builder.AddUserSecrets<OpenCodeGoOptions>(optional: true);
        builder.AddEnvironmentVariables(prefix: OpenCodeGoOptions.EnvironmentVariablePrefix);
        builder.AddEnvironmentVariables();

        return builder.Build();
    }
}
