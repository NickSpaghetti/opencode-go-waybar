using OpencodeGoWaybar.Models.Usages.Exposures;

namespace OpencodeGoWaybar.Exposers.Usages;

/// <summary>
/// The published contract for a usage detail view. Public because the Avalonia
/// head lives in another assembly and must reach the flow through an exposer
/// rather than through a broker or a service.
/// </summary>
public interface IUsageExposer
{
    ValueTask<UsageView> ExposeUsageAsync(CancellationToken cancellationToken);
}
