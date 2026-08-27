using OpencodeGoWaybar.Models.OpenCodeAuths;

namespace OpencodeGoWaybar.Brokers.Credentials;

/// <summary>Provides read access to opencode's own credential store.</summary>
internal interface IOpenCodeAuthBroker
{
    /// <summary>
    /// The stored credentials, keyed by provider id. Throws when the file is
    /// absent; what that absence means is the service's decision.
    /// </summary>
    IReadOnlyDictionary<string, OpenCodeAuthEntry>? ReadAuthEntries();
}
