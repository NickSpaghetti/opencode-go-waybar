using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.OpenCodeAuths.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.OpenCodeAuth;

public sealed partial class OpenCodeAuthServiceTests
{
    [Fact]
    public void ShouldReturnNoKeyOnRetrieveApiKeyIfTheStoreIsAbsent()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var service = CreateService(CreateFailingBroker(new FileNotFoundException()), loggingBroker);

        // when
        var apiKey = service.RetrieveApiKey();

        // then
        Assert.Null(apiKey);
        loggingBroker.DidNotReceive().LogError(Arg.Any<Exception>());
    }

    [Fact]
    public void ShouldThrowOpenCodeAuthResponseExceptionOnRetrieveApiKeyIfTheStoreIsMalformedAndLogIt()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var service = CreateService(
            CreateFailingBroker(new System.Text.Json.JsonException("unterminated object")),
            loggingBroker);

        // when
        Action retrieveApiKey = () => service.RetrieveApiKey();

        // when and then
        Assert.Throws<OpenCodeAuthResponseException>(retrieveApiKey);
        loggingBroker.Received(1).LogError(Arg.Any<OpenCodeAuthResponseException>());
    }

    [Fact]
    public void ShouldThrowOpenCodeAuthUnavailableExceptionOnRetrieveApiKeyIfTheStoreIsUnreadableAndLogIt()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var service = CreateService(
            CreateFailingBroker(new UnauthorizedAccessException("denied")),
            loggingBroker);

        // when
        Action retrieveApiKey = () => service.RetrieveApiKey();

        // when and then
        Assert.Throws<OpenCodeAuthUnavailableException>(retrieveApiKey);
        loggingBroker.Received(1).LogError(Arg.Any<OpenCodeAuthUnavailableException>());
    }
}
