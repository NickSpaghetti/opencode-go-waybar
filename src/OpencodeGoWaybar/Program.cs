using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpencodeGoWaybar.Brokers.Apis.Usage;
using OpencodeGoWaybar.Brokers.Configurations;
using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using OpencodeGoWaybar.Brokers.Storages.Cache;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Brokers.Support.Processes;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Configurations;
using OpencodeGoWaybar.Services.Foundations.Cache;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.Services.Foundations.Usage;
using OpencodeGoWaybar.Services.Exposers.Waybar;
using OpencodeGoWaybar.Services.Processings.Usage;

namespace OpencodeGoWaybar;

internal static class Program
{
    internal static async Task<int> Main()
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var cancellationToken = cancellationSource.Token;

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
            .AddTransient<IProcessBroker, ProcessBroker>()
            .AddTransient<IProcessService>(services =>
                new ProcessService(
                    services.GetRequiredService<IProcessBroker>(),
                    ReadProcessPresentOverride(),
                    services.GetRequiredService<ILoggingBroker>()))
            .AddTransient<IWaybarExposer, WaybarExposer>()
            .BuildServiceProvider();

        var processService = serviceProvider.GetRequiredService<IProcessService>();
        var waybarExposer = serviceProvider.GetRequiredService<IWaybarExposer>();
        var processIsActive = await processService.IsInteractiveOpenCodeRunningAsync(cancellationToken);

        if (!processIsActive)
        {
            Console.WriteLine(await waybarExposer.ExposeAsync(false, null, null, cancellationToken));
            return 0;
        }

        try
        {
            var snapshot = await serviceProvider.GetRequiredService<IUsageProcessingService>()
                .RetrieveUsageAsync(DateTimeOffset.UtcNow, cancellationToken);
            Console.WriteLine(await waybarExposer.ExposeAsync(true, snapshot, null, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(await waybarExposer.ExposeAsync(
                true,
                null,
                new TimeoutException("The usage refresh exceeded the ten-second limit."),
                CancellationToken.None));
        }
        catch (Exception exception)
        {
            Console.WriteLine(await waybarExposer.ExposeAsync(true, null, exception, cancellationToken));
        }

        return 0;
    }

    private static bool? ReadProcessPresentOverride()
    {
        var value = Environment.GetEnvironmentVariable("OPENCODE_GO_PROCESS_PRESENT");
        return bool.TryParse(value, out var processIsPresent) ? processIsPresent : null;
    }
}
