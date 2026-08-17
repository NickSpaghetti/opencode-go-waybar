namespace OpencodeGoWaybar.Models.Configurations;

/// <summary>
/// Secret inputs kept separate from normal application options, so the API key
/// cannot accidentally appear in the configuration output or Waybar state.
/// </summary>
internal sealed class OpenCodeGoSecrets
{
    public string? ApiKey { get; init; }
}
