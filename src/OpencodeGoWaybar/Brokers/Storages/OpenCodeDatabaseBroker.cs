using Dapper;
using Microsoft.Data.Sqlite;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Brokers.Storages;

// Dapper.AOT generates the materializers at build time; plain Dapper emits IL
// at runtime, which NativeAOT forbids.
[DapperAot]
internal sealed partial class OpenCodeDatabaseBroker(OpenCodeGoOptions options) : IOpenCodeDatabaseBroker
{
    private string DatabasePath => options.DatabasePath;

    /// <summary>Opencode owns this database; this module only ever reads it.</summary>
    private string ConnectionString =>
        new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadOnly,
        }.ConnectionString;

    public ValueTask<DateTimeOffset> GetLastWriteTimeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(
            new DateTimeOffset(File.GetLastWriteTimeUtc(DatabasePath), TimeSpan.Zero));
    }
}
