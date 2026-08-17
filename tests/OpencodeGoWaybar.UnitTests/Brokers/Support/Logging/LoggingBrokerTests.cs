using Microsoft.Extensions.Logging;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Support.Logging;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Brokers.Support.Logging;

public sealed class LoggingBrokerTests
{
    [Fact]
    public async Task LogsInformationMessageAtInformationLevel()
    {
        var logger = Substitute.For<ILogger<LoggingBroker>>();
        var broker = new LoggingBroker(logger);

        await broker.LogInformationAsync("started");

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task LogsWarningMessageAtWarningLevel()
    {
        var logger = Substitute.For<ILogger<LoggingBroker>>();
        var broker = new LoggingBroker(logger);

        await broker.LogWarningAsync("cache is stale");

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

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

    [Fact]
    public void LogsSynchronouslyAtErrorLevel()
    {
        var logger = Substitute.For<ILogger<LoggingBroker>>();
        var exception = new InvalidOperationException("test failure");
        var broker = new LoggingBroker(logger);

        broker.LogError(exception);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
