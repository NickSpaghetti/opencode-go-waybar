using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Hyprland;
using OpencodeGoWaybar.Models.Hyprland.Exceptions;
using OpencodeGoWaybar.Models.Processes;
using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Services.Foundations.UsageWindowCache;
using OpencodeGoWaybar.Services.Foundations.Hyprland;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.Services.Foundations.Usage;

namespace OpencodeGoWaybar.Services.Orchestrations.UsageWindows;

/// <summary>
/// The allowance windows: whether OpenCode is even running, whether it is running
/// somewhere you can see, a throttled refresh of the API snapshot, and the health
/// verdict for each window.
///
/// Owns the Usage and ApiRetrievedAt slice of the cached state and leaves the
/// history slice exactly as it found it.
/// </summary>
internal sealed class UsageWindowsOrchestrationService(
    IProcessService processService,
    IHyprlandService hyprlandService,
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

        if (!await IsOpenCodeOnActiveWorkspaceAsync(cancellationToken))
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

    /// <summary>
    /// Whether an OpenCode session is displayed on the workspace you are looking
    /// at. The rule is deliberately one-sided: it hides only a session it can
    /// positively place somewhere else, and answers true for every case it cannot
    /// decide. A filter meant to reduce noise must never be the reason a real
    /// allowance warning goes unseen.
    /// </summary>
    private async ValueTask<bool> IsOpenCodeOnActiveWorkspaceAsync(CancellationToken cancellationToken)
    {
        // The override forces the process answer outright, for container and
        // acceptance runs where there is no session and no compositor to place it on.
        if (!options.ActiveWorkspaceOnly || options.ProcessPresentOverride is not null)
        {
            return true;
        }

        int? activeWorkspaceId;
        IReadOnlyList<HyprlandWindow> windows;

        try
        {
            activeWorkspaceId = await hyprlandService.RetrieveActiveWorkspaceIdAsync(cancellationToken);

            // Not Hyprland, so there is no workspace to be off.
            if (activeWorkspaceId is null)
            {
                return true;
            }

            windows = await hyprlandService.RetrieveWindowsAsync(cancellationToken);
        }
        catch (Exception exception) when (exception
            is HyprlandUnavailableException
            or HyprlandResponseException
            or HyprlandServiceException)
        {
            // The service has already logged it. A compositor that will not answer
            // is a reason to stop filtering, not a reason to report usage as broken.
            return true;
        }

        if (windows.Count == 0)
        {
            return true;
        }

        IReadOnlyList<OpenCodeProcessLineage> lineages =
            await processService.RetrieveOpenCodeLineagesAsync(cancellationToken);

        return IsAnyLineageOnWorkspace(lineages, windows, activeWorkspaceId.Value);
    }

    private static bool IsAnyLineageOnWorkspace(
        IReadOnlyList<OpenCodeProcessLineage> lineages,
        IReadOnlyList<HyprlandWindow> windows,
        int activeWorkspaceId)
    {
        // One process can own several windows — a terminal that keeps a single
        // instance across every window it draws — so a process maps to a set of
        // workspaces rather than to one.
        Dictionary<int, HashSet<int>> workspacesByProcessId = windows
            .GroupBy(window => window.ProcessId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(window => window.WorkspaceId).ToHashSet());

        var placed = false;

        foreach (OpenCodeProcessLineage lineage in lineages)
        {
            foreach (var processId in lineage.LineageProcessIds)
            {
                if (!workspacesByProcessId.TryGetValue(processId, out var workspaceIds))
                {
                    continue;
                }

                // The nearest ancestor holding a window is the one the session is
                // displayed in; a grandparent that also happens to own a window is
                // not where you are watching this session run.
                placed = true;

                if (workspaceIds.Contains(activeWorkspaceId))
                {
                    return true;
                }

                break;
            }
        }

        // No session could be tied to any window — a headless or detached OpenCode.
        // There is no workspace to be off, so it stays visible.
        return !placed;
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
