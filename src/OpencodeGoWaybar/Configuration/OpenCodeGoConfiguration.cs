using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpencodeGoWaybar.Configuration;

/// <summary>
/// Builds <see cref="OpenCodeGoOptions"/> from defaults, an optional JSON
/// configuration file, environment variables, and (in development) the .NET
/// user secrets store.
///
/// <para>
/// This factory is the configuration composition step for the one-shot
/// application. The composition root calls <see cref="Build"/> once at startup
/// and treats the returned <see cref="IOptions{TOptions}"/> as the only public
/// way to read configuration. The factory itself is internal because it has
/// no business behavior; it only wires sources.
/// </para>
/// </summary>
internal static class OpenCodeGoConfiguration
{
    /// <summary>Prefix used to read options from environment variables.</summary>
    public const string EnvironmentVariablePrefix = "OPENCODE_GO_";

    /// <summary>
    /// Loads options from the listed sources in this order: defaults, the
    /// optional JSON file, environment variables, and (in development builds)
    /// user secrets. Later sources override earlier ones. Validation runs
    /// before the result is returned, so a misconfigured deployment fails fast.
    /// </summary>
    /// <param name="configPath">
    /// Optional path to a JSON configuration file. Missing or unreadable files
    /// are ignored; invalid JSON fails fast so the operator notices.
    /// </param>
    public static IOptions<OpenCodeGoOptions> Build(string? configPath = null)
    {
        var builder = new ConfigurationBuilder();

        if (configPath is not null && File.Exists(configPath))
        {
            builder.AddJsonFile(configPath, optional: true);
        }

        builder.AddEnvironmentVariables(prefix: EnvironmentVariablePrefix);

        if (IsDevelopmentBuild())
        {
            builder.AddUserSecrets<OpenCodeGoOptions>(optional: true);
        }

        var configuration = builder.Build();
        var services = new ServiceCollection()
            .AddOptions<OpenCodeGoOptions>()
            .Bind(configuration)
            .ValidateOnStart<OpenCodeGoOptions>();

        return services.Services.BuildServiceProvider().GetRequiredService<IOptions<OpenCodeGoOptions>>();
    }

    private static bool IsDevelopmentBuild() =>
#if DEBUG
        true;
#else
        false;
#endif
}