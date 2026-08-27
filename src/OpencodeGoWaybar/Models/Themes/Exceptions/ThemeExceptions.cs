namespace OpencodeGoWaybar.Models.Themes.Exceptions;

/// <summary>Indicates that a stylesheet exists but could not be read.</summary>
internal sealed class ThemeUnavailableException(Exception innerException)
    : Exception("The Waybar stylesheet could not be read.", innerException);

/// <summary>Categorizes an unexpected theme-service failure.</summary>
internal sealed class ThemeServiceException(Exception innerException)
    : Exception("The theme service failed.", innerException);
