using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Usages;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Foundations.Usage;
using OpencodeGoWaybar.Models.Usages.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Usage;

public sealed partial class UsageServiceTests
{
    [Fact]
    public async Task ShouldThrowUsageAuthenticationExceptionOnRetrieveUsageIfUnauthorizedAndLogItAsync()
    {
        // given
        var broker = new StubUsageApiBroker((_, _) =>
            ValueTask.FromResult(new UsageApiBrokerResponse(System.Net.HttpStatusCode.Unauthorized, "")));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new UsageService(broker, loggingBroker, new OpenCodeGoSecrets { ApiKey = "test-key" });

        // when and then
        await Assert.ThrowsAsync<UsageAuthenticationException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<UsageAuthenticationException>());
    }

    [Fact]
    public async Task ShouldThrowUsageApiUnavailableExceptionOnRetrieveUsageIfTransportFailsAndLogItAsync()
    {
        // given
        var broker = new StubUsageApiBroker((_, _) =>
            ValueTask.FromException<UsageApiBrokerResponse>(new HttpRequestException("offline")));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new UsageService(broker, loggingBroker, new OpenCodeGoSecrets { ApiKey = "test-key" });

        // when and then
        await Assert.ThrowsAsync<UsageApiUnavailableException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<UsageApiUnavailableException>());
    }

    [Fact]
    public async Task ShouldCarryTheApiErrorOnRetrieveUsageIfUnauthorizedBodyIsNestedAsync()
    {
        // Verbatim from the live service.
        // given
        var broker = new StubUsageApiBroker((_, _) => ValueTask.FromResult(new UsageApiBrokerResponse(
            System.Net.HttpStatusCode.Unauthorized,
            """{"type":"error","error":{"type":"AuthError","message":"Unauthorized"}}""")));
        var foundation = new UsageService(broker, Substitute.For<ILoggingBroker>(),
            new OpenCodeGoSecrets { ApiKey = "test-key" });

        var exception = await Assert.ThrowsAsync<UsageAuthenticationException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());

        // when
        Assert.Equal("AuthError", exception.ApiError!.Type);
        // when
        Assert.Equal("Unauthorized", exception.ApiError!.Message);
    }

    [Fact]
    public async Task ShouldCarryTheApiErrorOnRetrieveUsageIfBodyShapeIsFlatAsync()
    {
        // The shape recorded in contracts/fixtures/usage-rate-limited.json.
        // given
        var broker = new StubUsageApiBroker((_, _) => ValueTask.FromResult(new UsageApiBrokerResponse(
            System.Net.HttpStatusCode.TooManyRequests,
            """{"error":"rate_limited","message":"Too many requests"}""")));
        var foundation = new UsageService(broker, Substitute.For<ILoggingBroker>(),
            new OpenCodeGoSecrets { ApiKey = "test-key" });

        var exception = await Assert.ThrowsAsync<UsageRateLimitedException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());

        // when
        Assert.Equal("rate_limited", exception.ApiError!.Type);
        // when
        Assert.Equal("Too many requests", exception.ApiError!.Message);
    }

    [Fact]
    public async Task ShouldTolerateAnErrorBodyOnRetrieveUsageIfItCannotBeReadAsync()
    {
        // given
        var broker = new StubUsageApiBroker((_, _) => ValueTask.FromResult(new UsageApiBrokerResponse(
            System.Net.HttpStatusCode.Unauthorized, "<html>gateway said no</html>")));
        var foundation = new UsageService(broker, Substitute.For<ILoggingBroker>(),
            new OpenCodeGoSecrets { ApiKey = "test-key" });

        var exception = await Assert.ThrowsAsync<UsageAuthenticationException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());

        // when
        Assert.Null(exception.ApiError);
    }
}
