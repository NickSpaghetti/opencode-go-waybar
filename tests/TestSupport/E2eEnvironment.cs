namespace OpencodeGoWaybar.TestSupport;

/// <summary>
/// Locates everything the suite drives. All of it is provided by the e2e image
/// (see Dockerfile.e2e) and overridable so the suite can be pointed at a local
/// checkout instead.
/// </summary>
internal static class E2eEnvironment
{
    public static string Workspace =>
        Environment.GetEnvironmentVariable("E2E_WORKSPACE") ?? "/workspace";

    /// <summary>
    /// The shipped artifact: the NativeAOT linux-x64 single-file binary, built
    /// into the image by Dockerfile.e2e. The acceptance tier runs this rather
    /// than a Debug dll so trim and AOT behaviour is actually covered.
    /// </summary>
    public static string ModuleBinary =>
        Environment.GetEnvironmentVariable("E2E_MODULE_BIN")
        ?? "/opt/opencode-go-waybar/opencode-go-waybar";

    /// <summary>opencode as installed by https://opencode.ai/install.</summary>
    public static string ScriptInstalledOpenCode =>
        Environment.GetEnvironmentVariable("OPENCODE_SCRIPT_BIN") ?? "/opt/opencode/script/bin/opencode";

    /// <summary>opencode as installed by `npm install -g opencode-ai`.</summary>
    public static string NpmInstalledOpenCode =>
        Environment.GetEnvironmentVariable("OPENCODE_NPM_BIN") ?? "/usr/local/bin/opencode";

    /// <summary>The synthetic ACP client, run as a .NET file-based app.</summary>
    public static string AcpClientSource =>
        Path.Combine(Workspace, "tests/e2e/AcpClient.cs");

    public static string NeovimInit =>
        Environment.GetEnvironmentVariable("E2E_NVIM_INIT") ?? "/opt/nvim-e2e/init.lua";

    /// <summary>
    /// Whether a live API key reached the container. Passed through by the
    /// Makefile rather than baked in, so a keyless run simply skips that tier.
    /// </summary>
    public static bool HasApiKey =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENCODE_GO_API_KEY"));

    /// <summary>Which image layer is running, so tests assert only what is installed.</summary>
    public static int Layer =>
        int.TryParse(Environment.GetEnvironmentVariable("E2E_LAYER"), out var layer) ? layer : 0;
}
