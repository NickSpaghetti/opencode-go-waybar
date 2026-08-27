namespace OpencodeGoWaybar.Brokers.DateTimes;

internal sealed class DateTimeBroker : IDateTimeBroker
{
    public DateTimeOffset GetCurrentDateTime() => DateTimeOffset.UtcNow;
}

