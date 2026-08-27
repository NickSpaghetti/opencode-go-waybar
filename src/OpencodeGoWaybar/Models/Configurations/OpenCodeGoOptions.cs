namespace OpencodeGoWaybar.Models.Configurations;

/// <summary>
/// Typed options for the opencode-go-waybar Waybar module. Loaded from defaults,
/// the JSON configuration file, environment variables, and (in development)
/// the .NET user secrets store. The bearer key for the OpenCode Go API is
/// never stored on this type.
/// </summary>
internal sealed class OpenCodeGoOptions
{
    public const int MinRefreshIntervalSeconds = 60;
    public const int MaxRefreshIntervalSeconds = 3600;
    public const int MaxPromptDebounceSeconds = 60;
    public const int MinPromptDebounceSeconds = 0;
    public const int MinPercentThreshold = 1;
    public const int MaxPercentThreshold = 100;
    public const string DefaultAuthPath = "~/.local/share/opencode/auth.json";
    public const string DefaultDatabasePath = "~/.local/share/opencode/opencode.db";
    public const string DefaultCacheDirectory = "~/.cache/opencode-go-waybar";

    /// <summary>
    /// The bar's own stylesheet. Read so a detail window can be painted in the
    /// same colours; its @import chain is followed from here. The file need not
    /// exist — a machine with no Waybar simply has no theme to match.
    /// </summary>
    public const string DefaultWaybarStylePath = "~/.config/waybar/style.css";

    /// <summary>
    /// Where the optional JSON configuration file is looked for when no path is
    /// supplied. The file need not exist.
    /// </summary>
    public const string DefaultConfigPath = "~/.config/opencode-go-waybar/config.json";
    public const string DefaultUsageEndpoint = "https://opencode.ai/zen/go/v1/usage";
    public const string EnvironmentVariablePrefix = "OPENCODE_GO_";
    public const string ApiKeyEnvironmentVariable = "OPENCODE_GO_API_KEY";
    public const string ProcessPresentEnvironmentVariable = "OPENCODE_GO_PROCESS_PRESENT";

    public int RefreshIntervalSeconds
    {
        get;
        set => field = Math.Clamp(value, MinRefreshIntervalSeconds, MaxRefreshIntervalSeconds);
    } = 300;

    public bool PromptRefreshEnabled { get; set; } = true;

    public int PromptRefreshDebounceSeconds
    {
        get;
        set => field = Math.Clamp(value, MinPromptDebounceSeconds, MaxPromptDebounceSeconds);
    } = 3;

    /// <summary>
    /// The percentage at which a usage window stops reading as healthy. Exposed
    /// as a threshold rather than a hard-coded constant because what counts as
    /// "getting close" depends on the plan being watched.
    /// </summary>
    public int CautionPercent
    {
        get;
        set => field = Math.Clamp(value, MinPercentThreshold, MaxPercentThreshold);
    } = 75;

    /// <summary>
    /// The percentage at which a usage window reads as spent. Must sit above
    /// <see cref="CautionPercent"/>; the validator rejects the inverted case
    /// rather than silently reordering, so a bad config fails loudly at startup.
    /// </summary>
    public int DangerPercent
    {
        get;
        set => field = Math.Clamp(value, MinPercentThreshold, MaxPercentThreshold);
    } = 90;

    public string AuthPath { get; set; } = DefaultAuthPath;

    public string DatabasePath { get; set; } = DefaultDatabasePath;

    /// <summary>
    /// Where the module keeps what it remembers between polls. A directory rather
    /// than a file: the window and history halves are written independently, each
    /// to its own file, so that neither has to rewrite the other's data.
    /// </summary>
    public string CacheDirectory { get; set; } = DefaultCacheDirectory;

    public string WaybarStylePath { get; set; } = DefaultWaybarStylePath;

    public Uri UsageEndpoint { get; set; } = new(DefaultUsageEndpoint);

    /// <summary>
    /// Where to read the API key from. <see cref="ApiKeySource.Auto"/> prefers a
    /// configured key and falls back to opencode's own credential store, so the
    /// module works out of the box once `/connect` has been run in opencode.
    /// </summary>
    public ApiKeySource ApiKeySource { get; set; } = ApiKeySource.Auto;

    /// <summary>
    /// Forces the process-detection result instead of reading the operating system
    /// process table. Set by <see cref="ProcessPresentEnvironmentVariable"/> for
    /// container and acceptance runs, where no opencode process exists.
    /// </summary>
    public bool? ProcessPresentOverride { get; set; }
}
