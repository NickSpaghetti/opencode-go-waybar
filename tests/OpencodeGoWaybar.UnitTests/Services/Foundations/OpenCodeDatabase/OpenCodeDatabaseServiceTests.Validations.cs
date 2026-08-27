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
    public async Task ShouldThrowOpenCodeDatabaseResponseExceptionOnRetrieveRecentUsageDaysIfDataIsMalformedAndLogItAsync()
    {
        // given
        var broker = new StubDatabaseBroker((_, _, _) =>
            ValueTask.FromResult<IReadOnlyList<OpenCodeUsageDayRow>>(
                new[] { new OpenCodeUsageDayRow("", Tokens: 0, Cost: 0) }));
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new OpenCodeDatabaseService(broker, loggingBroker, new OpenCodeGoOptions());

        // when and then
        await Assert.ThrowsAsync<OpenCodeDatabaseResponseException>(() =>
            foundation.RetrieveRecentUsageDaysAsync(DateTimeOffset.UnixEpoch, "opencode-go", CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<OpenCodeDatabaseResponseException>());
    }
}
