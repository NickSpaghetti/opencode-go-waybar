using OpencodeGoWaybar.Brokers.Storages.Cache;
using OpencodeGoWaybar.Models.Processings.Usage;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Brokers.Storages.Cache;

public sealed class CacheBrokerTests
{
    [Fact]
    public async Task StoresAndRetrievesCacheState()
    {
        var path = CreatePath();
        try
        {
            var broker = new CacheBroker(path);
            var state = new UsageCacheState
            {
                RecentDays = new[] { new RecentUsageDay(new DateOnly(2026, 8, 16), 42, 0.12m) },
                ApiRetrievedAt = DateTimeOffset.UtcNow,
            };

            await broker.StoreStateAsync(state, CancellationToken.None);
            var actual = await broker.RetrieveStateAsync(CancellationToken.None);

            Assert.NotNull(actual);
            Assert.Equal(42, Assert.Single(actual!.RecentDays).Tokens);
        }
        finally
        {
            DeleteCacheFiles(path);
        }
    }

    [Fact]
    public async Task AllowsOnlyOneOwnerOfTheCacheLock()
    {
        var path = CreatePath();
        try
        {
            var first = new CacheBroker(path);
            var second = new CacheBroker(path);

            Assert.True(await first.TryAcquireLockAsync(TimeSpan.FromMinutes(2), CancellationToken.None));
            Assert.False(await second.TryAcquireLockAsync(TimeSpan.FromMinutes(2), CancellationToken.None));

            await first.ReleaseLockAsync(CancellationToken.None);

            Assert.True(await second.TryAcquireLockAsync(TimeSpan.FromMinutes(2), CancellationToken.None));
            await second.ReleaseLockAsync(CancellationToken.None);
        }
        finally
        {
            DeleteCacheFiles(path);
        }
    }

    private static string CreatePath() =>
        Path.Combine(Path.GetTempPath(), $"opencode-go-cache-{Guid.NewGuid():N}", "state.json");

    private static void DeleteCacheFiles(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
