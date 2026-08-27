using Microsoft.Extensions.Logging;

namespace OpencodeGoWaybar.Brokers.Loggings;

internal sealed class LoggingBroker(ILogger<LoggingBroker> logger) : ILoggingBroker
{
    /// <summary>Forwards an informational message to Microsoft.Extensions.Logging.</summary>
    public ValueTask LogInformationAsync(string message)
    {
        logger.LogInformation("{Message}", message);
        return ValueTask.CompletedTask;
    }

    /// <summary>Forwards a warning message to Microsoft.Extensions.Logging.</summary>
    public ValueTask LogWarningAsync(string message)
    {
        logger.LogWarning("{Message}", message);
        return ValueTask.CompletedTask;
    }

    /// <summary>Logs a categorized exception before a synchronous boundary rethrows it.</summary>
    public void LogError(Exception exception) =>
        logger.LogError(exception, "An opencode-go-waybar operation failed.");

    /// <summary>Logs a categorized exception for an asynchronous boundary.</summary>
    public ValueTask LogErrorAsync(Exception exception)
    {
        LogError(exception);
        return ValueTask.CompletedTask;
    }
}
