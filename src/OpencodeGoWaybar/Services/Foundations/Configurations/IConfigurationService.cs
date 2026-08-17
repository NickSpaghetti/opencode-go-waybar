using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

internal interface IConfigurationService
{
    IOptions<OpenCodeGoOptions> RetrieveOptions(string? configPath = null);

    IOptions<OpenCodeGoSecrets> RetrieveSecrets(string? configPath = null);
}
