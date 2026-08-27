using NSubstitute;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Secrets;

public sealed partial class SecretsServiceTests
{
    [Fact]
    public void ShouldRetrieveApiKeyFromConfiguration()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var configurationBroker = Substitute.For<IConfigurationBroker>();
        configurationBroker.Build(Arg.Any<string>())
            .Returns(CreateConfiguration(apiKey: "test-api-key"));

        // when
        OpenCodeGoSecrets secrets =
            CreateService(configurationBroker, loggingBroker).RetrieveSecrets();

        // then
        Assert.Equal("test-api-key", secrets.ApiKey);
        configurationBroker.Received(1).Build(Arg.Any<string>());
        loggingBroker.DidNotReceive().LogError(Arg.Any<Exception>());
    }

    [Fact]
    public void ShouldRetrieveNoApiKeyWhenConfigurationHasNone()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var configurationBroker = Substitute.For<IConfigurationBroker>();
        configurationBroker.Build(Arg.Any<string>())
            .Returns(CreateConfiguration(apiKey: null));

        // when
        OpenCodeGoSecrets secrets =
            CreateService(configurationBroker, loggingBroker).RetrieveSecrets();

        // then
        Assert.Null(secrets.ApiKey);
        configurationBroker.Received(1).Build(Arg.Any<string>());
        loggingBroker.DidNotReceive().LogError(Arg.Any<Exception>());
    }

    [Fact]
    public void ShouldExpandTheHomeRelativeDefaultConfigPath()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var configurationBroker = Substitute.For<IConfigurationBroker>();
        configurationBroker.Build(Arg.Any<string>())
            .Returns(CreateConfiguration(apiKey: null));

        string expectedConfigPath = ExpandedDefaultConfigPath();

        // when
        CreateService(configurationBroker, loggingBroker).RetrieveSecrets();

        // then
        configurationBroker.Received(1).Build(expectedConfigPath);
    }

    [Fact]
    public void ShouldRetrieveApiKeyFromTheSuppliedConfigPath()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var configurationBroker = Substitute.For<IConfigurationBroker>();
        configurationBroker.Build(Arg.Any<string>())
            .Returns(CreateConfiguration(apiKey: "supplied-api-key"));

        var suppliedConfigPath = "/etc/opencode-go-waybar/config.json";

        // when
        OpenCodeGoSecrets secrets = CreateService(configurationBroker, loggingBroker)
            .RetrieveSecrets(suppliedConfigPath);

        // then
        Assert.Equal("supplied-api-key", secrets.ApiKey);
        configurationBroker.Received(1).Build(suppliedConfigPath);
    }
}
