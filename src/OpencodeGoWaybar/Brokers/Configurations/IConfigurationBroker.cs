using Microsoft.Extensions.Configuration;

namespace OpencodeGoWaybar.Brokers.Configurations;

internal interface IConfigurationBroker
{
    IConfigurationRoot Build(string configPath);
}
