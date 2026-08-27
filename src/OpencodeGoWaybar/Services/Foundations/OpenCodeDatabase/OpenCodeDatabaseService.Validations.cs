using OpencodeGoWaybar.Brokers.Storages;
using OpencodeGoWaybar.Models.OpenCodeMessages.Exceptions;
using OpencodeGoWaybar.Models.OpenCodeMessages;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeDatabase;

internal sealed partial class OpenCodeDatabaseService
{
    private void ValidateDatabasePath()
    {
        if (string.IsNullOrWhiteSpace(_options.DatabasePath))
        {
            throw new OpenCodeDatabaseUnavailableException(
                new ArgumentException("DatabasePath must not be empty.", nameof(_options)));
        }
    }

    private static void ValidateUsageDays(IReadOnlyList<OpenCodeUsageDayRow>? usageDays)
    {
        if (usageDays is null || usageDays.Any(day => string.IsNullOrWhiteSpace(day.Date)))
        {
            throw new OpenCodeDatabaseResponseException(
                new InvalidDataException("The database returned invalid usage data."));
        }
    }
}
