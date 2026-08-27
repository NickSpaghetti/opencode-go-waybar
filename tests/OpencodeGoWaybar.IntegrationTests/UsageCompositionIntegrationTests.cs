using Microsoft.Extensions.DependencyInjection;
using OpencodeGoWaybar.Configurations;
using OpencodeGoWaybar.Exposers.Themes;
using OpencodeGoWaybar.Exposers.Usages;
using OpencodeGoWaybar.Exposers.Waybar;
using Xunit;

namespace OpencodeGoWaybar.IntegrationTests;

/// <summary>
/// A missing or mistyped registration compiles perfectly and only fails when the
/// container is asked for the service, so the graph is resolved for real here.
/// This sits in the integration tier because building it binds the actual
/// configuration sources on the host.
/// </summary>
[Trait("Tier", "Integration")]
public sealed class UsageCompositionIntegrationTests
{
    [Fact]
    public void ShouldResolveTheWaybarExposerFromTheComposition()
    {
        // given
        using var serviceProvider = UsageComposition.BuildServiceProvider();

        // when
        var exposer = serviceProvider.GetRequiredService<IWaybarExposer>();

        // then
        Assert.NotNull(exposer);
    }

    [Fact]
    public void ShouldResolveTheThemeExposerFromTheComposition()
    {
        // given
        using var serviceProvider = UsageComposition.BuildServiceProvider();

        // when
        var exposer = serviceProvider.GetRequiredService<IThemeExposer>();

        // then
        Assert.NotNull(exposer);
    }

    [Fact]
    public void ShouldResolveTheUsageExposerFromTheComposition()
    {
        // given
        using var serviceProvider = UsageComposition.BuildServiceProvider();

        // when
        var exposer = serviceProvider.GetRequiredService<IUsageExposer>();

        // then
        Assert.NotNull(exposer);
    }
}
