namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal interface IProcessService
{
    /// <summary>Reports whether an OpenCode process is currently running.</summary>
    ValueTask<bool> IsOpenCodeRunningAsync(CancellationToken cancellationToken);
}
