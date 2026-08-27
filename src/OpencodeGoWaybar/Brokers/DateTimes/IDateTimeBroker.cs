namespace OpencodeGoWaybar.Brokers.DateTimes;

/// <summary>
/// Abstracts the system clock so business logic can treat the current moment as
/// a dependency rather than reaching for it directly.
/// </summary>
internal interface IDateTimeBroker
{
    DateTimeOffset GetCurrentDateTime();
}
