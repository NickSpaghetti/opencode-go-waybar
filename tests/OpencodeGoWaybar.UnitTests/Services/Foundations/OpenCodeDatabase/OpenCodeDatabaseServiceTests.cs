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
    private sealed class StubDatabaseBroker(
        Func<DateTimeOffset, string, CancellationToken, ValueTask<IReadOnlyList<OpenCodeUsageDayRow>>> call)
        : IOpenCodeDatabaseBroker
    {
        public ValueTask<DateTimeOffset> GetLastWriteTimeAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult(DateTimeOffset.UnixEpoch);

        public ValueTask<IReadOnlyList<OpenCodeUsageDayRow>> SelectUsageDaysByCutoffAsync(
            DateTimeOffset cutoff,
            string providerId,
            CancellationToken cancellationToken) =>
            call(cutoff, providerId, cancellationToken);
    }
}
