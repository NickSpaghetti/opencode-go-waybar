using NSubstitute;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Configurations;
using OpencodeGoWaybar.Models.Configurations.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Configuration;

/// <summary>
/// The rules are called directly now rather than injected as an
/// IValidateOptions&lt;T&gt;, so they are still reachable on their own — which is the
/// reason they stayed a callable class instead of folding into the service's
/// partials.
/// </summary>
[Collection("Configuration")]
public sealed class OpenCodeGoOptionsValidatorTests
{
    [Fact]
    public void ShouldRejectEmptyPaths()
    {
        // given
        var options = new OpenCodeGoOptions
        {
            AuthPath = " ",
            DatabasePath = string.Empty,
            CacheDirectory = " ",
            WaybarStylePath = string.Empty,
        };

        // when
        IReadOnlyList<string> failures = OpenCodeGoOptionsValidator.Validate(options);

        // then
        Assert.Contains(failures, failure => failure.Contains("AuthPath", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("DatabasePath", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("CacheDirectory", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("WaybarStylePath", StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldRejectNonHttpsAndRelativeEndpoints()
    {
        // given
        var http = new OpenCodeGoOptions { UsageEndpoint = new Uri("http://localhost/usage") };
        var relative = new OpenCodeGoOptions { UsageEndpoint = new Uri("usage", UriKind.Relative) };

        // then
        Assert.NotEmpty(OpenCodeGoOptionsValidator.Validate(http));
        Assert.NotEmpty(OpenCodeGoOptionsValidator.Validate(relative));
    }

    [Fact]
    public void ShouldRejectNullEndpointFromConfiguration()
    {
        // given
        var options = new OpenCodeGoOptions { UsageEndpoint = null! };

        // then
        Assert.NotEmpty(OpenCodeGoOptionsValidator.Validate(options));
    }

    [Theory]
    [InlineData(90, 90)]
    [InlineData(95, 90)]
    public void ShouldRejectCautionPercentAtOrAboveDangerPercent(
        int cautionPercent,
        int dangerPercent)
    {
        // given
        var options = new OpenCodeGoOptions
        {
            CautionPercent = cautionPercent,
            DangerPercent = dangerPercent,
        };

        // when
        IReadOnlyList<string> failures = OpenCodeGoOptionsValidator.Validate(options);

        // then
        Assert.Contains(
            failures,
            failure => failure.Contains("CautionPercent", StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldAcceptTheDocumentedDefaults()
    {
        // given
        var options = new OpenCodeGoOptions();

        // then nothing shipped as a default breaks its own rules
        Assert.Empty(OpenCodeGoOptionsValidator.Validate(options));
    }

    [Fact]
    public void ShouldValidateBeforeReturningOptions()
    {
        // given
        using var scope = new EnvironmentVariableScope(
            ("OPENCODE_GO_AuthPath", ""),
            ("OPENCODE_GO_DatabasePath", "/tmp/opencode.db"));

        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new ConfigurationService(new ConfigurationBroker(), loggingBroker);

        // when and then the service raises its own exception, not a native one
        Assert.Throws<ConfigurationServiceException>(() => foundation.RetrieveOptions());
        loggingBroker.Received(1).LogError(Arg.Any<ConfigurationServiceException>());
    }
}
