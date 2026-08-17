namespace OpencodeGoWaybar.Brokers.Support.Logging;

internal interface ILoggingBroker
{
    ValueTask LogErrorAsync(Exception exception);
}
