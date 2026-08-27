using NSubstitute;
using OpencodeGoWaybar.Brokers.Credentials;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.OpenCodeAuths;
using OpencodeGoWaybar.Services.Foundations.OpenCodeAuth;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.OpenCodeAuth;

public sealed partial class OpenCodeAuthServiceTests
{
    private static OpenCodeAuthService CreateService(
        Dictionary<string, OpenCodeAuthEntry> entries,
        ILoggingBroker? loggingBroker = null) =>
        CreateService(CreateBroker(entries), loggingBroker);

    private static OpenCodeAuthService CreateService(
        IOpenCodeAuthBroker authBroker,
        ILoggingBroker? loggingBroker = null,
        string authPath = "/tmp/auth.json") =>
        new(
            authBroker,
            loggingBroker ?? Substitute.For<ILoggingBroker>(),
            new OpenCodeGoOptions { AuthPath = authPath });

    private static IOpenCodeAuthBroker CreateBroker(Dictionary<string, OpenCodeAuthEntry> entries)
    {
        var authBroker = Substitute.For<IOpenCodeAuthBroker>();
        authBroker.ReadAuthEntries().Returns(entries);

        return authBroker;
    }

    private static IOpenCodeAuthBroker CreateFailingBroker(Exception exception)
    {
        var authBroker = Substitute.For<IOpenCodeAuthBroker>();
        authBroker.ReadAuthEntries().Returns(_ => throw exception);

        return authBroker;
    }
}
