using OpencodeGoWaybar.Models.Processes;

namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal interface IProcessService
{
    /// <summary>Reports whether an OpenCode process is currently running.</summary>
    ValueTask<bool> IsOpenCodeRunningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reports every running OpenCode process together with the chain of
    /// processes that spawned it, so a caller holding a window-to-process map can
    /// work out which window each session belongs to.
    /// </summary>
    ValueTask<IReadOnlyList<OpenCodeProcessLineage>> RetrieveOpenCodeLineagesAsync(
        CancellationToken cancellationToken);
}
