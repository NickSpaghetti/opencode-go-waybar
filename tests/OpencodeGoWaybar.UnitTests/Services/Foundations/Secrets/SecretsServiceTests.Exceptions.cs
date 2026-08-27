using Microsoft.Extensions.Configuration;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Secrets;

public sealed partial class SecretsServiceTests
{
    [Fact]
    public void ShouldThrowSecretsServiceExceptionOnRetrieveIfServiceErrorOccursAndLogIt()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var configurationBroker = Substitute.For<IConfigurationBroker>();
        configurationBroker.Build(Arg.Any<string>())
            .Returns<IConfigurationRoot>(_ => throw new FormatException("unexpected"));

        // when and then
        Assert.Throws<SecretsServiceException>(() =>
            CreateService(configurationBroker, loggingBroker).RetrieveSecrets());
        loggingBroker.Received(1).LogError(Arg.Any<SecretsServiceException>());
    }
}
