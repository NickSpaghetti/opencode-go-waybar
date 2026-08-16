using OpencodeGoWaybar.Configuration;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Configuration;

public class OpenCodeGoOptionsTests
{
    [Fact]
    public void DefaultsMatchPlan()
    {
        var options = new OpenCodeGoOptions();

        Assert.Equal(300, options.RefreshIntervalSeconds);
        Assert.True(options.PromptRefreshEnabled);
        Assert.Equal(3, options.PromptRefreshDebounceSeconds);
        Assert.Equal("~/.local/share/opencode/auth.json", options.AuthPath);
        Assert.Equal("~/.local/share/opencode/opencode.db", options.DatabasePath);
        Assert.Equal(new Uri("https://opencode.ai/zen/go/v1/usage"), options.UsageEndpoint);
    }

    [Fact]
    public void WithPrimaryOverridesDefaults()
    {
        var primary = new OpenCodeGoOptions
        {
            RefreshIntervalSeconds = 600,
            PromptRefreshEnabled = false,
            PromptRefreshDebounceSeconds = 10,
            AuthPath = "/tmp/auth.json",
            DatabasePath = "/tmp/opencode.db",
            UsageEndpoint = new Uri("https://staging.opencode.ai/zen/go/v1/usage"),
        };

        Assert.Equal(600, primary.RefreshIntervalSeconds);
        Assert.False(primary.PromptRefreshEnabled);
        Assert.Equal(10, primary.PromptRefreshDebounceSeconds);
        Assert.Equal("/tmp/auth.json", primary.AuthPath);
        Assert.Equal("/tmp/opencode.db", primary.DatabasePath);
        Assert.Equal(new Uri("https://staging.opencode.ai/zen/go/v1/usage"), primary.UsageEndpoint);
    }

    [Fact]
    public void ClampRefreshIntervalSecondsWithinRange()
    {
        var tooLow = new OpenCodeGoOptions { RefreshIntervalSeconds = 5 };
        var tooHigh = new OpenCodeGoOptions { RefreshIntervalSeconds = 100_000 };

        Assert.Equal(60, tooLow.RefreshIntervalSeconds);
        Assert.Equal(3600, tooHigh.RefreshIntervalSeconds);
    }

    [Fact]
    public void ClampPromptRefreshDebounceSecondsWithinRange()
    {
        var tooLow = new OpenCodeGoOptions { PromptRefreshDebounceSeconds = 0 };
        var tooHigh = new OpenCodeGoOptions { PromptRefreshDebounceSeconds = 9999 };

        Assert.True(tooLow.PromptRefreshDebounceSeconds >= 0);
        Assert.True(tooHigh.PromptRefreshDebounceSeconds <= 60);
    }

    [Fact]
    public void OptionsDoNotExposeAnApiKey()
    {
        var options = new OpenCodeGoOptions();
        Assert.Null(options.GetType().GetProperty("ApiKey"));
        Assert.Null(options.GetType().GetProperty("Key"));
        Assert.Null(options.GetType().GetProperty("BearerKey"));
    }
}