using OpencodeGoWaybar.Brokers.Storages.OpenCode;
using OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;

internal sealed partial class OpenCodeDatabaseService
{
    private void ValidateDatabasePath()
    {
        if (string.IsNullOrWhiteSpace(_options.Value.DatabasePath))
        {
            throw new OpenCodeDatabaseUnavailableException(
                new ArgumentException("DatabasePath must not be empty.", nameof(_options)));
        }
    }

    private static void ValidateMessages(IReadOnlyList<OpenCodeMessage>? messages)
    {
        if (messages is null || messages.Any(message => string.IsNullOrWhiteSpace(message.Data)))
        {
            throw new OpenCodeDatabaseResponseException(
                new InvalidDataException("The database broker returned invalid usage data."));
        }
    }
}
