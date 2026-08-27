using NSubstitute;
using OpencodeGoWaybar.Brokers.Credentials;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.OpenCodeAuths.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.OpenCodeAuth;

public sealed partial class OpenCodeAuthServiceTests
{
    [Fact]
    public void ShouldThrowOpenCodeAuthUnavailableExceptionOnRetrieveApiKeyIfAuthPathIsBlankAndLogIt()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var authBroker = Substitute.For<IOpenCodeAuthBroker>();
        var service = CreateService(authBroker, loggingBroker, authPath: "  ");

        // when
        Action retrieveApiKey = () => service.RetrieveApiKey();

        // when and then
        Assert.Throws<OpenCodeAuthUnavailableException>(retrieveApiKey);
        authBroker.DidNotReceive().ReadAuthEntries();
        loggingBroker.Received(1).LogError(Arg.Any<OpenCodeAuthUnavailableException>());
    }
}
