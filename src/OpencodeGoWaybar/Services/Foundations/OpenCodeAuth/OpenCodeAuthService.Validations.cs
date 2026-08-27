using OpencodeGoWaybar.Models.OpenCodeAuths.Exceptions;

namespace OpencodeGoWaybar.Services.Foundations.OpenCodeAuth;

internal sealed partial class OpenCodeAuthService
{
    private void ValidateAuthPath()
    {
        if (string.IsNullOrWhiteSpace(options.AuthPath))
        {
            throw new OpenCodeAuthUnavailableException(
                new ArgumentException("AuthPath must not be empty.", nameof(options)));
        }
    }
}
