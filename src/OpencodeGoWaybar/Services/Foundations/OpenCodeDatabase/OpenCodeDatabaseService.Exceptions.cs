using Microsoft.Data.Sqlite;
using OpencodeGoWaybar.Brokers.Storages;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Models.OpenCodeMessages.Exceptions;
using OpencodeGoWaybar.Models.OpenCodeMessages;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;

internal sealed partial class OpenCodeDatabaseService
{
    private async ValueTask<IReadOnlyList<RecentUsageDay>> TryCatchAsync(
        DateTimeOffset cutoff,
        string providerId,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidateDatabasePath();

            IReadOnlyList<OpenCodeUsageDayRow> rows =
                await _databaseBroker.SelectUsageDaysByCutoffAsync(cutoff, providerId, cancellationToken);

            ValidateUsageDays(rows);

            return MapUsageDays(rows);
        }
        catch (SqliteException exception) when (exception.Message.Contains("no such table: message", StringComparison.OrdinalIgnoreCase))
        {
            throw await LogAndReturnAsync(new OpenCodeDatabaseSchemaException(exception));
        }
        catch (SqliteException exception)
        {
            throw await LogAndReturnAsync(new OpenCodeDatabaseUnavailableException(exception));
        }
        catch (OpenCodeDatabaseResponseException exception)
        {
            throw await LogAndReturnAsync(exception);
        }
        catch (OpenCodeDatabaseUnavailableException exception)
        {
            throw await LogAndReturnAsync(exception);
        }
        catch (Exception exception)
        {
            throw await LogAndReturnAsync(new OpenCodeDatabaseServiceException(exception));
        }
    }

    private async ValueTask<DateTimeOffset?> TryCatchLastWriteTimeAsync(CancellationToken cancellationToken)
    {
        try
        {
            DateTimeOffset writeTime = await _databaseBroker.GetLastWriteTimeAsync(cancellationToken);

            // File.GetLastWriteTimeUtc answers with a 1601 sentinel for a file
            // that is not there rather than throwing, so absence has to be read
            // out of the value. Treating the sentinel as a real timestamp made
            // the module think the database had changed and try to read it.
            return writeTime == MissingDatabaseWriteTime ? null : writeTime;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // opencode has not created its database yet.
            return null;
        }
        catch (Exception exception)
        {
            throw await LogAndReturnAsync(new OpenCodeDatabaseServiceException(exception));
        }
    }

    private async ValueTask<Exception> LogAndReturnAsync(Exception exception)
    {
        await _loggingBroker.LogErrorAsync(exception);
        return exception;
    }
}
