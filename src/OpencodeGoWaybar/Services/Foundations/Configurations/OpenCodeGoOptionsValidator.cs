using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

/// <summary>
/// The configuration service's own validation rules, split into a file rather than
/// handed to it. It used to arrive as an injected IValidateOptions&lt;T&gt; — a third
/// kind of dependency at a tier that admits only brokers (§2.1.2.1), and a
/// component trusting a neighbour to validate on its behalf (§2.0.2.4).
///
/// Kept as a callable class rather than folded into the service's partials so the
/// rules stay unit-testable on their own, without building configuration to reach
/// them.
/// </summary>
internal static class OpenCodeGoOptionsValidator
{
    /// <summary>Every rule the options break, or empty when they break none.</summary>
    internal static IReadOnlyList<string> Validate(OpenCodeGoOptions options)
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

        if (string.IsNullOrWhiteSpace(options.CacheDirectory))
        {
            failures.Add("CacheDirectory must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.WaybarStylePath))
        {
            failures.Add("WaybarStylePath must not be empty.");
        }

        if (options.UsageEndpoint is null ||
            !options.UsageEndpoint.IsAbsoluteUri ||
            options.UsageEndpoint.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("UsageEndpoint must be an absolute https URI.");
        }

        if (options.CautionPercent >= options.DangerPercent)
        {
            failures.Add("CautionPercent must be below DangerPercent.");
        }

        return failures;
    }
}
