namespace OpencodeGoWaybar.Models.OpenCodeAuths;

/// <summary>
/// One provider's credentials as opencode stores them, modelled to reflect that
/// document. Entries are heterogeneous — an OAuth provider carries
/// refresh/access/expires instead of a key — so unrelated members are absent.
/// </summary>
internal sealed class OpenCodeAuthEntry
{
    /// <summary>"api" for key-based providers, "oauth" for the token-based ones.</summary>
    public string? Type { get; init; }

    /// <summary>The API key, present on "api" entries.</summary>
    public string? Key { get; init; }
}
