namespace OpencodeGoWaybar.TestSupport;

/// <summary>
/// A generous per-test deadline. These tests start real editors and agents, so
/// the bound exists to turn a hang into a readable failure, not to be tight.
/// </summary>
internal static class E2eTimeout
{
    public static CancellationTokenSource Create() =>
        new(TimeSpan.FromMinutes(3));
}
