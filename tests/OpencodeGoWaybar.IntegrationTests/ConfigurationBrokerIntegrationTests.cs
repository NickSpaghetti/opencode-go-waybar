using Microsoft.Extensions.Configuration;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Models.Configurations;
using Xunit;

namespace OpencodeGoWaybar.IntegrationTests;

/// <summary>
/// Where the API key comes from is the broker's concern, not the secrets
/// service's, so provenance is proven here against the real configuration
/// sources rather than in a unit test that would have to drive them.
///
/// The assertion is deliberately "the environment variable wins": the broker
/// adds environment variables after the user-secrets store, so this holds on a
/// developer machine that already has a key on disk as well as in a clean
/// container.
/// </summary>
[Trait("Tier", "Integration")]
public sealed class ConfigurationBrokerIntegrationTests
{
    [Fact]
    public void ShouldSurfaceTheApiKeyEnvironmentVariableAboveOtherSources()
    {
        // given
        var broker = new ConfigurationBroker();
        var expectedApiKey = $"integration-{Guid.NewGuid():N}";

        var previousApiKey = Environment.GetEnvironmentVariable(
            OpenCodeGoOptions.ApiKeyEnvironmentVariable);

        Environment.SetEnvironmentVariable(
            OpenCodeGoOptions.ApiKeyEnvironmentVariable,
            expectedApiKey);

        try
        {
            // when
            IConfigurationRoot configuration = broker.Build(
                "/nonexistent/opencode-go-waybar/config.json");

            // then
            Assert.Equal(
                expectedApiKey,
                configuration[OpenCodeGoOptions.ApiKeyEnvironmentVariable]);

            (configuration as IDisposable)?.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                OpenCodeGoOptions.ApiKeyEnvironmentVariable,
                previousApiKey);
        }
    }
}
