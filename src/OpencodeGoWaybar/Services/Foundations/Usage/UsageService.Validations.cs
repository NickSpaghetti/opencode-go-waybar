using OpencodeGoWaybar.Brokers.Usages;
using OpencodeGoWaybar.Models.Usages.Exceptions;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.Usage;

internal sealed partial class UsageService
{
    private void ValidateApiKey()
    {
        if (string.IsNullOrWhiteSpace(_secrets.ApiKey))
        {
            throw new UsageCredentialsMissingException();
        }
    }

    private static void ValidateResponse(UsageResponse? response)
    {
        if (response?.Usage is null)
        {
            throw new UsageApiResponseException(new InvalidDataException("The usage response has no usage object."));
        }

        ValidateWindow(response.Usage.Rolling, "rolling");
        ValidateWindow(response.Usage.Weekly, "weekly");
        ValidateWindow(response.Usage.Monthly, "monthly");
    }

    private static void ValidateWindow(UsageWindow? window, string name)
    {
        if (window is null || string.IsNullOrWhiteSpace(window.Status))
        {
            throw new UsageApiResponseException(new InvalidDataException($"The {name} usage window is invalid."));
        }

        if (window.Percent is < 0 or > 100 || window.LimitDollars is < 0)
        {
            throw new UsageApiResponseException(new InvalidDataException($"The {name} usage window contains invalid values."));
        }

        if (window.Status.Equals("ok", StringComparison.OrdinalIgnoreCase) &&
            (window.Percent is null || window.ResetsAt is null))
        {
            throw new UsageApiResponseException(new InvalidDataException($"The {name} successful usage window is incomplete."));
        }
    }
}
