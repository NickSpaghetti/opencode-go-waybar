using OpencodeGoWaybar.Brokers.Themes;
using Xunit;

namespace OpencodeGoWaybar.IntegrationTests;

/// <summary>
/// The broker's whole job is the real filesystem, so it is exercised against one.
/// It holds no policy now: a missing file throws for the service to localise, and
/// every raw watcher event is passed straight through uncollapsed.
/// </summary>
[Trait("Tier", "Integration")]
public sealed class WaybarThemeBrokerIntegrationTests : IDisposable
{
    private readonly string directoryPath = Path.Combine(
        Path.GetTempPath(),
        $"opencode-go-theme-{Guid.NewGuid():N}");

    public WaybarThemeBrokerIntegrationTests() => Directory.CreateDirectory(this.directoryPath);

    [Fact]
    public async Task ShouldReadAStyleSheetFromDiskAsync()
    {
        // given
        var broker = new WaybarThemeBroker();
        var path = Path.Combine(this.directoryPath, "style.css");
        await File.WriteAllTextAsync(path, "@define-color base #111115;");

        // when
        var text = await broker.ReadTextAsync(path, CancellationToken.None);

        // then
        Assert.Equal("@define-color base #111115;", text);
    }

    [Fact]
    public async Task ShouldThrowTheNativeExceptionWhenTheStyleSheetIsAbsentAsync()
    {
        // given
        var broker = new WaybarThemeBroker();

        // when and then — the service localises this, the broker does not decide
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            broker.ReadTextAsync(
                Path.Combine(this.directoryPath, "absent.css"),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public void ShouldAnswerWhetherTheStyleSheetDirectoryExists()
    {
        // given
        var broker = new WaybarThemeBroker();

        // then
        Assert.True(broker.StyleSheetDirectoryExists(this.directoryPath));
        Assert.False(broker.StyleSheetDirectoryExists(
            Path.Combine(this.directoryPath, "no-such-directory")));
    }

    [Fact]
    public async Task ShouldRaiseRawEventsForAWriteAsync()
    {
        // given
        var broker = new WaybarThemeBroker();
        var path = Path.Combine(this.directoryPath, "style.css");
        await File.WriteAllTextAsync(path, "@define-color base #000000;");

        var changes = 0;
        using IDisposable subscription = broker.WatchStyleSheets(
            this.directoryPath,
            () => Interlocked.Increment(ref changes));

        await Task.Delay(300);

        // when
        await File.WriteAllTextAsync(path, "@define-color base #111111;");

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);

        while (Volatile.Read(ref changes) == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        // then at least one event arrived; how many is the filesystem's business,
        // and collapsing them is the service's
        Assert.True(Volatile.Read(ref changes) > 0);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.directoryPath))
        {
            Directory.Delete(this.directoryPath, recursive: true);
        }
    }
}
