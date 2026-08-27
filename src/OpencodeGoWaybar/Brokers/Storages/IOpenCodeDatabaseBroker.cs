namespace OpencodeGoWaybar.Brokers.Storages;

internal partial interface IOpenCodeDatabaseBroker
{
    /// <summary>
    /// When the database file was last written. Throws when it is absent.
    /// A property of the file rather than of any entity, so it lives here.
    /// </summary>
    ValueTask<DateTimeOffset> GetLastWriteTimeAsync(CancellationToken cancellationToken);
}
