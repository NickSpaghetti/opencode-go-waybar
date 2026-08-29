using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Foundations.UsageWindowCache;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.Services.Foundations.Usage;

namespace OpencodeGoWaybar.Services.Orchestrations.UsageWindows;

/// <summary>
/// The allowance windows: whether OpenCode is even running, a throttled refresh of
/// the API snapshot, and the health verdict for each window.
///
/// Owns the Usage and ApiRetrievedAt slice of the cached state and leaves the
/// history slice exactly as it found it.
/// </summary>
internal sealed class UsageWindowsOrchestrationService(
    IProcessService processService,
    IUsageWindowCacheService cacheService,
    IUsageService usageService,
    OpenCodeGoOptions options) : IUsageWindowsOrchestrationService
{
    public async ValueTask<UsageWindowSnapshot> RetrieveWindowsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!await processService.IsOpenCodeRunningAsync(cancellationToken))
        {
            return CreateInactiveSnapshot();
        }

        UsageWindowCacheState? state = await cacheService.RetrieveStateAsync(cancellationToken);

        if (!RequiresApiRefresh(state, now))
        {
            return ToSnapshot(state);
        }

        UsageResponse usage = await usageService.RetrieveUsageAsync(cancellationToken);

        // This service is the only writer of this file, so there is no other half
        // to preserve and nothing to lock against. The broker's write is atomic.
        var refreshedState = new UsageWindowCacheState
        {
            Usage = usage,
            ApiRetrievedAt = now,
        };

        await cacheService.StoreStateAsync(refreshedState, cancellationToken);

        return ToSnapshot(refreshedState);
    }

    private bool RequiresApiRefresh(UsageWindowCacheState? state, DateTimeOffset now) =>
        state?.Usage is null
        || now - state.ApiRetrievedAt >= TimeSpan.FromSeconds(options.RefreshIntervalSeconds);

    private static UsageWindowSnapshot CreateInactiveSnapshot() =>
        new(ProcessIsActive: false,
            UnknownWindow,
            UnknownWindow,
            UnknownWindow,
            ApiRetrievedAt: null,
            IsRateLimited: false,
            Usage: null);

    private UsageWindowSnapshot ToSnapshot(UsageWindowCacheState? state)
    {
        Usage? usage = state?.Usage?.Usage;

        if (usage is null)
        {
            return new UsageWindowSnapshot(
                ProcessIsActive: true,
                UnknownWindow,
                UnknownWindow,
                UnknownWindow,
                state?.ApiRetrievedAt,
                IsRateLimited: false,
                state?.Usage);
        }

        return new UsageWindowSnapshot(
            ProcessIsActive: true,
            CreateWindow(usage.Rolling),
            CreateWindow(usage.Weekly),
            CreateWindow(usage.Monthly),
            state?.ApiRetrievedAt,
            IsRateLimited(usage.Rolling.Status)
                || IsRateLimited(usage.Weekly.Status)
                || IsRateLimited(usage.Monthly.Status),
            state?.Usage);
    }

    private static UsageWindowState UnknownWindow { get; } =
        new(Percent: null, UsageWindowStatus.Unknown, ResetsAt: null);

    private UsageWindowState CreateWindow(UsageWindow window) =>
        new(window.Percent, Classify(window), window.ResetsAt);

    /// <summary>
    /// Order is the whole rule. A refusing API outranks any percentage, and the
    /// API withdrawing "ok" outranks a percentage that still looks comfortable —
    /// a five-hour window at 61% that is queueing requests is not healthy.
    /// </summary>
    private UsageWindowStatus Classify(UsageWindow window)
    {
        if (IsRateLimited(window.Status))
        {
            return UsageWindowStatus.RateLimited;
        }

        if (!IsHealthy(window.Status))
        {
            return UsageWindowStatus.Throttled;
        }

        if (window.Percent is not { } percent)
        {
            return UsageWindowStatus.Unknown;
        }

        if (percent >= options.DangerPercent)
        {
            return UsageWindowStatus.Spent;
        }

        return percent >= options.CautionPercent
            ? UsageWindowStatus.Caution
            : UsageWindowStatus.Ok;
    }

    /// <summary>
    /// Matched loosely on purpose: the live API answers "rate-limited" while the
    /// published contract names "HTTP 429", and neither is guaranteed stable.
    /// </summary>
    private static bool IsRateLimited(string? status) =>
        status is not null
        && ((status.Contains("rate", StringComparison.OrdinalIgnoreCase)
                && status.Contains("limit", StringComparison.OrdinalIgnoreCase))
            || status.Contains("429", StringComparison.Ordinal));

    private static bool IsHealthy(string status) =>
        status.Equals("ok", StringComparison.OrdinalIgnoreCase);
}
