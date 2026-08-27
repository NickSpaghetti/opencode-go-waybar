using NSubstitute;
using OpencodeGoWaybar.Models.OpenCodeMessages.Exceptions;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Services.Foundations.UsageHistoryCache;
using OpencodeGoWaybar.Services.Orchestrations.UsageHistory;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Orchestrations.UsageHistory;

/// <summary>
/// The history half of the usage flow. Sole writer of its own cache file, so the
/// lock stubs and the slice-preservation guard are gone with the shared file.
/// </summary>
public sealed class UsageHistoryOrchestrationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 15, 17, 0, TimeSpan.Zero);

    [Fact]
    public async Task ShouldNotTouchTheDatabaseWhenItIsUnchangedAsync()
    {
        // given a cached copy taken at the database's current write time
        var writeTime = Now.AddMinutes(-1);
        IUsageHistoryCacheService cacheService = CreateCacheService(CreateState(writeTime));
        IOpenCodeDatabaseService databaseService = CreateDatabaseService(writeTime);

        // when
        UsageHistorySnapshot snapshot = await new UsageHistoryOrchestrationService(
            cacheService, databaseService).RetrieveHistoryAsync(Now, CancellationToken.None);

        // then
        Assert.Single(snapshot.RecordedDays);
        await databaseService.DidNotReceive().RetrieveRecentUsageDaysAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cacheService.DidNotReceive().StoreStateAsync(
            Arg.Any<UsageHistoryCacheState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldRetrieveRecentUsageDaysWhenTheDatabaseChangedAsync()
    {
        // given a database written since the cached copy
        IUsageHistoryCacheService cacheService = CreateCacheService(CreateState(Now.AddHours(-1)));

        IOpenCodeDatabaseService databaseService = CreateDatabaseService(
            Now,
            [new RecentUsageDay(new DateOnly(2026, 8, 20), 30, 0.3m)]);

        // when
        UsageHistorySnapshot snapshot = await new UsageHistoryOrchestrationService(
            cacheService, databaseService).RetrieveHistoryAsync(Now, CancellationToken.None);

        // then
        var day = Assert.Single(snapshot.RecordedDays);
        Assert.Equal(30, day.Tokens);
        Assert.Equal(0.3m, day.Cost);
        Assert.Equal(30, snapshot.TotalTokens);
        await cacheService.Received(1).StoreStateAsync(
            Arg.Is<UsageHistoryCacheState>(written => written.DatabaseLastWriteTime == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldKeepTheCachedDaysWhenTheDatabaseCannotBeReadAsync()
    {
        // given a database that will not open
        IUsageHistoryCacheService cacheService = CreateCacheService(CreateState(Now.AddHours(-1)));

        var databaseService = Substitute.For<IOpenCodeDatabaseService>();
        databaseService.RetrieveLastWriteTimeAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<DateTimeOffset?>(Now));
        databaseService.RetrieveRecentUsageDaysAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<IReadOnlyList<RecentUsageDay>>>(_ =>
                throw new OpenCodeDatabaseUnavailableException(new IOException("locked")));

        // when
        UsageHistorySnapshot snapshot = await new UsageHistoryOrchestrationService(
            cacheService, databaseService).RetrieveHistoryAsync(Now, CancellationToken.None);

        // then the failure costs the token count, not the whole payload
        Assert.Single(snapshot.RecordedDays);
        Assert.Equal(99, snapshot.TotalTokens);
    }

    [Fact]
    public async Task ShouldRefreshWhenNothingHasBeenCachedYetAsync()
    {
        // given a cold cache — also the migration path off the old single file
        IUsageHistoryCacheService cacheService = CreateCacheService(state: null);

        IOpenCodeDatabaseService databaseService = CreateDatabaseService(
            Now,
            [new RecentUsageDay(new DateOnly(2026, 8, 20), 7, 0.1m)]);

        // when
        UsageHistorySnapshot snapshot = await new UsageHistoryOrchestrationService(
            cacheService, databaseService).RetrieveHistoryAsync(Now, CancellationToken.None);

        // then
        Assert.Single(snapshot.RecordedDays);
        Assert.Equal(7, snapshot.TotalTokens);
    }

    [Fact]
    public async Task ShouldTotalTheRecentDaysAsync()
    {
        // given
        UsageHistoryCacheState state = CreateState(Now.AddMinutes(-1));
        state.RecentDays =
        [
            new RecentUsageDay(new DateOnly(2026, 8, 20), 198_402, 2.94m),
            new RecentUsageDay(new DateOnly(2026, 8, 19), 24_118, 0.36m),
        ];

        // when
        UsageHistorySnapshot snapshot = await new UsageHistoryOrchestrationService(
            CreateCacheService(state), CreateDatabaseService(Now.AddMinutes(-1)))
            .RetrieveHistoryAsync(Now, CancellationToken.None);

        // then the sum lives here, because an exposer may not iterate
        Assert.Equal(222_520, snapshot.TotalTokens);
    }

    private static IUsageHistoryCacheService CreateCacheService(UsageHistoryCacheState? state)
    {
        var cacheService = Substitute.For<IUsageHistoryCacheService>();

        cacheService.RetrieveStateAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(state));

        return cacheService;
    }

    private static IOpenCodeDatabaseService CreateDatabaseService(
        DateTimeOffset? writeTime,
        IReadOnlyList<RecentUsageDay>? recentDays = null)
    {
        var databaseService = Substitute.For<IOpenCodeDatabaseService>();

        databaseService.RetrieveLastWriteTimeAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(writeTime));

        databaseService.RetrieveRecentUsageDaysAsync(
                Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(recentDays ?? []));

        return databaseService;
    }

    private static UsageHistoryCacheState CreateState(DateTimeOffset databaseWriteTime) =>
        new()
        {
            RecentDays = [new RecentUsageDay(new DateOnly(2026, 8, 19), 99, 1m)],
            DatabaseLastWriteTime = databaseWriteTime,
        };
}
