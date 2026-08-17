namespace OpencodeGoWaybar.Services.Foundations.Processes.Exceptions;

/// <summary>Categorizes an unexpected process-detection failure.</summary>
internal sealed class ProcessServiceException(Exception innerException)
    : Exception("The process service failed.", innerException);
