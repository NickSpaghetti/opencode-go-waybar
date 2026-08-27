using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Orchestrations.Credentials;

internal interface ICredentialsOrchestrationService
{
    /// <summary>Resolves the API key from the configured source.</summary>
    OpenCodeGoSecrets RetrieveSecrets();
}
