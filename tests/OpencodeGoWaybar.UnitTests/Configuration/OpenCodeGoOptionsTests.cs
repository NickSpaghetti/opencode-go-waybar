using OpencodeGoWaybar.Models.Configurations;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Configuration;

public class OpenCodeGoOptionsTests
{
    [Fact]
    public void ShouldMatchTheDocumentedDefaults()
    {
        // given
        var options = new OpenCodeGoOptions();

        // then
        Assert.Equal(300, options.RefreshIntervalSeconds);
        Assert.True(options.PromptRefreshEnabled);
        Assert.Equal(3, options.PromptRefreshDebounceSeconds);
        Assert.Equal("~/.local/share/opencode/auth.json", options.AuthPath);
        Assert.Equal("~/.local/share/opencode/opencode.db", options.DatabasePath);
        Assert.Equal(new Uri("https://opencode.ai/zen/go/v1/usage"), options.UsageEndpoint);
        Assert.Equal("~/.config/waybar/style.css", options.WaybarStylePath);
        Assert.Equal(75, options.CautionPercent);
        Assert.Equal(90, options.DangerPercent);
    }

    [Fact]
    public void ShouldOverrideDefaultsWithPrimaryValues()
    {
        // given
        var primary = new OpenCodeGoOptions
        {
            RefreshIntervalSeconds = 600,
            PromptRefreshEnabled = false,
            PromptRefreshDebounceSeconds = 10,
            AuthPath = "/tmp/auth.json",
            DatabasePath = "/tmp/opencode.db",
            UsageEndpoint = new Uri("https://staging.opencode.ai/zen/go/v1/usage"),
        };

        // then
        Assert.Equal(600, primary.RefreshIntervalSeconds);
        Assert.False(primary.PromptRefreshEnabled);
        Assert.Equal(10, primary.PromptRefreshDebounceSeconds);
        Assert.Equal("/tmp/auth.json", primary.AuthPath);
        Assert.Equal("/tmp/opencode.db", primary.DatabasePath);
        Assert.Equal(new Uri("https://staging.opencode.ai/zen/go/v1/usage"), primary.UsageEndpoint);
    }

    [Fact]
    public void ShouldClampRefreshIntervalSecondsWithinRange()
    {
        // given
        var tooLow = new OpenCodeGoOptions { RefreshIntervalSeconds = 5 };
        var tooHigh = new OpenCodeGoOptions { RefreshIntervalSeconds = 100_000 };

        // then
        Assert.Equal(60, tooLow.RefreshIntervalSeconds);
        Assert.Equal(3600, tooHigh.RefreshIntervalSeconds);
    }

    [Fact]
    public void ShouldClampPromptRefreshDebounceSecondsWithinRange()
    {
        // given
        var tooLow = new OpenCodeGoOptions { PromptRefreshDebounceSeconds = 0 };
        var tooHigh = new OpenCodeGoOptions { PromptRefreshDebounceSeconds = 9999 };

        // then
        Assert.True(tooLow.PromptRefreshDebounceSeconds >= 0);
        Assert.True(tooHigh.PromptRefreshDebounceSeconds <= 60);
    }

    [Fact]
    public void ShouldClampCautionPercentWithinRange()
    {
        // given
        var tooLow = new OpenCodeGoOptions { CautionPercent = 0 };
        var tooHigh = new OpenCodeGoOptions { CautionPercent = 250 };

        // then
        Assert.Equal(OpenCodeGoOptions.MinPercentThreshold, tooLow.CautionPercent);
        Assert.Equal(OpenCodeGoOptions.MaxPercentThreshold, tooHigh.CautionPercent);
    }

    [Fact]
    public void ShouldClampDangerPercentWithinRange()
    {
        // given
        var tooLow = new OpenCodeGoOptions { DangerPercent = -5 };
        var tooHigh = new OpenCodeGoOptions { DangerPercent = 1_000 };

        // then
        Assert.Equal(OpenCodeGoOptions.MinPercentThreshold, tooLow.DangerPercent);
        Assert.Equal(OpenCodeGoOptions.MaxPercentThreshold, tooHigh.DangerPercent);
    }

    [Fact]
    public void ShouldNotExposeAnApiKeyOnOptions()
    {
        // given
        var options = new OpenCodeGoOptions();
        // then
        Assert.Null(options.GetType().GetProperty("ApiKey"));
        Assert.Null(options.GetType().GetProperty("Key"));
        Assert.Null(options.GetType().GetProperty("BearerKey"));
    }
}
