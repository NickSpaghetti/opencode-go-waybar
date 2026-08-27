using Microsoft.Extensions.Configuration;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.Configurations;

namespace OpencodeGoWaybar.Services.Foundations.Configurations;

internal sealed partial class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationBroker _configurationBroker;
    private readonly ILoggingBroker _loggingBroker;

    public ConfigurationService(
        IConfigurationBroker configurationBroker,
        ILoggingBroker loggingBroker)
    {
        _configurationBroker = configurationBroker;
        _loggingBroker = loggingBroker;
    }
    public OpenCodeGoOptions RetrieveOptions(string? configPath = null)
    {
        return TryCatch(() => RetrieveOptionsCore(configPath));
    }

    private OpenCodeGoOptions RetrieveOptionsCore(string? configPath)
    {
        var configuration = _configurationBroker.Build(
            ExpandHomeRelativePath(configPath ?? OpenCodeGoOptions.DefaultConfigPath));
        try
        {
            var options = new OpenCodeGoOptions();
            configuration.Bind(options);
            BindProcessPresentOverride(configuration, options);
            ExpandHomeRelativePaths(options);
            ValidateOptions(options);
            return options;
        }
        finally
        {
            (configuration as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Reads the process-detection override by its full environment variable name.
    /// The prefixed provider strips <c>OPENCODE_GO_</c> down to <c>PROCESS_PRESENT</c>,
    /// which the binder will not match to <see cref="OpenCodeGoOptions.ProcessPresentOverride"/>.
    /// A value already bound from a JSON file survives when the variable is unset.
    /// </summary>
    private static void BindProcessPresentOverride(IConfiguration configuration, OpenCodeGoOptions options)
    {
        if (bool.TryParse(configuration[OpenCodeGoOptions.ProcessPresentEnvironmentVariable], out var processIsPresent))
        {
            options.ProcessPresentOverride = processIsPresent;
        }
    }

    /// <summary>
    /// Turns the documented `~/...` defaults into real paths. Nothing else in
    /// the process expands them: the shell does it for command lines, but these
    /// values arrive from configuration, where a literal `~` would mean a
    /// directory of that name in the working directory.
    /// </summary>
    private static void ExpandHomeRelativePaths(OpenCodeGoOptions options)
    {
        options.AuthPath = ExpandHomeRelativePath(options.AuthPath);
        options.DatabasePath = ExpandHomeRelativePath(options.DatabasePath);
        options.CacheDirectory = ExpandHomeRelativePath(options.CacheDirectory);
    }

    private static string ExpandHomeRelativePath(string path)
    {
        if (!path.StartsWith("~/", StringComparison.Ordinal))
        {
            return path;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return string.IsNullOrEmpty(home) ? path : Path.Combine(home, path[2..]);
    }
}
