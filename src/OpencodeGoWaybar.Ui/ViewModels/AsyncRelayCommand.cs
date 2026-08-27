using System.Windows.Input;

namespace OpencodeGoWaybar.Ui.ViewModels;

/// <summary>
/// The toolbar's Refresh button. Hand-rolled rather than pulled from an MVVM
/// package: this is the only command in the application, and it exists mainly to
/// stop a second refresh starting while the first is still in flight.
/// </summary>
public sealed class AsyncRelayCommand(Func<CancellationToken, Task> execute) : ICommand
{
    private bool isRunning;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !this.isRunning;

    /// <summary>
    /// Awaitable, unlike ICommand.Execute, so a test can assert on the outcome
    /// instead of polling for it.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (this.isRunning)
        {
            return;
        }

        this.isRunning = true;
        RaiseCanExecuteChanged();

        try
        {
            await execute(cancellationToken);
        }
        finally
        {
            this.isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    void ICommand.Execute(object? parameter) => _ = ExecuteAsync();

    private void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
