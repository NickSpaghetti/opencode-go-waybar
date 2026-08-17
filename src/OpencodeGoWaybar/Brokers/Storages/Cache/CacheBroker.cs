using System.Text.Json;
using System.Text.Json.Serialization;
using OpencodeGoWaybar.Models.Processings.Usage;

namespace OpencodeGoWaybar.Brokers.Storages.Cache;

internal sealed class CacheBroker(string cachePath) : ICacheBroker
{
    private string LockPath => cachePath + ".lock";

    public async ValueTask<UsageCacheState?> RetrieveStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(cachePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(cachePath, cancellationToken);
        return JsonSerializer.Deserialize(json, UsageCacheJsonContext.Default.UsageCacheState);
    }

    public async ValueTask StoreStateAsync(UsageCacheState state, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";
        var json = JsonSerializer.Serialize(state, UsageCacheJsonContext.Default.UsageCacheState);
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, cachePath, overwrite: true);
    }

    public ValueTask<bool> TryAcquireLockAsync(TimeSpan staleAfter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.GetDirectoryName(LockPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            using var lockFile = new FileStream(LockPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(lockFile);
            writer.Write($"{Environment.ProcessId}\n{DateTimeOffset.UtcNow:O}");
            return ValueTask.FromResult(true);
        }
        catch (IOException) when (File.Exists(LockPath))
        {
            var lockTime = new DateTimeOffset(File.GetLastWriteTimeUtc(LockPath), TimeSpan.Zero);
            if (DateTimeOffset.UtcNow - lockTime > staleAfter)
            {
                File.Delete(LockPath);
            }

            return ValueTask.FromResult(false);
        }
    }

    public ValueTask ReleaseLockAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(LockPath))
        {
            File.Delete(LockPath);
        }

        return ValueTask.CompletedTask;
    }
}

[JsonSerializable(typeof(UsageCacheState))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class UsageCacheJsonContext : JsonSerializerContext
{
}
