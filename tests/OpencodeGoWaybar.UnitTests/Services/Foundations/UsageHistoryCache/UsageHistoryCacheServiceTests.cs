using System.Text.Json;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Caches;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exceptions;
using OpencodeGoWaybar.Services.Foundations.UsageHistoryCache;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.UsageHistoryCache;

/// <summary>
/// The one decision this service owns: that a missing file means "nothing cached"
/// rather than a failure.
///
/// Same shape as the window service over its own file; see that suite on why the
/// lock tests are gone rather than moved.
///
/// </summary>
public sealed class UsageHistoryCacheServiceTests
{
    [Fact]
    public async Task ShouldRetrieveNoStateWhenTheCacheIsAbsentAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageHistoryCacheBroker>();
        cacheBroker.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageHistoryCacheState?>>(_ =>
                throw new FileNotFoundException("no cache yet"));

        // when
        UsageHistoryCacheState? state = await CreateService(cacheBroker, loggingBroker)
            .RetrieveStateAsync(CancellationToken.None);

        // then a cold cache is a state, not an error
        Assert.Null(state);
        await loggingBroker.DidNotReceive().LogErrorAsync(Arg.Any<Exception>());
    }

    [Fact]
    public async Task ShouldStoreTheStateThroughTheBrokerAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageHistoryCacheBroker>();
        var state = new UsageHistoryCacheState { DatabaseLastWriteTime = DateTimeOffset.UnixEpoch };

        // when
        await CreateService(cacheBroker, loggingBroker)
            .StoreStateAsync(state, CancellationToken.None);

        // then — one broker call, which is all Pure-Primitive allows
        await cacheBroker.Received(1).WriteStateAsync(state, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldThrowCacheValidationExceptionIfTheCacheDirectoryIsBlankAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageHistoryCacheBroker>();

        // when and then
        await Assert.ThrowsAsync<CacheValidationException>(() =>
            CreateService(cacheBroker, loggingBroker, cacheDirectory: " ")
                .RetrieveStateAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<CacheValidationException>());
    }

    [Fact]
    public async Task ShouldThrowCacheResponseExceptionIfTheCacheIsUnreadableAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageHistoryCacheBroker>();
        cacheBroker.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageHistoryCacheState?>>(_ => throw new JsonException("truncated"));

        // when and then
        await Assert.ThrowsAsync<CacheResponseException>(() =>
            CreateService(cacheBroker, loggingBroker)
                .RetrieveStateAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<CacheResponseException>());
    }

    [Fact]
    public async Task ShouldThrowCacheUnavailableExceptionIfTheCacheIsInaccessibleAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageHistoryCacheBroker>();
        cacheBroker.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageHistoryCacheState?>>(_ =>
                throw new UnauthorizedAccessException("denied"));

        // when and then
        await Assert.ThrowsAsync<CacheUnavailableException>(() =>
            CreateService(cacheBroker, loggingBroker)
                .RetrieveStateAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<CacheUnavailableException>());
    }

    [Fact]
    public async Task ShouldThrowCacheServiceExceptionOnStoreIfServiceErrorOccursAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageHistoryCacheBroker>();
        cacheBroker.WriteStateAsync(Arg.Any<UsageHistoryCacheState>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask>(_ => throw new FormatException("unexpected"));

        // when and then
        await Assert.ThrowsAsync<CacheServiceException>(() =>
            CreateService(cacheBroker, loggingBroker)
                .StoreStateAsync(new UsageHistoryCacheState(), CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<CacheServiceException>());
    }

    [Fact]
    public async Task ShouldLetCancellationPropagateUnwrappedAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageHistoryCacheBroker>();
        cacheBroker.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageHistoryCacheState?>>(_ => throw new OperationCanceledException());

        // when and then — the ten-second budget must not read as a cache failure
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(cacheBroker, loggingBroker)
                .RetrieveStateAsync(CancellationToken.None).AsTask());
        await loggingBroker.DidNotReceive().LogErrorAsync(Arg.Any<Exception>());
    }

    private static UsageHistoryCacheService CreateService(
        IUsageHistoryCacheBroker cacheBroker,
        ILoggingBroker loggingBroker,
        string cacheDirectory = "/tmp/opencode-go-waybar") =>
        new(cacheBroker, loggingBroker, new OpenCodeGoOptions { CacheDirectory = cacheDirectory });
}
