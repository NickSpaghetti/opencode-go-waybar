namespace OpencodeGoWaybar.Models.Configurations.Exceptions;

/// <summary>
/// Indicates that the bound configuration breaks one or more of its own rules.
/// Local by design: the previous native OptionsValidationException carried a
/// Microsoft.Extensions type out of the foundation (§2.1.3).
/// </summary>
internal sealed class InvalidOpenCodeGoOptionsException(IReadOnlyList<string> failures)
    : Exception($"The opencode-go-waybar configuration is invalid. {string.Join(" ", failures)}")
{
    public IReadOnlyList<string> Failures { get; } = failures;
}

/// <summary>
/// Categorizes a configuration failure after the foundation has logged it.
/// The inner exception retains the source-specific binding or provider detail.
/// </summary>
internal sealed class ConfigurationServiceException(Exception innerException)
    : Exception("The configuration service failed.", innerException);
