using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.Seam;

/// <summary>
/// The seam tests set a process-wide environment variable to point configuration
/// at a fixture, so they must not run alongside anything else that reads it.
/// </summary>
[CollectionDefinition("ThemeSeam", DisableParallelization = true)]
public sealed class ThemeSeamCollection
{
}
