using OpencodeGoWaybar.Brokers.Storages.OpenCode;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;

internal interface IOpenCodeDatabaseService
{
    ValueTask<DateTimeOffset?> RetrieveLastWriteTimeAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<OpenCodeMessage>> RetrieveMessagesAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);
}
