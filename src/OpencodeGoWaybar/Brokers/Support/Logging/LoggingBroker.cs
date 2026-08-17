using Microsoft.Extensions.Logging;

namespace OpencodeGoWaybar.Brokers.Support.Logging;

internal sealed class LoggingBroker(ILogger<LoggingBroker> logger) : ILoggingBroker
{
    public ValueTask LogErrorAsync(Exception exception)
    {
        logger.LogError(exception, "An opencode-go-waybar operation failed.");
        return ValueTask.CompletedTask;
    }
}
