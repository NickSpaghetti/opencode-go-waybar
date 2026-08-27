using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

internal interface IConfigurationService
{
    OpenCodeGoOptions RetrieveOptions(string? configPath = null);
}
