using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Brokers.Apis.Usage;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using OpencodeGoWaybar.Brokers.Storages.Cache;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Configurations;
using OpencodeGoWaybar.Services.Foundations.Cache;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Services.Foundations.Usage;
using OpencodeGoWaybar.Services.Processings.Usage;

namespace OpencodeGoWaybar;

internal static class Program
{
    internal static int Main()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLogging(loggingBuilder => loggingBuilder.AddSimpleConsole())
            .AddSingleton<ILoggingBroker, LoggingBroker>()
            .AddSingleton<IConfigurationBroker, ConfigurationBroker>()
            .AddSingleton<IValidateOptions<OpenCodeGoOptions>, OpenCodeGoOptionsValidator>()
            .AddTransient<IConfigurationService, ConfigurationService>()
            .AddSingleton<IOptions<OpenCodeGoOptions>>(services =>
                services.GetRequiredService<IConfigurationService>().RetrieveOptions())
            .AddSingleton<IOptions<OpenCodeGoSecrets>>(services =>
                services.GetRequiredService<IConfigurationService>().RetrieveSecrets())
            .AddSingleton<HttpClient>()
            .AddTransient<IUsageBroker>(services =>
                new UsageBroker(
                    services.GetRequiredService<HttpClient>(),
                    services.GetRequiredService<IOptions<OpenCodeGoOptions>>().Value.UsageEndpoint))
            .AddTransient<IOpenCodeDatabaseBroker>(services =>
                new OpenCodeDatabaseBroker(
                    services.GetRequiredService<IOptions<OpenCodeGoOptions>>().Value.DatabasePath))
            .AddTransient<ICacheBroker>(services =>
                new CacheBroker(
                    services.GetRequiredService<IOptions<OpenCodeGoOptions>>().Value.CachePath))
            .AddTransient<ICacheService, CacheService>()
            .AddTransient<IUsageService, UsageService>()
            .AddTransient<IOpenCodeDatabaseService, OpenCodeDatabaseService>()
            .AddTransient<IUsageProcessingService, UsageProcessingService>()
            .BuildServiceProvider();

        Console.WriteLine("opencode-go-waybar 0.0.0");
        return 0;
    }
}
