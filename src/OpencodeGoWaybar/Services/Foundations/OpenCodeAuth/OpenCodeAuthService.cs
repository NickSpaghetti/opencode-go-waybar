using OpencodeGoWaybar.Brokers.Credentials;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.OpenCodeAuths;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeAuth;

/// <summary>
/// Selects this module's credentials out of opencode's credential store, which
/// holds an entry per provider.
/// </summary>
internal sealed partial class OpenCodeAuthService(
    IOpenCodeAuthBroker authBroker,
    ILoggingBroker loggingBroker,
    OpenCodeGoOptions options) : IOpenCodeAuthService
{
    /// <summary>
    /// The provider id OpenCode Go is configured under — models are referenced
    /// as `opencode-go/&lt;model-id&gt;`, and the same id tags usage rows in the
    /// opencode database. See https://opencode.ai/docs/go/
    /// </summary>
    private const string OpenCodeGoProviderId = "opencode-go";

    /// <summary>Key-based providers; OAuth entries carry tokens instead.</summary>
    private const string ApiKeyEntryType = "api";

    public string? RetrieveApiKey() =>
        TryCatch(RetrieveOpenCodeGoApiKey);

    private string? RetrieveOpenCodeGoApiKey()
    {
        ValidateAuthPath();

        IReadOnlyDictionary<string, OpenCodeAuthEntry>? entries = authBroker.ReadAuthEntries();

        if (entries is null || !entries.TryGetValue(OpenCodeGoProviderId, out OpenCodeAuthEntry? entry))
        {
            return null;
        }

        return IsUsableApiKeyEntry(entry) ? entry.Key : null;
    }

    /// <summary>
    /// A usable entry carries a key. The type is checked when present so an
    /// OAuth entry is never mistaken for a key, but an entry that omits the type
    /// yet has a key is still accepted rather than silently discarded.
    /// </summary>
    private static bool IsUsableApiKeyEntry(OpenCodeAuthEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.Key)
        && (entry.Type is null || entry.Type.Equals(ApiKeyEntryType, StringComparison.OrdinalIgnoreCase));
}
