using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;

internal sealed partial class OpenCodeDatabaseService : IOpenCodeDatabaseService
{
    private readonly IOpenCodeDatabaseBroker _databaseBroker;
    private readonly ILoggingBroker _loggingBroker;
    private readonly IOptions<OpenCodeGoOptions> _options;

    public OpenCodeDatabaseService(
        IOpenCodeDatabaseBroker databaseBroker,
        ILoggingBroker loggingBroker,
        IOptions<OpenCodeGoOptions> options)
    {
        _databaseBroker = databaseBroker;
        _loggingBroker = loggingBroker;
        _options = options;
    }

    public ValueTask<IReadOnlyList<OpenCodeMessage>> RetrieveMessagesAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken) =>
        TryCatchAsync(cutoff, cancellationToken);

    public ValueTask<DateTimeOffset?> RetrieveLastWriteTimeAsync(CancellationToken cancellationToken) =>
        TryCatchLastWriteTimeAsync(cancellationToken);
}
