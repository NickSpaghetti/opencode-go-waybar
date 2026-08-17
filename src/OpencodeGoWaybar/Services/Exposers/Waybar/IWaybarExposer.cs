using OpencodeGoWaybar.Models.Processings.Usage;

namespace OpencodeGoWaybar.Services.Exposers.Waybar;

internal interface IWaybarExposer
{
    ValueTask<string> ExposeAsync(
        bool processIsActive,
        UsageSnapshot? snapshot,
        Exception? exception,
        CancellationToken cancellationToken);
}
