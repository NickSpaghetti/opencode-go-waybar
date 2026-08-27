using Microsoft.Extensions.Configuration.UserSecrets;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Configurations;
using NSubstitute;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Configuration;

[Collection("Configuration")]
public class ConfigurationLoadingTests
{
    private static ConfigurationService CreateFoundation() =>
        new(new ConfigurationBroker(), Substitute.For<ILoggingBroker>());

    /// <summary>
    /// Home-relative defaults are expanded when options are bound, because
    /// nothing downstream expands `~` — File.Exists would take it literally.
    /// The home directory itself varies by environment, so assert the shape.
    /// </summary>
    private static void AssertExpandedHomePath(string expectedSuffix, string actual)
    {
        Assert.False(actual.StartsWith('~'), $"'{actual}' still starts with an unexpanded '~'.");
        Assert.EndsWith(expectedSuffix, actual, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldReturnDefaultsWhenConfigurationIsEmpty()
    {
        // given
        OpenCodeGoOptions options = CreateFoundation().RetrieveOptions(configPath: null);

        // then
        Assert.Equal(300, options.RefreshIntervalSeconds);
        AssertExpandedHomePath("/.local/share/opencode/auth.json", options.AuthPath);
    }

    [Fact]
    public void ShouldOverrideDefaultsFromJsonFile()
    {
        // given
        var path = WriteTempConfig("""
            {
              "RefreshIntervalSeconds": 600,
              "AuthPath": "/etc/opencode/auth.json"
            }
            """);

        OpenCodeGoOptions options = CreateFoundation().RetrieveOptions(configPath: path);

        // then
        Assert.Equal(600, options.RefreshIntervalSeconds);
        Assert.Equal("/etc/opencode/auth.json", options.AuthPath);
        AssertExpandedHomePath("/.local/share/opencode/opencode.db", options.DatabasePath);
    }

    [Fact]
    public void ShouldFallBackToDefaultsWhenJsonFileIsMissing()
    {
        // given
        OpenCodeGoOptions options = CreateFoundation().RetrieveOptions(configPath: "/tmp/does-not-exist.json");

        // then
        Assert.Equal(300, options.RefreshIntervalSeconds);
    }

    [Fact]
    public void ShouldOverrideJsonFileFromEnvironmentVariables()
    {
        // given
        var path = WriteTempConfig("""
            {
              "RefreshIntervalSeconds": 600
            }
            """);

        using var scope = new EnvironmentVariableScope(
            ("OPENCODE_GO_RefreshIntervalSeconds", "1800"),
            ("OPENCODE_GO_AuthPath", "/env/auth.json"));

        OpenCodeGoOptions options = CreateFoundation().RetrieveOptions(configPath: path);

        // then
        Assert.Equal(1800, options.RefreshIntervalSeconds);
        Assert.Equal("/env/auth.json", options.AuthPath);
    }

    [Fact]
    public void ShouldDeclareUserSecretsIdOnTheAssembly()
    {
        // given
        var attribute = typeof(OpenCodeGoOptions).Assembly
            .GetCustomAttributes(typeof(UserSecretsIdAttribute), inherit: false)
            .Cast<UserSecretsIdAttribute>()
            .SingleOrDefault();

        // then
        Assert.NotNull(attribute);
        Assert.Equal("opencode-go-waybar-development", attribute!.UserSecretsId);
    }

    [Fact]
    public void ShouldLeaveKnownKeysAtDefaultWhenJsonHasUnknownKeys()
    {
        // given
        var path = WriteTempConfig("""
            {
              "RefreshIntervalSeconds": 120,
              "FutureKey": "ignored"
            }
            """);

        OpenCodeGoOptions options = CreateFoundation().RetrieveOptions(configPath: path);

        // then
        Assert.Equal(120, options.RefreshIntervalSeconds);
        AssertExpandedHomePath("/.local/share/opencode/auth.json", options.AuthPath);
    }

    private static string WriteTempConfig(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"opencode-go-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}

internal sealed class EnvironmentVariableScope : IDisposable
{
    private readonly (string Name, string? PreviousValue)[] _variables;

    public EnvironmentVariableScope(params (string Name, string? Value)[] variables)
    {
        _variables = variables
            .Select(variable => (variable.Name, Environment.GetEnvironmentVariable(variable.Name)))
            .ToArray();

        foreach (var (name, value) in variables)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    public void Dispose()
    {
        foreach (var (name, previousValue) in _variables)
        {
            Environment.SetEnvironmentVariable(name, previousValue);
        }
    }
}
