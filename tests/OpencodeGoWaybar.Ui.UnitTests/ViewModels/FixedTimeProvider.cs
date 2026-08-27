namespace OpencodeGoWaybar.Ui.UnitTests.ViewModels;

/// <summary>
/// A clock the tests own. Countdowns and "is this today" are the two things a
/// view model cannot be checked on while it reads the ambient clock.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
