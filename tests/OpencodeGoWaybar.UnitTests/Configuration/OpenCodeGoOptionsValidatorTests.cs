using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Configurations;
using OpencodeGoWaybar.Services.Foundations.Configurations.Exceptions;
using NSubstitute;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Configuration;

[Collection("Configuration")]
public sealed class OpenCodeGoOptionsValidatorTests
{
    private readonly OpenCodeGoOptionsValidator _validator = new();

    [Fact]
    public void RejectsEmptyPaths()
    {
        var options = new OpenCodeGoOptions
        {
            AuthPath = " ",
            DatabasePath = string.Empty,
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("AuthPath", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("DatabasePath", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsNonHttpsAndRelativeEndpoints()
    {
        var http = new OpenCodeGoOptions { UsageEndpoint = new Uri("http://localhost/usage") };
        var relative = new OpenCodeGoOptions { UsageEndpoint = new Uri("usage", UriKind.Relative) };

        Assert.False(_validator.Validate(null, http).Succeeded);
        Assert.False(_validator.Validate(null, relative).Succeeded);
    }

    [Fact]
    public void RejectsNullEndpointFromConfiguration()
    {
        var options = new OpenCodeGoOptions { UsageEndpoint = null! };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void BuildTriggersValidationBeforeReturningOptions()
    {
        using var scope = new EnvironmentVariableScope(
            ("OPENCODE_GO_AuthPath", ""),
            ("OPENCODE_GO_DatabasePath", "/tmp/opencode.db"));

        var loggingBroker = Substitute.For<ILoggingBroker>();
        var foundation = new ConfigurationService(new ConfigurationBroker(), new OpenCodeGoOptionsValidator(), loggingBroker);

        Assert.Throws<ConfigurationServiceException>(() => foundation.RetrieveOptions());
        loggingBroker.Received(1).LogError(Arg.Any<ConfigurationServiceException>());
    }
}
