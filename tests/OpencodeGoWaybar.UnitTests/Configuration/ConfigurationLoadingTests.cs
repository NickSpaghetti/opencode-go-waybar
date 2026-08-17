using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration.UserSecrets;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Configurations;
using NSubstitute;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Configuration;

[Collection("Configuration")]
public class ConfigurationLoadingTests
{
    private static ConfigurationService CreateFoundation() =>
        new(new ConfigurationBroker(), new OpenCodeGoOptionsValidator(), Substitute.For<ILoggingBroker>());

    [Fact]
    public void EmptyConfigurationReturnsDefaults()
    {
        IOptions<OpenCodeGoOptions> options = CreateFoundation().RetrieveOptions(configPath: null);

        Assert.Equal(300, options.Value.RefreshIntervalSeconds);
        Assert.Equal("~/.local/share/opencode/auth.json", options.Value.AuthPath);
    }

    [Fact]
    public void JsonFileOverridesDefaults()
    {
        var path = WriteTempConfig("""
            {
              "RefreshIntervalSeconds": 600,
              "AuthPath": "/etc/opencode/auth.json"
            }
            """);

        IOptions<OpenCodeGoOptions> options = CreateFoundation().RetrieveOptions(configPath: path);

        Assert.Equal(600, options.Value.RefreshIntervalSeconds);
        Assert.Equal("/etc/opencode/auth.json", options.Value.AuthPath);
        Assert.Equal("~/.local/share/opencode/opencode.db", options.Value.DatabasePath);
    }

    [Fact]
    public void MissingJsonFileFallsBackToDefaults()
    {
        IOptions<OpenCodeGoOptions> options = CreateFoundation().RetrieveOptions(configPath: "/tmp/does-not-exist.json");

        Assert.Equal(300, options.Value.RefreshIntervalSeconds);
    }

    [Fact]
    public void EnvironmentVariablesOverrideJsonFile()
    {
        var path = WriteTempConfig("""
            {
              "RefreshIntervalSeconds": 600
            }
            """);

        using var scope = new EnvironmentVariableScope(
            ("OPENCODE_GO_RefreshIntervalSeconds", "1800"),
            ("OPENCODE_GO_AuthPath", "/env/auth.json"));

        IOptions<OpenCodeGoOptions> options = CreateFoundation().RetrieveOptions(configPath: path);

        Assert.Equal(1800, options.Value.RefreshIntervalSeconds);
        Assert.Equal("/env/auth.json", options.Value.AuthPath);
    }

    [Fact]
    public void AssemblyDeclaresUserSecretsId()
    {
        var attribute = typeof(OpenCodeGoOptions).Assembly
            .GetCustomAttributes(typeof(UserSecretsIdAttribute), inherit: false)
            .Cast<UserSecretsIdAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("opencode-go-waybar-development", attribute!.UserSecretsId);
    }

    [Fact]
    public void ReadsApiKeyFromTheDedicatedEnvironmentVariable()
    {
        using var scope = new EnvironmentVariableScope(
            (OpenCodeGoOptions.ApiKeyEnvironmentVariable, "test-api-key"));

        var secrets = CreateFoundation().RetrieveSecrets().Value;

        Assert.Equal("test-api-key", secrets.ApiKey);
    }

    [Fact]
    public void JsonFileWithUnknownKeysLeavesKnownKeysAtDefault()
    {
        var path = WriteTempConfig("""
            {
              "RefreshIntervalSeconds": 120,
              "FutureKey": "ignored"
            }
            """);

        IOptions<OpenCodeGoOptions> options = CreateFoundation().RetrieveOptions(configPath: path);

        Assert.Equal(120, options.Value.RefreshIntervalSeconds);
        Assert.Equal("~/.local/share/opencode/auth.json", options.Value.AuthPath);
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
