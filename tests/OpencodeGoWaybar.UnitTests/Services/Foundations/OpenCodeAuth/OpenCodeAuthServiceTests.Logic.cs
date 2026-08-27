using OpencodeGoWaybar.Models.OpenCodeAuths;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.OpenCodeAuth;

public sealed partial class OpenCodeAuthServiceTests
{
    [Fact]
    public void ShouldRetrieveTheOpenCodeGoApiKey()
    {
        // given
        var service = CreateService(new Dictionary<string, OpenCodeAuthEntry>
        {
            ["opencode-go"] = new() { Type = "api", Key = "sk-from-auth-file" },
        });

        // when
        var apiKey = service.RetrieveApiKey();

        // then
        Assert.Equal("sk-from-auth-file", apiKey);
    }

    [Fact]
    public void ShouldIgnoreOtherProviders()
    {
        // given
        var service = CreateService(new Dictionary<string, OpenCodeAuthEntry>
        {
            ["anthropic"] = new() { Type = "api", Key = "sk-ant-not-ours" },
            ["cursor"] = new() { Type = "oauth" },
        });

        // when
        var apiKey = service.RetrieveApiKey();

        // then
        Assert.Null(apiKey);
    }

    [Fact]
    public void ShouldReturnNoKeyWhenGoIsPresentButCarriesNone()
    {
        // given
        var service = CreateService(new Dictionary<string, OpenCodeAuthEntry>
        {
            ["opencode-go"] = new() { Type = "oauth" },
        });

        // when
        var apiKey = service.RetrieveApiKey();

        // then
        Assert.Null(apiKey);
    }

    [Fact]
    public void ShouldAcceptAnEntryWithAKeyButNoDeclaredType()
    {
        // given
        var service = CreateService(new Dictionary<string, OpenCodeAuthEntry>
        {
            ["opencode-go"] = new() { Key = "sk-untyped" },
        });

        // when
        var apiKey = service.RetrieveApiKey();

        // then
        Assert.Equal("sk-untyped", apiKey);
    }

    [Fact]
    public void ShouldReturnNoKeyWhenTheStoreHasNoEntries()
    {
        // given
        var service = CreateService(new Dictionary<string, OpenCodeAuthEntry>());

        // when
        var apiKey = service.RetrieveApiKey();

        // then
        Assert.Null(apiKey);
    }
}
