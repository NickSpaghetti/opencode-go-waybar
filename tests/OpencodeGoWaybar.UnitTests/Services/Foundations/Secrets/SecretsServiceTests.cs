using Microsoft.Extensions.Configuration;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Secrets;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Secrets;

/// <summary>
/// Split out of ConfigurationService so each foundation service returns a
/// single entity contract (The Standard 2.0.2.3).
///
/// The configuration broker is substituted rather than constructed. It adds the
/// real user-secrets store unconditionally and by design, so a test driving the
/// real broker asserts against whatever key the developer happens to have on
/// disk — which is machine state, not this service's behaviour.
/// </summary>
public sealed partial class SecretsServiceTests
{
    private static SecretsService CreateService(
        IConfigurationBroker configurationBroker,
        ILoggingBroker loggingBroker) =>
        new(configurationBroker, loggingBroker);

    private static IConfigurationRoot CreateConfiguration(string? apiKey)
    {
        var values = new Dictionary<string, string?>();

        if (apiKey is not null)
        {
            values[OpenCodeGoOptions.ApiKeyEnvironmentVariable] = apiKey;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static string ExpandedDefaultConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return Path.Combine(home, OpenCodeGoOptions.DefaultConfigPath[2..]);
    }
}
