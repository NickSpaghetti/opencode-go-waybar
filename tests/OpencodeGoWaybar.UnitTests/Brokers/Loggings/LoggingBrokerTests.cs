using Microsoft.Extensions.Logging;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Brokers.Loggings;

public sealed class LoggingBrokerTests
{
    [Fact]
    public async Task ShouldLogInformationMessageAtInformationLevelAsync()
    {
        // given
        var logger = Substitute.For<ILogger<LoggingBroker>>();
        var broker = new LoggingBroker(logger);

        // when
        await broker.LogInformationAsync("started");

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ShouldLogWarningMessageAtWarningLevelAsync()
    {
        // given
        var logger = Substitute.For<ILogger<LoggingBroker>>();
        var broker = new LoggingBroker(logger);

        // when
        await broker.LogWarningAsync("cache is stale");

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ShouldLogTheCompleteExceptionAtErrorLevelAsync()
    {
        // given
        var logger = Substitute.For<ILogger<LoggingBroker>>();
        var exception = new InvalidOperationException("test failure");
        var broker = new LoggingBroker(logger);

        // when
        await broker.LogErrorAsync(exception);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ShouldLogSynchronouslyAtErrorLevel()
    {
        // given
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
