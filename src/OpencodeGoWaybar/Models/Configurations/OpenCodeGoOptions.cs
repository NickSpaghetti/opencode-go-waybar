namespace OpencodeGoWaybar.Models.Configurations;

/// <summary>
/// Typed options for the opencode-go-waybar Waybar module. Loaded from defaults,
/// the JSON configuration file, environment variables, and (in development)
/// the .NET user secrets store. The bearer key for the OpenCode Go API is
/// never stored on this type.
/// </summary>
public sealed class OpenCodeGoOptions
{
    public const int MinRefreshIntervalSeconds = 60;
    public const int MaxRefreshIntervalSeconds = 3600;
    public const int MaxPromptDebounceSeconds = 60;
    public const int MinPromptDebounceSeconds = 0;
    public const string DefaultAuthPath = "~/.local/share/opencode/auth.json";
    public const string DefaultDatabasePath = "~/.local/share/opencode/opencode.db";
    public const string DefaultUsageEndpoint = "https://opencode.ai/zen/go/v1/usage";
    public const string EnvironmentVariablePrefix = "OPENCODE_GO_";
    public const string ApiKeyEnvironmentVariable = "OPENCODE_GO_API_KEY";

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

    public string AuthPath { get; set; } = DefaultAuthPath;

    public string DatabasePath { get; set; } = DefaultDatabasePath;

    public Uri UsageEndpoint { get; set; } = new(DefaultUsageEndpoint);
    
}
