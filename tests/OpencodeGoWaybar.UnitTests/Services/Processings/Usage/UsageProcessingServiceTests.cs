using Microsoft.Extensions.Options;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Apis.Usage;
using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Processings.Usage;
using OpencodeGoWaybar.Services.Foundations.Cache;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Services.Foundations.Usage;
using OpencodeGoWaybar.Services.Processings.Usage;
using Xunit;
using UsageModel = OpencodeGoWaybar.Brokers.Apis.Usage.Usage;

namespace OpencodeGoWaybar.UnitTests.Services.Processings.Usage;

public sealed class UsageProcessingServiceTests
{
    [Fact]
    public async Task ReturnsFreshCacheWithoutRefreshingDependencies()
    {
        var now = DateTimeOffset.UtcNow;
        var usage = CreateUsageResponse();
        var cache = Substitute.For<ICacheService>();
        cache.RetrieveStateAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<UsageCacheState?>(new UsageCacheState
            {
                Usage = usage,
                RecentDays = Array.Empty<RecentUsageDay>(),
                ApiRetrievedAt = now.AddMinutes(-1),
                DatabaseLastWriteTime = now.AddMinutes(-1),
            }));
        var database = Substitute.For<IOpenCodeDatabaseService>();
        database.RetrieveLastWriteTimeAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<DateTimeOffset?>(now.AddMinutes(-1)));
        var service = CreateService(cache, database, Substitute.For<IUsageService>());

        var result = await service.RetrieveUsageAsync(now, CancellationToken.None);

        Assert.Same(usage, result.Usage);
        await database.DidNotReceive().RetrieveMessagesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshesApiWithoutRefreshingUnchangedDatabase()
    {
        var now = DateTimeOffset.UtcNow;
        var usage = CreateUsageResponse();
        var cache = Substitute.For<ICacheService>();
        cache.RetrieveStateAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<UsageCacheState?>(new UsageCacheState
        {
            Usage = CreateUsageResponse(),
            RecentDays = Array.Empty<RecentUsageDay>(),
            ApiRetrievedAt = now.AddHours(-1),
            DatabaseLastWriteTime = now.AddMinutes(-1),
        }));
        cache.TryAcquireLockAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(true));
        var database = Substitute.For<IOpenCodeDatabaseService>();
        database.RetrieveLastWriteTimeAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<DateTimeOffset?>(now.AddMinutes(-1)));
        var usageService = Substitute.For<IUsageService>();
        usageService.RetrieveUsageAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(usage));
        var service = CreateService(cache, database, usageService);

        var result = await service.RetrieveUsageAsync(now, CancellationToken.None);

        Assert.Same(usage, result.Usage);
        await usageService.Received(1).RetrieveUsageAsync(Arg.Any<CancellationToken>());
        await database.DidNotReceive().RetrieveMessagesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshesDatabaseAndAggregatesOnlyOpenCodeGoMessages()
    {
        var now = DateTimeOffset.UtcNow;
        var cache = Substitute.For<ICacheService>();
        cache.RetrieveStateAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<UsageCacheState?>(new UsageCacheState
        {
            Usage = CreateUsageResponse(),
            RecentDays = Array.Empty<RecentUsageDay>(),
            ApiRetrievedAt = now.AddMinutes(-1),
            DatabaseLastWriteTime = now.AddHours(-1),
        }));
        cache.TryAcquireLockAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(true));
        var database = Substitute.For<IOpenCodeDatabaseService>();
        database.RetrieveLastWriteTimeAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<DateTimeOffset?>(now));
        database.RetrieveMessagesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<IReadOnlyList<OpenCodeMessage>>(
            new[]
            {
                new OpenCodeMessage(now, "{\"providerID\":\"opencode-go\",\"tokens\":{\"total\":10},\"cost\":0.1}"),
                new OpenCodeMessage(now, "{\"providerID\":\"opencode-go\",\"tokens\":{\"total\":20},\"cost\":0.2}"),
                new OpenCodeMessage(now, "{\"providerID\":\"other\",\"tokens\":{\"total\":99},\"cost\":9.9}"),
            }));
        var service = CreateService(cache, database, Substitute.For<IUsageService>());

        var result = await service.RetrieveUsageAsync(now, CancellationToken.None);

        var day = Assert.Single(result.RecentDays);
        Assert.Equal(30, day.Tokens);
        Assert.Equal(0.3m, day.Cost);
    }

    [Fact]
    public async Task ReturnsCachedStateWhenAnotherInvocationOwnsTheLock()
    {
        var now = DateTimeOffset.UtcNow;
        var usage = CreateUsageResponse();
        var cache = Substitute.For<ICacheService>();
        cache.RetrieveStateAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<UsageCacheState?>(new UsageCacheState
            {
                Usage = usage,
                RecentDays = Array.Empty<RecentUsageDay>(),
                ApiRetrievedAt = now.AddHours(-1),
                DatabaseLastWriteTime = now.AddHours(-1),
            }));
        cache.TryAcquireLockAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(false));
        var database = Substitute.For<IOpenCodeDatabaseService>();
        database.RetrieveLastWriteTimeAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<DateTimeOffset?>(now));
        var service = CreateService(cache, database, Substitute.For<IUsageService>());

        var result = await service.RetrieveUsageAsync(now, CancellationToken.None);

        Assert.Same(usage, result.Usage);
    }

    private static UsageProcessingService CreateService(
        ICacheService cache,
        IOpenCodeDatabaseService database,
        IUsageService usage) =>
        new(cache, database, usage, Options.Create(new OpenCodeGoOptions()));

    private static ICacheService CreateRefreshableCache()
    {
        var cache = Substitute.For<ICacheService>();
        cache.RetrieveStateAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<UsageCacheState?>(null));
        cache.TryAcquireLockAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(true));
        return cache;
    }

    private static UsageResponse CreateUsageResponse() => new(new UsageModel(
        new UsageWindow("ok", 10, DateTimeOffset.UtcNow),
        new UsageWindow("ok", 20, DateTimeOffset.UtcNow),
        new UsageWindow("ok", 30, DateTimeOffset.UtcNow)));
}
