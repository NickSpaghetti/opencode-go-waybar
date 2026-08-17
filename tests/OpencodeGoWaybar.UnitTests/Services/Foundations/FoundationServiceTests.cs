using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Brokers.Apis.Usage;
using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase.Exceptions;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Services.Foundations.Usage.Exceptions;
using OpencodeGoWaybar.Services.Foundations.Usage;
using NSubstitute;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations;

public sealed class FoundationServiceTests
{
    [Fact]
    public async Task UsageFoundationPassesSuccessfulBrokerResultThrough()
    {
        var broker = new StubUsageApiBroker((_, _) => ValueTask.FromResult(new UsageApiBrokerResponse(
            System.Net.HttpStatusCode.OK,
            """
            {"usage":{"rolling":{"status":"ok","percent":10,"resetsAt":"2026-08-15T19:29:58Z"},"weekly":{"status":"ok","percent":20,"resetsAt":"2026-08-17T00:00:00Z"},"monthly":{"status":"ok","percent":30,"resetsAt":"2026-09-15T00:00:00Z"}}}
            """)));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var secrets = Options.Create(new OpenCodeGoSecrets { ApiKey = "test-key" });
        var foundation = new UsageService(broker, loggingBroker, secrets);

        var actual = await foundation.RetrieveUsageAsync(CancellationToken.None);

        Assert.Equal(20, actual.Usage.Weekly.Percent);
        Assert.Equal("test-key", broker.ReceivedApiKey);
    }

    [Fact]
    public async Task UsageFoundationRejectsMissingApiKeyBeforeCallingBroker()
    {
        var broker = new StubUsageApiBroker((_, _) => throw new InvalidOperationException());
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new UsageService(broker, loggingBroker, Options.Create(new OpenCodeGoSecrets()));

        await Assert.ThrowsAsync<UsageCredentialsMissingException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());
        Assert.Null(broker.ReceivedApiKey);
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<Exception>());
    }

    [Fact]
    public async Task UsageFoundationMapsUnauthorizedFailure()
    {
        var broker = new StubUsageApiBroker((_, _) =>
            ValueTask.FromResult(new UsageApiBrokerResponse(System.Net.HttpStatusCode.Unauthorized, "")));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new UsageService(broker, loggingBroker, Options.Create(new OpenCodeGoSecrets { ApiKey = "test-key" }));

        await Assert.ThrowsAsync<UsageAuthenticationException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<UsageAuthenticationException>());
    }

    [Fact]
    public async Task UsageFoundationMapsTransportFailure()
    {
        var broker = new StubUsageApiBroker((_, _) =>
            ValueTask.FromException<UsageApiBrokerResponse>(new HttpRequestException("offline")));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new UsageService(broker, loggingBroker, Options.Create(new OpenCodeGoSecrets { ApiKey = "test-key" }));

        await Assert.ThrowsAsync<UsageApiUnavailableException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<UsageApiUnavailableException>());
    }

    [Fact]
    public async Task UsageFoundationRejectsMalformedBrokerOutput()
    {
        var broker = new StubUsageApiBroker((_, _) => ValueTask.FromResult(new UsageApiBrokerResponse(
            System.Net.HttpStatusCode.OK,
            """
            {"usage":{"rolling":{"status":"ok","percent":null,"resetsAt":null},"weekly":{"status":"ok","percent":20,"resetsAt":"2026-08-17T00:00:00Z"},"monthly":{"status":"ok","percent":30,"resetsAt":"2026-09-15T00:00:00Z"}}}
            """)));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new UsageService(broker, loggingBroker, Options.Create(new OpenCodeGoSecrets { ApiKey = "test-key" }));

        await Assert.ThrowsAsync<UsageApiResponseException>(() =>
            foundation.RetrieveUsageAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<UsageApiResponseException>());
    }

    [Fact]
    public async Task DatabaseFoundationPassesSuccessfulBrokerResultThrough()
    {
        var expected = new[] { new OpenCodeMessage(DateTimeOffset.UnixEpoch, "{}") };
        var broker = new StubDatabaseBroker((_, _) => ValueTask.FromResult<IReadOnlyList<OpenCodeMessage>>(expected));
        var foundation = new OpenCodeDatabaseService(broker, Substitute.For<ILoggingBroker>(), Options.Create(new OpenCodeGoOptions()));

        var actual = await foundation.RetrieveMessagesAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task DatabaseFoundationMapsMissingSchema()
    {
        var broker = new StubDatabaseBroker((_, _) =>
            ValueTask.FromException<IReadOnlyList<OpenCodeMessage>>(new SqliteException("no such table: message", 1)));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new OpenCodeDatabaseService(broker, loggingBroker, Options.Create(new OpenCodeGoOptions()));

        await Assert.ThrowsAsync<OpenCodeDatabaseSchemaException>(() =>
            foundation.RetrieveMessagesAsync(DateTimeOffset.UnixEpoch, CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<OpenCodeDatabaseSchemaException>());
    }

    [Fact]
    public async Task DatabaseFoundationMapsOtherSqliteFailure()
    {
        var broker = new StubDatabaseBroker((_, _) =>
            ValueTask.FromException<IReadOnlyList<OpenCodeMessage>>(new SqliteException("database is locked", 5)));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new OpenCodeDatabaseService(broker, loggingBroker, Options.Create(new OpenCodeGoOptions()));

        await Assert.ThrowsAsync<OpenCodeDatabaseUnavailableException>(() =>
            foundation.RetrieveMessagesAsync(DateTimeOffset.UnixEpoch, CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<OpenCodeDatabaseUnavailableException>());
    }

    [Fact]
    public async Task DatabaseFoundationRejectsMalformedBrokerOutput()
    {
        var broker = new StubDatabaseBroker((_, _) =>
            ValueTask.FromResult<IReadOnlyList<OpenCodeMessage>>(
                new[] { new OpenCodeMessage(DateTimeOffset.UnixEpoch, "") }));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new OpenCodeDatabaseService(broker, loggingBroker, Options.Create(new OpenCodeGoOptions()));

        await Assert.ThrowsAsync<OpenCodeDatabaseResponseException>(() =>
            foundation.RetrieveMessagesAsync(DateTimeOffset.UnixEpoch, CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<OpenCodeDatabaseResponseException>());
    }

    private sealed class StubUsageApiBroker(Func<string, CancellationToken, ValueTask<UsageApiBrokerResponse>> call) : IUsageBroker
    {
        public string? ReceivedApiKey { get; private set; }

        public ValueTask<UsageApiBrokerResponse> GetUsageAsync(string apiKey, CancellationToken cancellationToken)
        {
            ReceivedApiKey = apiKey;
            return call(apiKey, cancellationToken);
        }
    }

    private sealed class StubDatabaseBroker(Func<DateTimeOffset, CancellationToken, ValueTask<IReadOnlyList<OpenCodeMessage>>> call) : IOpenCodeDatabaseBroker
    {
        public ValueTask<IReadOnlyList<OpenCodeMessage>> RetrieveMessagesAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
            call(cutoff, cancellationToken);
    }
}
