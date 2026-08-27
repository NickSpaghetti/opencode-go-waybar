using System.Text.Json;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.OpenCodeAuths;

namespace OpencodeGoWaybar.Brokers.Credentials;

internal sealed class OpenCodeAuthBroker(OpenCodeGoOptions options) : IOpenCodeAuthBroker
{
    private string AuthPath => options.AuthPath;

    public IReadOnlyDictionary<string, OpenCodeAuthEntry>? ReadAuthEntries() =>
        JsonSerializer.Deserialize(
            File.ReadAllText(AuthPath),
            OpenCodeAuthJsonContext.Default.AuthDocument);
}
