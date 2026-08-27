using OpencodeGoWaybar.Models.Usages;
using Microsoft.Data.Sqlite;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Storages;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.OpenCodeMessages;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Models.OpenCodeMessages.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.OpenCodeDatabase;

public sealed partial class OpenCodeDatabaseServiceTests
{
    [Fact]
    public async Task ShouldThrowOpenCodeDatabaseSchemaExceptionOnRetrieveRecentUsageDaysIfTableIsMissingAndLogItAsync()
    {
        // given
        var broker = new StubDatabaseBroker((_, _, _) =>
            ValueTask.FromException<IReadOnlyList<OpenCodeUsageDayRow>>(new SqliteException("no such table: message", 1)));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new OpenCodeDatabaseService(broker, loggingBroker, new OpenCodeGoOptions());

        // when and then
        await Assert.ThrowsAsync<OpenCodeDatabaseSchemaException>(() =>
            foundation.RetrieveRecentUsageDaysAsync(DateTimeOffset.UnixEpoch, "opencode-go", CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<OpenCodeDatabaseSchemaException>());
    }

    [Fact]
    public async Task ShouldThrowOpenCodeDatabaseUnavailableExceptionOnRetrieveRecentUsageDaysIfSqliteFailsAndLogItAsync()
    {
        // given
        var broker = new StubDatabaseBroker((_, _, _) =>
            ValueTask.FromException<IReadOnlyList<OpenCodeUsageDayRow>>(new SqliteException("database is locked", 5)));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new OpenCodeDatabaseService(broker, loggingBroker, new OpenCodeGoOptions());

        // when and then
        await Assert.ThrowsAsync<OpenCodeDatabaseUnavailableException>(() =>
            foundation.RetrieveRecentUsageDaysAsync(DateTimeOffset.UnixEpoch, "opencode-go", CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<OpenCodeDatabaseUnavailableException>());
    }
}
