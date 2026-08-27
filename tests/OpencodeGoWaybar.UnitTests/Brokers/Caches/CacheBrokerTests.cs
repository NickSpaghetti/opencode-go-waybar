using OpencodeGoWaybar.Brokers.Caches;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Brokers.Caches;

/// <summary>
/// Two brokers over one directory, each owning its own file — §1.2.5's
/// multiple-targets pattern.
///
/// There are no lock tests any more. A lock existed only because two independent
/// halves shared one file and each writer had to rewrite the other's; one writer
/// per file removes the lost update the lock was guarding, and the write was
/// already atomic via temp-file-and-rename.
/// </summary>
public sealed class CacheBrokerTests : IDisposable
{
    private readonly string cacheDirectory = Path.Combine(
        Path.GetTempPath(),
        $"opencode-go-cache-{Guid.NewGuid():N}");

    private OpenCodeGoOptions Options => new() { CacheDirectory = this.cacheDirectory };

    [Fact]
    public async Task ShouldStoreAndRetrieveWindowStateAsync()
    {
        // given
        var broker = new UsageWindowCacheBroker(Options);
        var retrievedAt = new DateTimeOffset(2026, 8, 20, 15, 17, 0, TimeSpan.Zero);

        var state = new UsageWindowCacheState
        {
            Usage = new UsageResponse(new Usage(
                new UsageWindow("ok", 61, retrievedAt),
                new UsageWindow("ok", 24, retrievedAt),
                new UsageWindow("ok", 12, retrievedAt))),
            ApiRetrievedAt = retrievedAt,
        };

        // when
        await broker.WriteStateAsync(state, CancellationToken.None);
        UsageWindowCacheState? actual = await broker.ReadStateAsync(CancellationToken.None);

        // then
        Assert.NotNull(actual);
        Assert.Equal(retrievedAt, actual.ApiRetrievedAt);
        Assert.Equal(61, actual.Usage!.Usage.Rolling.Percent);
    }

    [Fact]
    public async Task ShouldStoreAndRetrieveHistoryStateAsync()
    {
        // given
        var broker = new UsageHistoryCacheBroker(Options);
        var writeTime = new DateTimeOffset(2026, 8, 20, 14, 22, 51, TimeSpan.Zero);

        var state = new UsageHistoryCacheState
        {
            RecentDays = [new RecentUsageDay(new DateOnly(2026, 8, 20), 198_402, 2.94m)],
            DatabaseLastWriteTime = writeTime,
        };

        // when
        await broker.WriteStateAsync(state, CancellationToken.None);
        UsageHistoryCacheState? actual = await broker.ReadStateAsync(CancellationToken.None);

        // then
        Assert.NotNull(actual);
        Assert.Equal(writeTime, actual.DatabaseLastWriteTime);
        Assert.Equal(198_402, Assert.Single(actual.RecentDays).Tokens);
    }

    [Fact]
    public async Task ShouldWriteEachHalfToItsOwnFileAsync()
    {
        // given both brokers over the same directory
        var windowBroker = new UsageWindowCacheBroker(Options);
        var historyBroker = new UsageHistoryCacheBroker(Options);

        // when each writes, in either order
        await windowBroker.WriteStateAsync(
            new UsageWindowCacheState { ApiRetrievedAt = DateTimeOffset.UnixEpoch },
            CancellationToken.None);

        await historyBroker.WriteStateAsync(
            new UsageHistoryCacheState { DatabaseLastWriteTime = DateTimeOffset.UnixEpoch },
            CancellationToken.None);

        // then two files exist and neither write erased the other — the invariant
        // the shared file needed a lock to hold
        string[] files = [.. Directory.GetFiles(this.cacheDirectory).Select(Path.GetFileName)!];
        Assert.Equal(2, files.Length);
        Assert.Contains("windows.json", files);
        Assert.Contains("history.json", files);
        Assert.NotNull(await windowBroker.ReadStateAsync(CancellationToken.None));
        Assert.NotNull(await historyBroker.ReadStateAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ShouldLeaveNoTemporaryFilesBehindAsync()
    {
        // given
        var broker = new UsageWindowCacheBroker(Options);

        // when
        await broker.WriteStateAsync(new UsageWindowCacheState(), CancellationToken.None);

        // then the temp-and-rename leaves nothing to clean up — and there is no
        // .lock file to be orphaned either
        Assert.Empty(Directory.GetFiles(this.cacheDirectory, "*.tmp"));
        Assert.Empty(Directory.GetFiles(this.cacheDirectory, "*.lock"));
    }

    [Fact]
    public async Task ShouldThrowOnReadWhenNoCacheHasBeenWrittenAsync()
    {
        // given
        var broker = new UsageWindowCacheBroker(Options);

        // when and then — the service localises this into "nothing cached yet"
        await Assert.ThrowsAnyAsync<IOException>(() =>
            broker.ReadStateAsync(CancellationToken.None).AsTask());
    }

    public void Dispose()
    {
        if (Directory.Exists(this.cacheDirectory))
        {
            Directory.Delete(this.cacheDirectory, recursive: true);
        }
    }
}
