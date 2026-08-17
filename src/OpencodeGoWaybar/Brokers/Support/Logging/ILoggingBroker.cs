namespace OpencodeGoWaybar.Brokers.Support.Logging;

/// <summary>Provides the logging operations used by application boundaries.</summary>
internal interface ILoggingBroker
{
    /// <summary>Writes an informational diagnostic message.</summary>
    ValueTask LogInformationAsync(string message);

    /// <summary>Writes a warning diagnostic message.</summary>
    ValueTask LogWarningAsync(string message);

    /// <summary>Writes an error synchronously for synchronous boundaries.</summary>
    void LogError(Exception exception);

    /// <summary>Writes an error without blocking asynchronous boundaries.</summary>
    ValueTask LogErrorAsync(Exception exception);
}
