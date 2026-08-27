using System.Text.Json;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Caches;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exceptions;
using OpencodeGoWaybar.Services.Foundations.UsageWindowCache;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.UsageWindowCache;

/// <summary>
/// The one decision this service owns: that a missing file means "nothing cached"
/// rather than a failure.
///
/// The lock tests that used to live here are gone rather than moved. Splitting the
/// cache file gave each half a single writer, so there is no lost update to guard
/// and no staleness window to reclaim — the routines they covered no longer exist.
/// </summary>
public sealed class UsageWindowCacheServiceTests
{
    [Fact]
    public async Task ShouldRetrieveNoStateWhenTheCacheIsAbsentAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageWindowCacheBroker>();
        cacheBroker.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageWindowCacheState?>>(_ =>
                throw new FileNotFoundException("no cache yet"));

        // when
        UsageWindowCacheState? state = await CreateService(cacheBroker, loggingBroker)
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
        var cacheBroker = Substitute.For<IUsageWindowCacheBroker>();
        var state = new UsageWindowCacheState { ApiRetrievedAt = DateTimeOffset.UnixEpoch };

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
        var cacheBroker = Substitute.For<IUsageWindowCacheBroker>();

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
        var cacheBroker = Substitute.For<IUsageWindowCacheBroker>();
        cacheBroker.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageWindowCacheState?>>(_ => throw new JsonException("truncated"));

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
        var cacheBroker = Substitute.For<IUsageWindowCacheBroker>();
        cacheBroker.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageWindowCacheState?>>(_ =>
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
        var cacheBroker = Substitute.For<IUsageWindowCacheBroker>();
        cacheBroker.WriteStateAsync(Arg.Any<UsageWindowCacheState>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask>(_ => throw new FormatException("unexpected"));

        // when and then
        await Assert.ThrowsAsync<CacheServiceException>(() =>
            CreateService(cacheBroker, loggingBroker)
                .StoreStateAsync(new UsageWindowCacheState(), CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<CacheServiceException>());
    }

    [Fact]
    public async Task ShouldLetCancellationPropagateUnwrappedAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var cacheBroker = Substitute.For<IUsageWindowCacheBroker>();
        cacheBroker.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<UsageWindowCacheState?>>(_ => throw new OperationCanceledException());

        // when and then — the ten-second budget must not read as a cache failure
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(cacheBroker, loggingBroker)
                .RetrieveStateAsync(CancellationToken.None).AsTask());
        await loggingBroker.DidNotReceive().LogErrorAsync(Arg.Any<Exception>());
    }

    private static UsageWindowCacheService CreateService(
        IUsageWindowCacheBroker cacheBroker,
        ILoggingBroker loggingBroker,
        string cacheDirectory = "/tmp/opencode-go-waybar") =>
        new(cacheBroker, loggingBroker, new OpenCodeGoOptions { CacheDirectory = cacheDirectory });
}
