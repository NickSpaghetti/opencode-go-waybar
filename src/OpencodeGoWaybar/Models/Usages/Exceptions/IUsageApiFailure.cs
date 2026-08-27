using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Models.Usages.Exceptions;

/// <summary>A failure the usage API described in its response body.</summary>
internal interface IUsageApiFailure
{
    UsageApiError? ApiError { get; }
}
