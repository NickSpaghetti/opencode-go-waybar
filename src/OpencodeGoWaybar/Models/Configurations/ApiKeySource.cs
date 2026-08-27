namespace OpencodeGoWaybar.Models.Configurations;

/// <summary>Where the OpenCode Go API key is read from.</summary>
internal enum ApiKeySource
{
    /// <summary>
    /// Prefer the configured key, fall back to opencode's own credential store.
    /// </summary>
    Auto = 0,

    /// <summary>Only the OPENCODE_GO_API_KEY environment variable or user secrets.</summary>
    Environment = 1,

    /// <summary>Only opencode's credential store at <see cref="OpenCodeGoOptions.AuthPath"/>.</summary>
    AuthFile = 2,
}
