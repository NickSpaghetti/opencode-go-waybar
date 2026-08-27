using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;

internal interface IOpenCodeDatabaseService
{
    ValueTask<DateTimeOffset?> RetrieveLastWriteTimeAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<RecentUsageDay>> RetrieveRecentUsageDaysAsync(
        DateTimeOffset cutoff,
        string providerId,
        CancellationToken cancellationToken);
}
