using Spectre.Console;
using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Engine.Tournament;
using WorldCup.Reporting;

namespace WorldCup.Cli;

/// <summary>Shared CLI prompt/progress helpers.</summary>
public static class ConsoleHelpers
{
    /// <summary>Prompt the user to choose a team (grouped, searchable).</summary>
    public static Team PickTeam(TournamentData data, string title)
    {
        var choices = data.Teams
            .OrderBy(t => t.Group).ThenBy(t => t.Pot)
            .Select(t => $"{t.Code} — {t.Name} (Grp {t.Group})")
            .ToList();

        string pick = Nav.Show(
            new SelectionPrompt<string>()
                .Title(title)
                .PageSize(16)
                .WrapAround()
                .EnableSearch()
                .SearchPlaceholderText("[grey](type a name or code to filter)[/]")
                .MoreChoicesText("[grey](↑/↓ to scroll · type to search)[/]")
                .AddChoices(choices));

        string code = pick.Split(' ')[0];
        return data.Team(code);
    }

    /// <summary>Prompt the user to choose one of the four teams in a specific group.</summary>
    public static Team PickTeamInGroup(TournamentData data, char group, string title)
    {
        var choices = data.TeamsInGroup(group)
            .Select(t => $"{t.Code} — {t.Name}")
            .ToList();

        string pick = Nav.Show(
            new SelectionPrompt<string>()
                .Title(title)
                .AddChoices(choices));

        return data.Team(pick.Split(' ')[0]);
    }

    /// <summary>
    /// Correct a team's first-choice goalkeeper. The live roster lists keepers but not who actually
    /// starts, so the projected XI can show a backup; this pins the chosen keeper as a preferred
    /// starter in the Current parameters, which both the displayed and the simulated line-up honour.
    /// </summary>
    public static void SetStartingGoalkeeper(Session session, Team team)
    {
        var keepers = team.Squad.Where(pl => pl.Position == Position.GK).ToList();
        if (keepers.Count <= 1)
        {
            Ui.Warning($"{team.Name} has only one goalkeeper in the roster — nothing to change.");
            return;
        }

        var current = LineupProjector
            .Project(team, session.Current.Formation(team), session.Current.IsAvailable, session.Current.PreferredStarters)
            .Xi.FirstOrDefault(pl => pl.Position == Position.GK);

        string pick = Nav.Show(new SelectionPrompt<string>()
            .Title($"Pick {team.Name}'s starting goalkeeper [grey](current: {current?.Name ?? "—"})[/]:")
            .AddChoices(keepers.Select(k => $"{k.Id} — {k.Name}")));
        string id = pick.Split(' ')[0];

        // Exactly one keeper is the preferred starter — clear any other GK for this team first.
        foreach (var k in keepers)
        {
            session.Current.PreferredStarters.Remove(k.Id);
        }

        session.Current.PreferredStarters.Add(id);
        Ui.Success($"{keepers.First(k => k.Id == id).Name} will start in goal for {team.Name} (Current parameters).");
    }

    /// <summary>Run a long task on a background thread while showing a live progress bar with ETA + throughput.</summary>
    public static T RunWithProgress<T>(string description, long total, Func<ProgressCounter, CancellationToken, T> work)
    {
        var counter = new ProgressCounter { Total = total };
        T result = default!;

        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .Start(ctx =>
            {
                var task = ctx.AddTask(description, maxValue: total);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(Nav.Token); // Ctrl+C aborts the run → main menu
                var run = Task.Run(() => work(counter, cts.Token));
                while (!run.IsCompleted)
                {
                    task.Value = Math.Min(counter.Completed, total);
                    Thread.Sleep(60);
                }

                task.Value = total;
                result = run.GetAwaiter().GetResult();
            });

        return result;
    }

    /// <summary>Prompt for an iteration count with sensible presets and validation.</summary>
    public static long PromptIterations(string title, long defaultValue)
    {
        var choice = Nav.Show(
            new SelectionPrompt<string>()
                .Title(title)
                .AddChoices("1,000", "10,000", "100,000", "1,000,000", "Custom…"));

        if (choice != "Custom…")
        {
            return long.Parse(choice.Replace(",", ""));
        }

        return Nav.Show(
            new TextPrompt<long>("Enter N:")
                .DefaultValue(defaultValue)
                .Validate(n => n > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]N must be positive[/]")));
    }

    public static bool ConfirmLongRun(long n)
    {
        // Only confirm for a genuinely heavy custom run — the preset defaults (≤1M) run in a few seconds.
        if (n <= 2_000_000)
        {
            return true;
        }

        return Nav.Confirm($"This will run [bold]{n:N0}[/] simulations. Proceed?");
    }

    public static void OfferExports(string baseName, Action<string> writeJson, Action<string>? writeCsv, Action<string>? writeHtml = null)
    {
        var choices = new List<string>();
        if (writeHtml is not null) choices.Add("HTML (styled page)");
        choices.Add("JSON");
        if (writeCsv is not null) choices.Add("CSV");

        var formats = Nav.Show(
            new MultiSelectionPrompt<string>()
                .Title($"Export results? [grey](space to toggle, enter to confirm; none = skip)[/]")
                .NotRequired()
                .AddChoices(choices));

        if (formats.Count == 0)
        {
            return;
        }

        string dir = OutputFolder.Root;
        Directory.CreateDirectory(dir);
        if (formats.Contains("HTML (styled page)") && writeHtml is not null)
        {
            string path = Path.Combine(dir, baseName + ".html");
            writeHtml(path);
            Ui.Success($"Wrote {path}");
            if (Nav.Confirm("Open it in your browser?", true))
            {
                OpenInBrowser(path);
            }
        }

        if (formats.Contains("JSON"))
        {
            string path = Path.Combine(dir, baseName + ".json");
            writeJson(path);
            Ui.Success($"Wrote {path}");
        }

        if (formats.Contains("CSV") && writeCsv is not null)
        {
            string path = Path.Combine(dir, baseName + ".csv");
            writeCsv(path);
            Ui.Success($"Wrote {path}");
        }
    }

    public static void OpenInBrowser(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Ui.Warning($"Couldn't open the browser ({ex.Message}). Open the file manually: {path}");
        }
    }

    /// <summary>
    /// Hold the finished results on screen until the user is ready to go back, so a long report isn't
    /// instantly scrolled away by the menu (and a stray key can't re-enter a flow). Redirect-safe:
    /// with no interactive console <see cref="Console.ReadLine"/> returns immediately rather than hanging.
    /// </summary>
    public static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup($"[{Ui.Muted}]Press [/][{Ui.Accent}]Enter[/][{Ui.Muted}] to return to the main menu…[/]");
        try
        {
            Console.ReadLine();
        }
        catch
        {
            // Non-interactive / redirected input: don't block.
        }

        AnsiConsole.WriteLine();
    }
}
