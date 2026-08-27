using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Brokers.Caches;

/// <summary>
/// The window cache file. One of two cache brokers over the same directory,
/// each with its own target file — §1.2.5's multiple-targets pattern, the same
/// reasoning that lets four brokers share the filesystem.
///
/// There are no lock routines. The write is atomic, and one writer per file means
/// there is no lost update to guard against.
/// </summary>
internal interface IUsageWindowCacheBroker
{
    /// <summary>Throws when no cache file has been written yet.</summary>
    ValueTask<UsageWindowCacheState?> ReadStateAsync(CancellationToken cancellationToken);

    ValueTask WriteStateAsync(UsageWindowCacheState state, CancellationToken cancellationToken);
}
