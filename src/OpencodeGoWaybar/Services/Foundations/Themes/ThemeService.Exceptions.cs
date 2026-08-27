using OpencodeGoWaybar.Models.Themes;
using OpencodeGoWaybar.Models.Themes.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.Themes;

internal sealed partial class ThemeService
{
    private async ValueTask<ThemePalette?> TryCatchAsync(Func<ValueTask<ThemePalette?>> operation)
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw await LogAndReturnAsync(MapException(exception));
        }
    }

    private static Exception MapException(Exception exception) => exception switch
    {
        IOException or UnauthorizedAccessException => new ThemeUnavailableException(exception),
        _ => new ThemeServiceException(exception),
    };

    private async ValueTask<Exception> LogAndReturnAsync(Exception exception)
    {
        await loggingBroker.LogErrorAsync(exception);

        return exception;
    }
}
