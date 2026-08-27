using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using OpencodeGoWaybar.Ui.ViewModels;
using Xunit;

namespace OpencodeGoWaybar.Ui.UnitTests.Views;

/// <summary>
/// AvaloniaUseCompiledBindingsByDefault makes the compiler check every binding
/// path against its x:DataType — except the DataGrid columns, which must use
/// ReflectionBinding because their paths resolve against the row type rather than
/// the window's. Those are the bindings that can rot unnoticed after a property
/// rename, so they are checked here instead.
///
/// The markup is located from this file's own compile-time path rather than
/// copied into the output: the views are not a runtime dependency of the tests,
/// and shipping them there just to read them invites them going stale.
/// </summary>
public sealed class ReflectionBindingTests
{
    [Fact]
    public void EveryReflectionBindingShouldNameARealPropertyOnTheRow()
    {
        // given every view shipped with the application
        string[] viewPaths = Directory.GetFiles(ViewsDirectory(), "*.axaml");

        Assert.NotEmpty(viewPaths);

        var rowProperties = typeof(UsageWindowRow)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        var checkedBindings = 0;

        // when
        foreach (var viewPath in viewPaths)
        {
            var markup = File.ReadAllText(viewPath);

            foreach (Match match in Regex.Matches(markup, @"\{ReflectionBinding\s+([A-Za-z0-9_]+)"))
            {
                // then
                Assert.Contains(match.Groups[1].Value, rowProperties);
                checkedBindings++;
            }
        }

        // and the guard is guarding something: the three DataGrid columns
        Assert.True(checkedBindings >= 3, $"expected the DataGrid columns, found {checkedBindings}");
    }

    private static string ViewsDirectory([CallerFilePath] string testFilePath = "")
    {
        // tests/OpencodeGoWaybar.Ui.UnitTests/Views/<this file>
        var projectDirectory =
            Path.GetDirectoryName(Path.GetDirectoryName(testFilePath)!)!;

        var repositoryRoot = Path.GetFullPath(Path.Combine(projectDirectory, "..", ".."));

        return Path.Combine(repositoryRoot, "src", "OpencodeGoWaybar.Ui", "Views");
    }
}
