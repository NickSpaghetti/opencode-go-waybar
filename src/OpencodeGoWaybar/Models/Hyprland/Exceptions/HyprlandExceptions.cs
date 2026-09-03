namespace OpencodeGoWaybar.Models.Hyprland.Exceptions;

/// <summary>Indicates that the Hyprland IPC socket could not be reached.</summary>
internal sealed class HyprlandUnavailableException(Exception innerException)
    : Exception("The Hyprland IPC socket could not be reached.", innerException);

/// <summary>Indicates that Hyprland answered with data this module cannot read.</summary>
internal sealed class HyprlandResponseException(Exception innerException)
    : Exception("Hyprland returned invalid IPC data.", innerException);

/// <summary>Categorizes an unexpected Hyprland lookup failure.</summary>
internal sealed class HyprlandServiceException(Exception innerException)
    : Exception("The Hyprland service failed.", innerException);
