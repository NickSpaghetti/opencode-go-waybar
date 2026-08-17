using Microsoft.Data.Sqlite;
using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using OpencodeGoWaybar.Brokers.Support.Logging;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;

internal sealed partial class OpenCodeDatabaseService
{
    private async ValueTask<IReadOnlyList<OpenCodeMessage>> TryCatchAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidateDatabasePath();
            var messages = await _databaseBroker.RetrieveMessagesAsync(cutoff, cancellationToken);
            ValidateMessages(messages);
            return messages;
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
            return await _databaseBroker.RetrieveLastWriteTimeAsync(cancellationToken);
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
