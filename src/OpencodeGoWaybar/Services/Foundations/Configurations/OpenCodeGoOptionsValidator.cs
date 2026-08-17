using Microsoft.Extensions.Options;

using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

/// <summary>
/// Validates <see cref="OpenCodeGoOptions"/> at startup. The configuration
/// factory calls <see cref="Validate"/> after binding so a misconfigured
/// deployment fails before the application emits JSON.
/// </summary>
internal sealed class OpenCodeGoOptionsValidator : IValidateOptions<OpenCodeGoOptions>
{
    public ValidateOptionsResult Validate(string? name, OpenCodeGoOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.AuthPath))
        {
            failures.Add("AuthPath must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.DatabasePath))
        {
            failures.Add("DatabasePath must not be empty.");
        }

        if (options.UsageEndpoint is null ||
            !options.UsageEndpoint.IsAbsoluteUri ||
            options.UsageEndpoint.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("UsageEndpoint must be an absolute https URI.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
