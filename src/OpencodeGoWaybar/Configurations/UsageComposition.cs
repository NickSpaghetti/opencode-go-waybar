using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OpencodeGoWaybar.Brokers.Caches;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Credentials;
using OpencodeGoWaybar.Brokers.DateTimes;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Processes;
using OpencodeGoWaybar.Brokers.Storages;
using OpencodeGoWaybar.Brokers.Themes;
using OpencodeGoWaybar.Brokers.Usages;
using OpencodeGoWaybar.Exposers.Themes;
using OpencodeGoWaybar.Exposers.Usages;
using OpencodeGoWaybar.Exposers.Waybar;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Aggregations.Usage;
using OpencodeGoWaybar.Services.Foundations.UsageHistoryCache;
using OpencodeGoWaybar.Services.Foundations.UsageWindowCache;
using OpencodeGoWaybar.Services.Foundations.Configurations;
using OpencodeGoWaybar.Services.Foundations.OpenCodeAuth;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.Services.Foundations.Secrets;
using OpencodeGoWaybar.Services.Foundations.Themes;
using OpencodeGoWaybar.Services.Foundations.Usage;
using OpencodeGoWaybar.Services.Orchestrations.Credentials;
using OpencodeGoWaybar.Services.Orchestrations.UsageHistory;
using OpencodeGoWaybar.Services.Orchestrations.UsageWindows;

namespace OpencodeGoWaybar.Configurations;

/// <summary>
/// The composition model for the usage flow (The Standard 0.1.2.0.2): the single
/// place every dependency in this assembly is wired together.
///
/// Purposing: two heads run this flow — the Waybar module in this assembly, and
/// the Avalonia window in OpencodeGoWaybar.Ui. Both need the identical graph, so
/// it lives here rather than being duplicated in each entry point.
///
/// Outcome: a provider the caller owns and must dispose.
///
/// Side effects: none at build time. Options binding and secret retrieval are
/// registered lazily, so nothing reads configuration or credentials until a
/// service is first resolved.
///
/// This stays one visible chain rather than a set of Add* extension methods: a
/// reader can see the whole graph without chasing references (The Standard
/// 0.2.0.0.2.0, No Magic).
/// </summary>
public static class UsageComposition
{
    public static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSimpleConsole();

                // Waybar parses the module process's stdout as JSON, one payload
                // per line. Diagnostics must never share that stream or they are
                // read as a malformed payload, so every level goes to stderr.
                loggingBuilder.Services.Configure<ConsoleLoggerOptions>(consoleOptions =>
                    consoleOptions.LogToStandardErrorThreshold = LogLevel.Trace);
            })
            .AddSingleton<ILoggingBroker, LoggingBroker>()
            .AddSingleton<IDateTimeBroker, DateTimeBroker>()
            .AddSingleton<IConfigurationBroker, ConfigurationBroker>()
            .AddTransient<IConfigurationService, ConfigurationService>()
            .AddSingleton<OpenCodeGoOptions>(services =>
                services.GetRequiredService<IConfigurationService>().RetrieveOptions())
            .AddTransient<IOpenCodeAuthBroker, OpenCodeAuthBroker>()
            .AddTransient<IOpenCodeAuthService, OpenCodeAuthService>()
            .AddTransient<ICredentialsOrchestrationService, CredentialsOrchestrationService>()
            .AddTransient<ISecretsService, SecretsService>()
            .AddSingleton<OpenCodeGoSecrets>(services =>
                services.GetRequiredService<ICredentialsOrchestrationService>().RetrieveSecrets())
            .AddSingleton<HttpClient>()
            .AddTransient<IUsageBroker, UsageBroker>()
            .AddTransient<IOpenCodeDatabaseBroker, OpenCodeDatabaseBroker>()
            // Two cache brokers over one directory, each owning its own file
            // (§1.2.5 multiple targets). One writer per file, so no lock.
            .AddTransient<IUsageWindowCacheBroker, UsageWindowCacheBroker>()
            .AddTransient<IUsageHistoryCacheBroker, UsageHistoryCacheBroker>()
            .AddTransient<IUsageWindowCacheService, UsageWindowCacheService>()
            .AddTransient<IUsageHistoryCacheService, UsageHistoryCacheService>()
            .AddTransient<IUsageService, UsageService>()
            .AddTransient<IOpenCodeDatabaseService, OpenCodeDatabaseService>()
            .AddTransient<IUsageWindowsOrchestrationService, UsageWindowsOrchestrationService>()
            .AddTransient<IUsageHistoryOrchestrationService, UsageHistoryOrchestrationService>()
            .AddTransient<IProcessBroker, ProcessBroker>()
            .AddTransient<IProcessService, ProcessService>()
            .AddTransient<IUsageAggregationService, UsageAggregationService>()
            .AddTransient<IWaybarExposer, WaybarExposer>()
            .AddTransient<IUsageExposer, UsageExposer>()
            // The theme broker owns filesystem watchers and is disposable, so it is
            // held once and torn down with the provider; the exposer is a singleton
            // too so a consumer cannot accumulate duplicate subscriptions.
            .AddSingleton<IWaybarThemeBroker, WaybarThemeBroker>()
            .AddSingleton<IThemeService, ThemeService>()
            .AddSingleton<IThemeExposer, ThemeExposer>()
            .BuildServiceProvider();
    }
}
