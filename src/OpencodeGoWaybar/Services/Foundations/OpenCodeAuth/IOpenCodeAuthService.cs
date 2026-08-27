namespace OpencodeGoWaybar.Services.Foundations.OpenCodeAuth;

internal interface IOpenCodeAuthService
{
    /// <summary>
    /// The OpenCode Go API key opencode itself stores, or null when Go has not
    /// been connected. Written by `/connect` in the opencode TUI.
    /// </summary>
    string? RetrieveApiKey();
}
