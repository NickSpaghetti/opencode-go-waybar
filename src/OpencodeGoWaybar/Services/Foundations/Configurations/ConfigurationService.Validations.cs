using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Configurations.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

internal sealed partial class ConfigurationService
{
    private static void ValidateOptions(OpenCodeGoOptions options)
    {
        IReadOnlyList<string> failures = OpenCodeGoOptionsValidator.Validate(options);

        if (failures.Count > 0)
        {
            throw new InvalidOpenCodeGoOptionsException(failures);
        }
    }
}
