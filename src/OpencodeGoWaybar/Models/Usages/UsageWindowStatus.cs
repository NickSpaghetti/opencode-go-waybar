namespace OpencodeGoWaybar.Models.Usages;

/// <summary>
/// How one usage window is doing. A business fact about the account, decided from
/// the API's own status and the configured percent thresholds — not a presentation
/// concern, which is why it lives here and not under Exposures.
///
/// A window can be spent without being refused, and refused without being spent.
/// </summary>
public enum UsageWindowStatus
{
    /// <summary>No percentage was reported, so nothing can be said.</summary>
    Unknown,

    /// <summary>Healthy and below the caution threshold.</summary>
    Ok,

    /// <summary>At or above the caution threshold, still accepting requests.</summary>
    Caution,

    /// <summary>The API stopped calling this window healthy, whatever the percentage.</summary>
    Throttled,

    /// <summary>At or above the danger threshold.</summary>
    Spent,

    /// <summary>The API is actively refusing requests for this window.</summary>
    RateLimited,
}
