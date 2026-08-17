namespace OpencodeGoWaybar.Brokers.Storages.OpenCode;

internal interface IOpenCodeDatabaseBroker
{
    ValueTask<DateTimeOffset?> RetrieveLastWriteTimeAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<OpenCodeMessage>> RetrieveMessagesAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);
}

internal sealed record OpenCodeMessage(DateTimeOffset CreatedAt, string Data);
