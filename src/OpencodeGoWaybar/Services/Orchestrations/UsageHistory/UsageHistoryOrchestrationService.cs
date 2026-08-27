using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.OpenCodeMessages.Exceptions;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Services.Foundations.UsageHistoryCache;

namespace OpencodeGoWaybar.Services.Orchestrations.UsageHistory;

/// <summary>
/// Recent daily usage, refreshed only when the database opencode owns has actually
/// been written since the cached copy was taken.
///
/// Two foundation dependencies of one variation. It is the only writer of the
/// history cache file, so there is no lock and no other half to preserve.
/// </summary>
internal sealed class UsageHistoryOrchestrationService(
    IUsageHistoryCacheService cacheService,
    IOpenCodeDatabaseService databaseService) : IUsageHistoryOrchestrationService
{
    /// <summary>The provider whose spend this module reports.</summary>
    private const string OpenCodeGoProviderId = "opencode-go";

    private const int RecentDayCount = 7;

    public async ValueTask<UsageHistorySnapshot> RetrieveHistoryAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        UsageHistoryCacheState? state = await cacheService.RetrieveStateAsync(cancellationToken);

        DateTimeOffset? databaseWriteTime =
            await databaseService.RetrieveLastWriteTimeAsync(cancellationToken);

        if (state is not null && state.DatabaseLastWriteTime == databaseWriteTime)
        {
            return ToSnapshot(state);
        }

        IReadOnlyList<RecentUsageDay> recentDays = await RetrieveRecentDaysAsync(
            state?.RecentDays ?? [],
            now,
            cancellationToken);

        var refreshedState = new UsageHistoryCacheState
        {
            RecentDays = recentDays,
            DatabaseLastWriteTime = databaseWriteTime,
        };

        await cacheService.StoreStateAsync(refreshedState, cancellationToken);

        return ToSnapshot(refreshedState);
    }

    /// <summary>
    /// Recent-day totals come from a database opencode owns and this module only
    /// reads. When it cannot be read the allowances from the API are still worth
    /// showing, so the failure costs the token count rather than the whole payload.
    /// </summary>
    private async ValueTask<IReadOnlyList<RecentUsageDay>> RetrieveRecentDaysAsync(
        IReadOnlyList<RecentUsageDay> cachedRecentDays,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            return await databaseService.RetrieveRecentUsageDaysAsync(
                now.AddDays(-RecentDayCount),
                OpenCodeGoProviderId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is OpenCodeDatabaseUnavailableException
            or OpenCodeDatabaseSchemaException)
        {
            return cachedRecentDays;
        }
    }

    /// <summary>
    /// The total happens here because an exposer may not iterate and the bar's
    /// tooltip needs it. The days themselves are handed on as recorded.
    /// </summary>
    private static UsageHistorySnapshot ToSnapshot(UsageHistoryCacheState? state) =>
        state is null
            ? new UsageHistorySnapshot([], TotalTokens: 0, DatabaseLastWriteTime: null)
            : new UsageHistorySnapshot(
                state.RecentDays,
                state.RecentDays.Sum(day => day.Tokens),
                state.DatabaseLastWriteTime);
}
