using NSubstitute;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.Services.Foundations.Usage;
using OpencodeGoWaybar.Services.Foundations.UsageWindowCache;
using OpencodeGoWaybar.Services.Orchestrations.UsageWindows;
using Xunit;
using UsageModel = OpencodeGoWaybar.Models.Usages.Usage;

namespace OpencodeGoWaybar.UnitTests.Services.Orchestrations.UsageWindows;

/// <summary>
/// The window half of the usage flow: the process gate that used to sit in the
/// aggregation, the throttled API refresh, and the health classification the
/// exposer used to make.
///
/// The lock tests and the slice-preservation guard are gone. This service is now
/// the only writer of its own cache file, so "do not clobber the other half" is
/// structural rather than something a test has to hold in place.
/// </summary>
public sealed class UsageWindowsOrchestrationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 15, 17, 0, TimeSpan.Zero);

    [Fact]
    public async Task ShouldNotFetchUsageWhenOpenCodeIsNotRunningAsync()
    {
        // given
        var cacheService = Substitute.For<IUsageWindowCacheService>();
        var usageService = Substitute.For<IUsageService>();

        // when
        UsageWindowSnapshot snapshot = await CreateService(
            CreateProcessService(isRunning: false), cacheService, usageService)
            .RetrieveWindowsAsync(Now, CancellationToken.None);

        // then nothing downstream is touched — there would be nothing to show
        Assert.False(snapshot.ProcessIsActive);
        Assert.Equal(UsageWindowStatus.Unknown, snapshot.Weekly.Status);
        await cacheService.DidNotReceive().RetrieveStateAsync(Arg.Any<CancellationToken>());
        await usageService.DidNotReceive().RetrieveUsageAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnTheCachedSnapshotWithoutCallingTheApiAsync()
    {
        // given a snapshot taken a minute ago
        var usageService = Substitute.For<IUsageService>();
        IUsageWindowCacheService cacheService = CreateCacheService(
            CreateState(apiRetrievedAt: Now.AddMinutes(-1)));

        // when
        UsageWindowSnapshot snapshot = await CreateService(
            CreateProcessService(), cacheService, usageService)
            .RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.Equal(61, snapshot.Rolling.Percent);
        await usageService.DidNotReceive().RetrieveUsageAsync(Arg.Any<CancellationToken>());
        await cacheService.DidNotReceive().StoreStateAsync(
            Arg.Any<UsageWindowCacheState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldRefreshTheApiWhenTheCachedSnapshotIsStaleAsync()
    {
        // given a snapshot an hour old
        IUsageWindowCacheService cacheService = CreateCacheService(
            CreateState(apiRetrievedAt: Now.AddHours(-1)));

        var usageService = Substitute.For<IUsageService>();
        usageService.RetrieveUsageAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(CreateUsage(rollingPercent: 12)));

        // when
        UsageWindowSnapshot snapshot = await CreateService(
            CreateProcessService(), cacheService, usageService)
            .RetrieveWindowsAsync(Now, CancellationToken.None);

        // then the fresh answer is stored and returned, stamped with the read time
        Assert.Equal(12, snapshot.Rolling.Percent);
        await usageService.Received(1).RetrieveUsageAsync(Arg.Any<CancellationToken>());
        await cacheService.Received(1).StoreStateAsync(
            Arg.Is<UsageWindowCacheState>(written => written.ApiRetrievedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldRefreshWhenNothingHasBeenCachedYetAsync()
    {
        // given a cold cache
        IUsageWindowCacheService cacheService = CreateCacheService(state: null);

        var usageService = Substitute.For<IUsageService>();
        usageService.RetrieveUsageAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(CreateUsage()));

        // when
        UsageWindowSnapshot snapshot = await CreateService(
            CreateProcessService(), cacheService, usageService)
            .RetrieveWindowsAsync(Now, CancellationToken.None);

        // then — this is also the migration path off the old single-file cache
        Assert.Equal(61, snapshot.Rolling.Percent);
        await usageService.Received(1).RetrieveUsageAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("ok", null, UsageWindowStatus.Unknown)]
    [InlineData("ok", 74, UsageWindowStatus.Ok)]
    [InlineData("ok", 75, UsageWindowStatus.Caution)]
    [InlineData("ok", 89, UsageWindowStatus.Caution)]
    [InlineData("ok", 90, UsageWindowStatus.Spent)]
    [InlineData("throttled", 10, UsageWindowStatus.Throttled)]
    [InlineData("throttled", 95, UsageWindowStatus.Throttled)]
    [InlineData("no_api_key", null, UsageWindowStatus.Throttled)]
    [InlineData("rate-limited", 10, UsageWindowStatus.RateLimited)]
    [InlineData("HTTP 429", 95, UsageWindowStatus.RateLimited)]
    public async Task ShouldClassifyAWindowFromItsPercentAndApiStatusAsync(
        string status,
        int? percent,
        UsageWindowStatus expectedStatus)
    {
        // given
        IUsageWindowCacheService cacheService = CreateCacheService(CreateState(
            apiRetrievedAt: Now.AddMinutes(-1),
            usage: CreateUsage(rollingStatus: status, rollingPercent: percent)));

        // when
        UsageWindowSnapshot snapshot = await CreateService(
            CreateProcessService(), cacheService, Substitute.For<IUsageService>())
            .RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.Equal(expectedStatus, snapshot.Rolling.Status);
    }

    [Theory]
    [InlineData(49, UsageWindowStatus.Ok)]
    [InlineData(50, UsageWindowStatus.Caution)]
    [InlineData(60, UsageWindowStatus.Spent)]
    public async Task ShouldHonourTheConfiguredThresholdsAsync(
        int percent,
        UsageWindowStatus expectedStatus)
    {
        // given
        IUsageWindowCacheService cacheService = CreateCacheService(CreateState(
            apiRetrievedAt: Now.AddMinutes(-1),
            usage: CreateUsage(rollingPercent: percent)));

        // when
        UsageWindowSnapshot snapshot = await CreateService(
            CreateProcessService(), cacheService, Substitute.For<IUsageService>(),
            cautionPercent: 50, dangerPercent: 60)
            .RetrieveWindowsAsync(Now, CancellationToken.None);

        // then
        Assert.Equal(expectedStatus, snapshot.Rolling.Status);
    }

    [Fact]
    public async Task ShouldFlagRateLimitedWhenAnyWindowIsRefusedAsync()
    {
        // given a refusal on the weekly window only
        IUsageWindowCacheService cacheService = CreateCacheService(CreateState(
            apiRetrievedAt: Now.AddMinutes(-1),
            usage: new UsageResponse(new UsageModel(
                new UsageWindow("ok", 10, Now),
                new UsageWindow("rate-limited", 98, Now),
                new UsageWindow("ok", 12, Now)))));

        // when
        UsageWindowSnapshot snapshot = await CreateService(
            CreateProcessService(), cacheService, Substitute.For<IUsageService>())
            .RetrieveWindowsAsync(Now, CancellationToken.None);

        // then the flag is computed once, here, so no exposer looks for it
        Assert.True(snapshot.IsRateLimited);
    }

    private static UsageWindowsOrchestrationService CreateService(
        IProcessService processService,
        IUsageWindowCacheService cacheService,
        IUsageService usageService,
        int cautionPercent = 75,
        int dangerPercent = 90) =>
        new(processService,
            cacheService,
            usageService,
            new OpenCodeGoOptions
            {
                CautionPercent = cautionPercent,
                DangerPercent = dangerPercent,
            });

    private static IProcessService CreateProcessService(bool isRunning = true)
    {
        var processService = Substitute.For<IProcessService>();

        processService.IsOpenCodeRunningAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(isRunning));

        return processService;
    }

    private static IUsageWindowCacheService CreateCacheService(UsageWindowCacheState? state)
    {
        var cacheService = Substitute.For<IUsageWindowCacheService>();

        cacheService.RetrieveStateAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(state));

        return cacheService;
    }

    private static UsageWindowCacheState CreateState(
        DateTimeOffset apiRetrievedAt,
        UsageResponse? usage = null) =>
        new()
        {
            Usage = usage ?? CreateUsage(),
            ApiRetrievedAt = apiRetrievedAt,
        };

    private static UsageResponse CreateUsage(
        string rollingStatus = "ok",
        int? rollingPercent = 61) =>
        new(new UsageModel(
            new UsageWindow(rollingStatus, rollingPercent, Now),
            new UsageWindow("ok", 24, Now),
            new UsageWindow("ok", 12, Now)));
}
