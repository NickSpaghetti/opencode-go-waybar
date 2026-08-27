using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Secrets;
using OpencodeGoWaybar.Services.Foundations.OpenCodeAuth;

namespace OpencodeGoWaybar.Services.Orchestrations.Credentials;

/// <summary>
/// Chooses between the two places an OpenCode Go key can live: this module's own
/// configuration, and opencode's credential store. Higher-order logic over two
/// foundation services, so it sits above both rather than inside either.
/// </summary>
internal sealed class CredentialsOrchestrationService(
    ISecretsService secretsService,
    IOpenCodeAuthService authService,
    OpenCodeGoOptions options) : ICredentialsOrchestrationService
{
    public OpenCodeGoSecrets RetrieveSecrets()
    {
        var apiKey = options.ApiKeySource switch
        {
            ApiKeySource.Environment => RetrieveConfiguredApiKey(),
            ApiKeySource.AuthFile => authService.RetrieveApiKey(),
            _ => RetrieveConfiguredApiKey() ?? authService.RetrieveApiKey(),
        };

        return new OpenCodeGoSecrets { ApiKey = apiKey };
    }

    /// <summary>
    /// The key as configured for this module — OPENCODE_GO_API_KEY in the
    /// environment, or user secrets in a development build. Whitespace is
    /// treated as absent so a blank variable falls through to the auth file.
    /// </summary>
    private string? RetrieveConfiguredApiKey()
    {
        var apiKey = secretsService.RetrieveSecrets().ApiKey;

        return string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
    }
}
