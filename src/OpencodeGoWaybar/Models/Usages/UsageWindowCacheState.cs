namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// What the module remembers about the allowance windows between polls.
///
/// Its own file, and its own writer. The history slice used to live alongside it
/// in one object, which forced each writer to rewrite the other's half and made a
/// lock necessary to stop them losing each other's updates. Nothing was ever
/// shared between the two — they were only co-located.
/// </summary>
internal sealed class UsageWindowCacheState
{
    public UsageResponse? Usage { get; set; }

    public DateTimeOffset ApiRetrievedAt { get; set; }
}
