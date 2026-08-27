using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Secrets;

internal interface ISecretsService
{
    /// <summary>The credentials this module was configured with, if any.</summary>
    OpenCodeGoSecrets RetrieveSecrets(string? configPath = null);
}
