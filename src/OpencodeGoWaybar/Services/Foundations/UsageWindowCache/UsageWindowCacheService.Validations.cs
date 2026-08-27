using OpencodeGoWaybar.Models.Usages;
using OpencodeGoWaybar.Models.Usages.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.UsageWindowCache;

internal sealed partial class UsageWindowCacheService
{
    private void ValidateCacheDirectory()
    {
        if (string.IsNullOrWhiteSpace(options.CacheDirectory))
        {
            throw new CacheValidationException(
                new ArgumentException("CacheDirectory must not be empty."));
        }
    }

    private static void ValidateState(UsageWindowCacheState state)
    {
        if (state is null)
        {
            throw new CacheValidationException(
                new ArgumentNullException(nameof(state)));
        }
    }
}
