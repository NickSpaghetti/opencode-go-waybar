namespace OpencodeGoWaybar.Models.Processes.Exceptions;

/// <summary>Indicates that the operating system process table could not be read.</summary>
internal sealed class ProcessTableUnavailableException(Exception innerException)
    : Exception("The operating system process table could not be read.", innerException);

/// <summary>Indicates that the process broker returned invalid process data.</summary>
internal sealed class ProcessResponseException(Exception innerException)
    : Exception("The process broker returned invalid process data.", innerException);

/// <summary>Categorizes an unexpected process-detection failure.</summary>
internal sealed class ProcessServiceException(Exception innerException)
    : Exception("The process service failed.", innerException);
