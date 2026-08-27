using Microsoft.Data.Sqlite;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Storages;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.OpenCodeMessages;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Models.OpenCodeMessages.Exceptions;
using Xunit;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.OpenCodeDatabase;

public sealed partial class OpenCodeDatabaseServiceTests
{
    [Fact]
    public async Task ShouldRetrieveRecentUsageDaysAsync()
    {
        // given
        var rows = new[] { new OpenCodeUsageDayRow("2026-08-18", Tokens: 30, Cost: 0.3) };
        var broker = new StubDatabaseBroker((_, _, _) => ValueTask.FromResult<IReadOnlyList<OpenCodeUsageDayRow>>(rows));
        var foundation = new OpenCodeDatabaseService(broker, Substitute.For<ILoggingBroker>(), new OpenCodeGoOptions());

        // when
        var actual = await foundation.RetrieveRecentUsageDaysAsync(DateTimeOffset.UnixEpoch, "opencode-go", CancellationToken.None);

        // then
        RecentUsageDay day = Assert.Single(actual);
        Assert.Equal(new DateOnly(2026, 8, 18), day.Date);
        Assert.Equal(30, day.Tokens);
        Assert.Equal(0.3m, day.Cost);
    }

    [Fact]
    public async Task ShouldReportNoWriteTimeWhenTheDatabaseFileIsAbsentAsync()
    {
        // given
        // File.GetLastWriteTimeUtc answers with a 1601 sentinel rather than throwing.
        var missingFileWriteTime = new DateTimeOffset(DateTime.FromFileTimeUtc(0), TimeSpan.Zero);
        var broker = Substitute.For<IOpenCodeDatabaseBroker>();
        broker.GetLastWriteTimeAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(missingFileWriteTime));

        var foundation = new OpenCodeDatabaseService(
            broker,
            Substitute.For<ILoggingBroker>(),
            new OpenCodeGoOptions());

        // when
        DateTimeOffset? writeTime = await foundation.RetrieveLastWriteTimeAsync(CancellationToken.None);

        // then
        Assert.Null(writeTime);
    }
}
