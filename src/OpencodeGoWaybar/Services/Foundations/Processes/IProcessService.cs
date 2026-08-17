namespace OpencodeGoWaybar.Services.Foundations.Processes;

internal interface IProcessService
{
    ValueTask<bool> IsInteractiveOpenCodeRunningAsync(CancellationToken cancellationToken);
}
