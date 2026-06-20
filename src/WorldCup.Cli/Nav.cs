using Spectre.Console;

namespace WorldCup.Cli;

/// <summary>
/// Global navigation: pressing <c>Ctrl+C</c> anywhere aborts the current activity (a prompt or a running
/// simulation) and returns to the main menu, rather than quitting the app — the app only exits via the
/// Exit menu item. Prompts and long runs are routed through here so they observe the cancellation.
/// </summary>
public static class Nav
{
    private static CancellationTokenSource _cts = new();

    /// <summary>The current abort token — cancelled when the user presses Ctrl+C, reset on return to the menu.</summary>
    public static CancellationToken Token => _cts.Token;

    /// <summary>Install the Ctrl+C handler. Call once at startup.</summary>
    public static void Install()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;   // never terminate the process — we want to drop back to the menu
            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Raced with a reset; harmless.
            }
        };
    }

    /// <summary>Recreate a fresh token once we're safely back at the main menu (no-op if nothing was cancelled).</summary>
    public static void Reset()
    {
        if (!_cts.IsCancellationRequested)
        {
            return;
        }

        var old = _cts;
        _cts = new CancellationTokenSource();
        old.Dispose();
    }

    /// <summary>Show a Spectre prompt that can be aborted to the main menu with Ctrl+C.</summary>
    public static T Show<T>(IPrompt<T> prompt) =>
        prompt.ShowAsync(AnsiConsole.Console, _cts.Token).GetAwaiter().GetResult();

    /// <summary>A yes/no confirmation that can be aborted to the main menu with Ctrl+C.</summary>
    public static bool Confirm(string question, bool defaultValue = true) =>
        Show(new ConfirmationPrompt(question) { DefaultValue = defaultValue });

    /// <summary>A text prompt (with a default) that can be aborted to the main menu with Ctrl+C.</summary>
    public static T Ask<T>(string question, T defaultValue) =>
        Show(new TextPrompt<T>(question).DefaultValue(defaultValue));
}
