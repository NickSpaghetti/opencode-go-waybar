using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace OpencodeGoWaybar.Ui.Converters;

/// <summary>
/// Resolves a palette key to a brush. Kept as a plain function over a resource
/// dictionary rather than reaching for Application.Current so the converters that
/// use it can be exercised without a running application — the alternative is a
/// headless host for what is a dictionary lookup.
/// </summary>
internal static class PaletteLookup
{
    internal static IBrush? ResolveBrush(
        IResourceDictionary? resources,
        ThemeVariant? themeVariant,
        object? key)
    {
        if (resources is null || key is null)
        {
            return null;
        }

        return resources.TryGetResource(key, themeVariant, out var resource) && resource is IBrush brush
            ? brush
            : null;
    }
}
