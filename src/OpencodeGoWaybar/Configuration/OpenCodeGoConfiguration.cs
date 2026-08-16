using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpencodeGoWaybar.Configuration;

/// <summary>
/// Builds <see cref="OpenCodeGoOptions"/> from defaults, an optional JSON
/// configuration file, user secrets in development, and environment variables.
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
/// optional JSON file, user secrets in development, and environment variables.
/// Later sources override earlier ones. Validation runs
    /// before the result is returned, so a misconfigured deployment fails fast.
    /// </summary>
    /// <param name="configPath">
    /// Optional path to a JSON configuration file. Missing files are ignored;
    /// invalid or unreadable files fail fast so the operator notices.
    /// </param>
    public static IOptions<OpenCodeGoOptions> Build(string? configPath = null)
    {
        var builder = new ConfigurationBuilder();

        if (configPath is not null && File.Exists(configPath))
        {
            builder.AddJsonFile(configPath, optional: true);
        }

        if (IsDevelopmentBuild())
        {
            builder.AddUserSecrets<OpenCodeGoOptions>(optional: true);
        }

        builder.AddEnvironmentVariables(prefix: EnvironmentVariablePrefix);

        var configuration = builder.Build();
        try
        {
            var services = new ServiceCollection();
            services.AddOptions<OpenCodeGoOptions>().Bind(configuration);
            services.AddSingleton<IValidateOptions<OpenCodeGoOptions>, OpenCodeGoOptionsValidator>();

            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<OpenCodeGoOptions>>().Value;
            return Options.Create(options);
        }
        finally
        {
            (configuration as IDisposable)?.Dispose();
        }
    }

    private static bool IsDevelopmentBuild() =>
#if DEBUG
        true;
#else
        false;
#endif
}
