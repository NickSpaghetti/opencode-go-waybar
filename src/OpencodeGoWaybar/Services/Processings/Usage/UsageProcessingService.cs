using System.Text.Json;
using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Processings.Usage;
using OpencodeGoWaybar.Services.Foundations.Cache;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Services.Foundations.Usage;

namespace OpencodeGoWaybar.Services.Processings.Usage;

internal sealed class UsageProcessingService(
    ICacheService cacheService,
    IOpenCodeDatabaseService databaseService,
    IUsageService usageService,
    IOptions<OpenCodeGoOptions> options) : IUsageProcessingService
{
    private static readonly TimeSpan LockStaleAfter = TimeSpan.FromMinutes(2);

    public async ValueTask<UsageSnapshot> RetrieveUsageAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var state = await cacheService.RetrieveStateAsync(cancellationToken);
        var databaseWriteTime = await databaseService.RetrieveLastWriteTimeAsync(cancellationToken);

        if (!RequiresRefresh(state, databaseWriteTime, now))
        {
            return ToSnapshot(state!);
        }

        if (!await cacheService.TryAcquireLockAsync(LockStaleAfter, cancellationToken))
        {
            return ToSnapshot(state);
        }

        try
        {
            state = await cacheService.RetrieveStateAsync(cancellationToken) ?? new UsageCacheState();
            databaseWriteTime = await databaseService.RetrieveLastWriteTimeAsync(cancellationToken);

            var usage = state.Usage;
            var recentDays = state.RecentDays;
            var apiRetrievedAt = state.ApiRetrievedAt;

            if (RequiresApiRefresh(state, now))
            {
                usage = await usageService.RetrieveUsageAsync(cancellationToken);
                apiRetrievedAt = now;
            }

            if (RequiresDatabaseRefresh(state, databaseWriteTime))
            {
                var messages = await databaseService.RetrieveMessagesAsync(
                    now.AddDays(-7),
                    cancellationToken);
                recentDays = AggregateRecentUsage(messages);
            }

            var updatedState = new UsageCacheState
            {
                Usage = usage,
                RecentDays = recentDays,
                ApiRetrievedAt = apiRetrievedAt,
                DatabaseLastWriteTime = databaseWriteTime,
            };
            await cacheService.StoreStateAsync(updatedState, cancellationToken);

            return ToSnapshot(updatedState);
        }
        finally
        {
            await cacheService.ReleaseLockAsync(cancellationToken);
        }
    }

    private bool RequiresRefresh(
        UsageCacheState? state,
        DateTimeOffset? databaseWriteTime,
        DateTimeOffset now) =>
        state is null ||
        RequiresApiRefresh(state, now) ||
        RequiresDatabaseRefresh(state, databaseWriteTime);

    private bool RequiresApiRefresh(UsageCacheState state, DateTimeOffset now) =>
        state.Usage is null ||
        now - state.ApiRetrievedAt >= TimeSpan.FromSeconds(options.Value.RefreshIntervalSeconds);

    private static bool RequiresDatabaseRefresh(
        UsageCacheState state,
        DateTimeOffset? databaseWriteTime) =>
        state.DatabaseLastWriteTime != databaseWriteTime;

    private static UsageSnapshot ToSnapshot(UsageCacheState? state) =>
        state is null
            ? new UsageSnapshot(null, Array.Empty<RecentUsageDay>(), DateTimeOffset.MinValue, null)
            : new UsageSnapshot(state.Usage, state.RecentDays, state.ApiRetrievedAt, state.DatabaseLastWriteTime);

    private static IReadOnlyList<RecentUsageDay> AggregateRecentUsage(
        IReadOnlyList<OpenCodeMessage> messages)
    {
        var days = new Dictionary<DateOnly, (long Tokens, decimal Cost)>();

        foreach (var message in messages)
        {
            using var document = JsonDocument.Parse(message.Data);
            var root = document.RootElement;
            if (!root.TryGetProperty("providerID", out var provider) ||
                !string.Equals(provider.GetString(), "opencode-go", StringComparison.Ordinal))
            {
                continue;
            }

            var tokens = root.TryGetProperty("tokens", out var tokenObject) &&
                tokenObject.TryGetProperty("total", out var totalTokens)
                ? totalTokens.GetInt64()
                : 0;
            var cost = root.TryGetProperty("cost", out var messageCost)
                ? messageCost.GetDecimal()
                : 0m;
            var date = DateOnly.FromDateTime(message.CreatedAt.UtcDateTime.Date);
            var existing = days.GetValueOrDefault(date);
            days[date] = (existing.Tokens + tokens, existing.Cost + cost);
        }

        return days
            .OrderBy(day => day.Key)
            .Select(day => new RecentUsageDay(day.Key, day.Value.Tokens, decimal.Round(day.Value.Cost, 4)))
            .ToArray();
    }
}
