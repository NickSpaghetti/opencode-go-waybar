namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// The error the usage API reported, reduced to the two fields worth showing.
/// Only these reach the Waybar tooltip — never a raw body or an exception
/// message, so nothing the module holds can be rendered onto the bar.
/// </summary>
internal sealed record UsageApiError(string? Type, string? Message);
