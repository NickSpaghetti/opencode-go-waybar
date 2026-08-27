using Avalonia.Controls;
using Avalonia.Input;
using OpencodeGoWaybar.Ui.ViewModels;

namespace OpencodeGoWaybar.Ui.Views;

/// <summary>
/// The frameless radial card. No decorations, transparent window, and its own
/// rounded surface with a drawn shadow.
///
/// Dismissal is Escape or losing focus. The design docs paired that with a
/// stay-focused compositor rule "so Deactivated closes it" — which does the
/// opposite: a window that never loses focus never deactivates, and the popup
/// would never close. No such rule is shipped.
/// </summary>
public partial class RingPopup : Window
{
    /// <summary>
    /// A popup only dismisses on focus loss once it has actually held focus.
    /// Closing on the first Deactivated is wrong: a compositor that opens the
    /// window without focusing it would shut it again immediately, and clicking
    /// the bar would look like it did nothing. This is the problem the design
    /// docs tried to solve with a stay-focused rule, which instead stops the
    /// window ever closing.
    /// </summary>
    private bool hasHeldFocus;

    public RingPopup()
    {
        InitializeComponent();

        Opened += OnOpened;
        Activated += (_, _) => this.hasHeldFocus = true;
        Deactivated += OnDeactivated;
        KeyDown += OnKeyDown;
    }

    private async void OnOpened(object? sender, EventArgs eventArgs)
    {
        if (DataContext is UsageWindowViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }

    private void OnDeactivated(object? sender, EventArgs eventArgs)
    {
        if (this.hasHeldFocus && !KeepOpen)
        {
            Close();
        }
    }

    /// <summary>
    /// Holds the popup open through focus loss. Set OPENCODE_GO_UI_KEEP_OPEN=1
    /// when inspecting the window during development, or on any desktop that does
    /// not hand a freshly launched window the focus it needs to stay up — macOS
    /// launching an unbundled binary from a terminal being the case that found it.
    /// </summary>
    private static bool KeepOpen =>
        Environment.GetEnvironmentVariable("OPENCODE_GO_UI_KEEP_OPEN") == "1";

    private void OnKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key == Key.Escape)
        {
            Close();
        }
    }

    private void OnHeaderPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(eventArgs);
        }
    }
}
