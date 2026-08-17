using Microsoft.Extensions.Logging;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Support.Logging;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Brokers.Support.Logging;

public sealed class LoggingBrokerTests
{
    [Fact]
    public async Task LogsTheCompleteExceptionAtErrorLevel()
    {
        var logger = Substitute.For<ILogger<LoggingBroker>>();
        var exception = new InvalidOperationException("test failure");
        var broker = new LoggingBroker(logger);

        await broker.LogErrorAsync(exception);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
