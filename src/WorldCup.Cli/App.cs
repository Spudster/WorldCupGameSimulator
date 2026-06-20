using Spectre.Console;
using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Stats;
using WorldCup.Engine.Tournament;
using WorldCup.Reporting;

namespace WorldCup.Cli;

/// <summary>The interactive menu-driven application. Holds no business logic of its own.</summary>
public sealed partial class App
{
    private readonly Session _session;
    private readonly Random _seedSource = new();

    public App(Session session)
    {
        _session = session;
    }

    /// <summary>A fresh random seed so each single playthrough differs (Monte Carlo runs keep the
    /// reproducible parameter seed). Shown in the run config so a result can still be reproduced.</summary>
    private ulong NextRunSeed() => unchecked((ulong)_seedSource.NextInt64() * 0x9E3779B97F4A7C15UL + 1);

    public void Run()
    {
        Nav.Install(); // Ctrl+C anywhere → back to the main menu (app exits only via the Exit item)
        AnsiConsole.Clear();
        Ui.Banner();
        Ui.Info(_session.ProviderDiagnostics);
        Ui.Info(_session.LiveDiagnostics);
        if (!_session.LiveConfigured)
        {
            ApiKeyNotice(keyPresent: false);
        }
        else if (_session.LiveKeyUnauthorized)
        {
            ApiKeyNotice(keyPresent: true);
        }

        var data = _session.Data;
        Ui.Info($"{data.Teams.Count} teams · {data.Groups.Count} groups · {_session.PlayedResults.Count} real results loaded.");
        if (data.Teams.Any(t => t.IsSyntheticSquad))
        {
            Ui.Warning("Player squads are synthetic (deterministically generated from team strength).");
        }

        AnsiConsole.MarkupLine($"[{Ui.Good}]📂 All saved files go to:[/] [bold]{Markup.Escape(OutputFolder.Root)}[/]");
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Navigate with ↑/↓ then Enter. Press [bold]Ctrl+C[/] anytime to jump back to this menu.[/]");

        while (true)
        {
            Nav.Reset(); // fresh abort token each time we land back on the menu
            Ui.Blank();
            AnsiConsole.MarkupLine(SessionStatus());

            string choice;
            try
            {
                choice = Nav.Show(BuildMainMenu());
            }
            catch (OperationCanceledException)
            {
                continue; // Ctrl+C at the menu just re-draws it
            }

            try
            {
                int sel = int.TryParse(choice.Split('.')[0], out var parsed) ? parsed : 0;
                switch (sel)
                {
                    case 1: TeamVsTeam(); break;
                    case 2: ScheduledMatch(); break;
                    case 3: RunAllScheduledGames(); break;
                    case 4: TournamentScenario(currentState: false); break;
                    case 5: TournamentScenario(currentState: true); break;
                    case 6: GroupPath(); break;
                    case 7: RoadToGlory(); break;
                    case 8: OddsBoard(); break;
                    case 9: ModelAccuracy(); break;
                    case 10: CompareScenarios(); break;
                    case 11: BracketChallenge(); break;
                    case 12: LiveDashboard(); break;
                    case 13: RunUntilWin(); break;
                    case 14: LoadSavedTournament(); break;
                    case 15: new ParametersMenu(_session).Run(); break;
                    case 16:
                        Ui.Goodbye();
                        return;
                }
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine($"[{Ui.Muted}]↩ Back to the main menu (Ctrl+C).[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red3]Something went wrong:[/] {Markup.Escape(ex.Message)}");
                ConsoleHelpers.Pause();
            }
        }
    }

    private static SelectionPrompt<string> BuildMainMenu() =>
        new SelectionPrompt<string>()
            .Title($"[bold {Ui.Accent}]Main menu[/] [{Ui.Muted}]— pick a scenario[/]")
            .PageSize(18)
            .WrapAround()
            .HighlightStyle(new Style(foreground: Ui.GoldColor, decoration: Decoration.Bold))
            .AddChoices(
                "1.  ⚽ Team vs Team",
                "2.  📅 Run current scheduled match",
                "3.  🗓 Run scheduled games (today, a chosen day, or all remaining)",
                "4.  🏆 Simulate full tournament — official groups (from the start)",
                "5.  ♻ Simulate full tournament — current state (continue from now)",
                "6.  🧭 Group path to victory & defeat",
                "7.  🛣 Road to glory — a team's tournament campaign",
                "8.  💰 Tournament odds board — title, top scorer, golden glove",
                "9.  🎯 Model accuracy — backtest predictions vs real results",
                "10. ⚖ Compare two scenarios (e.g. with vs without a player)",
                "11. 🃏 Bracket challenge — make your picks and grade them",
                "12. 📡 Live matchday dashboard — today's games + qualification",
                "13. 🔁 Run until a team wins (cup or match)",
                "14. 📂 Load a saved tournament (re-open a snapshot)",
                "15. ⚙ Parameters",
                "16. 🚪 Exit");

    /// <summary>On startup, tell the user when live data is unavailable — either no API key was found, or
    /// the configured key was rejected — and exactly how to get and install a free football-data.org key.
    /// The app still runs fully on the bundled 2026 data; only the live auto-update is affected.</summary>
    private static void ApiKeyNotice(bool keyPresent)
    {
        string headline = keyPresent
            ? "[bold red3]⚠  API key doesn't work[/] — football-data.org rejected it (invalid or expired)."
            : "[bold gold1]⚠  API key not detected[/] — live results, fixtures and real squads are off.";

        var body = new Markup(
            headline + "\n\n" +
            "[grey]The app still runs fully on the bundled 2026 data — only the live auto-update is disabled.[/]\n\n" +
            "[bold]To enable live data (it's free):[/]\n" +
            "  [deepskyblue1]1.[/] Get a free key at  [underline deepskyblue1]https://www.football-data.org/client/register[/]\n" +
            "  [deepskyblue1]2.[/] Provide it one of two ways:\n" +
            $"        • set the  [bold]{Markup.Escape(Session.ApiKeyEnvVar)}[/]  environment variable, or\n" +
            "        • create a  [bold]config.local.json[/]  file next to the app containing:\n" +
            "            [grey]{ \"footballDataApiKey\": \"YOUR_KEY_HERE\" }[/]\n" +
            "  [deepskyblue1]3.[/] Restart the app.");

        AnsiConsole.Write(new Panel(body)
        {
            Header = new PanelHeader(" Live data "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(keyPresent ? Color.Red3 : Color.Gold1),
            Padding = new Padding(2, 1, 2, 1),
        });
    }

    /// <summary>A compact one-line snapshot of the session state, shown above the main menu.</summary>
    private string SessionStatus()
    {
        string paramSet = _session.ParametersEdited ? "Current (edited)" : "Starting";
        string paramCol = _session.ParametersEdited ? Ui.Warn : Ui.Good;
        string playoff = _session.IncludeThirdPlacePlayoff ? $"[{Ui.Good}]on[/]" : $"[{Ui.Muted}]off[/]";
        string data = _session.LiveConfigured ? $"[{Ui.Accent}]live[/]" : $"[{Ui.Muted}]bundled[/]";
        return $"[{Ui.Muted}]session ▸[/] params [{paramCol}]{paramSet}[/]   " +
               $"[{Ui.Muted}]▸[/] 3rd-place {playoff}   " +
               $"[{Ui.Muted}]▸[/] data {data}";
    }

    // --- 1. Team vs Team ---

    private void TeamVsTeam()
    {
        var data = _session.Data;
        var home = ConsoleHelpers.PickTeam(data, "Pick the [bold]first[/] team:");
        var away = ConsoleHelpers.PickTeam(data, "Pick the [bold]second[/] team:");
        if (home.Code == away.Code)
        {
            Ui.Warning("A team cannot play itself.");
            return;
        }

        var mode = Nav.Show(new SelectionPrompt<string>()
            .Title($"[bold]{home.Name}[/] vs [bold]{away.Name}[/] — how?")
            .AddChoices(
                "Monte Carlo — most probable score + average stats (N runs)",
                "Single detailed match (one full box score)"));

        var p = PromptParameterSet();
        AnsiConsole.Clear();
        Ui.Header($"{home.Name} vs {away.Name}");

        if (mode.StartsWith("Single"))
        {
            bool log = Nav.Confirm("Show minute-by-minute event log?", false);
            MatchResult result;
            do
            {
                ulong seed = NextRunSeed();
                var rng = new Xoshiro256(seed);
                Ui.RunConfig($"{home.Name} vs {away.Name}", p.Label, seed, "Detailed", showPredictedOn: false);
                result = MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rng, neutralVenue: true);
                MatchReportFormatter.PrintDetailed(result, log);

                if (Nav.Confirm("📻 Hear the play-by-play commentary?", false))
                {
                    MatchReportFormatter.PrintCommentary(CommentaryGenerator.Generate(result));
                }
            }
            while (Nav.Confirm("Get another match?", false));

            // Downloading the HTML also writes a sibling <name>_commentary.txt transcript.
            ConsoleHelpers.OfferExports($"match_{home.Code}_{away.Code}", path => Exporters.ToJson(result, path), null,
                path => HtmlExporter.MatchResultToHtml(result, path));
        }
        else
        {
            long n = ConsoleHelpers.PromptIterations("How many simulations to aggregate?", 1_000_000);
            if (!ConsoleHelpers.ConfirmLongRun(n)) return;
            Ui.RunConfig($"{home.Name} vs {away.Name}", p.Label, p.Global.Seed, "Detailed (aggregated)", n);
            var report = ConsoleHelpers.RunWithProgress("Simulating matches", n,
                (counter, ct) => MonteCarloMatchRunner.RunAggregate(home, away, p, n, Stage.Group, neutralVenue: true, counter, ct));
            MatchReportFormatter.PrintAggregate(report);
            if (Nav.Confirm("View example matches?", true))
            {
                ShowExampleMatches(home, away, p, neutral: true, report);
            }

            ConsoleHelpers.OfferExports($"mc_{home.Code}_{away.Code}",
                path => Exporters.ToJson(report, path), path => Exporters.MatchAggregateToCsv(report, path),
                path => HtmlExporter.MatchAggregateToHtml(report, path));
        }

        ConsoleHelpers.Pause();
    }

    /// <summary>
    /// Live in-play forecast: updates the win/draw/loss probabilities from the current score and the
    /// minutes remaining (expected goals scale down as the clock runs). Auto-fetches the live score
    /// when the data feed has it; otherwise the score/minute are entered by hand. Loop to refresh.
    /// </summary>
    private void InPlayPrediction(Team home, Team away, SimulationParameters p, bool neutral, DateTime kickoffUtc)
    {
        var (lh, la) = MatchModel.ExpectedGoals(p.EffectiveStrength(home), p.EffectiveStrength(away), p.Global, neutral);
        var (preH, preD, preA) = MatchModel.InPlayOutcome(lh, la, p.Global.DrawCoupling, 0, 0, 0);

        var liveScore = _session.TryGetLiveScore(home.Code, away.Code, refresh: true);
        int hg = liveScore?.Home ?? 0;
        int ag = liveScore?.Away ?? 0;
        int minute = (int)Math.Clamp((DateTime.UtcNow - kickoffUtc).TotalMinutes, 0, 120);
        if (liveScore is not null)
        {
            Ui.Info($"Live score from the feed: {home.Name} {hg}-{ag} {away.Name} (~{minute}′). Adjust if needed.");
        }
        else if (_session.LiveConfigured)
        {
            Ui.Warning("No live score from the feed (needs the match to be IN_PLAY) — enter the current score by hand.");
        }

        do
        {
            hg = PromptInt($"{home.Name} goals so far", hg);
            ag = PromptInt($"{away.Name} goals so far", ag);
            minute = PromptInt("Minute now (0–90+)", minute);

            var (pH, pD, pA) = MatchModel.InPlayOutcome(lh, la, p.Global.DrawCoupling, hg, ag, minute);
            Ui.Hero(
                $"[{Ui.Gold}]In-play at {minute}′ — {Markup.Escape(home.Name)} {hg}–{ag} {Markup.Escape(away.Name)}[/]\n" +
                $"{Markup.Escape(home.Name)} win [bold]{Ui.Pct(pH)}[/]  ·  draw [bold]{Ui.Pct(pD)}[/]  ·  {Markup.Escape(away.Name)} win [bold]{Ui.Pct(pA)}[/]\n" +
                $"[{Ui.Muted}]Pre-match was {Ui.Pct(preH)} / {Ui.Pct(preD)} / {Ui.Pct(preA)}[/]",
                "Live in-play prediction", Ui.GoldColor);
        }
        while (Nav.Confirm("Update again (score / minute changed)?", false));
    }

    private static int PromptInt(string label, int dflt) =>
        Nav.Show(new TextPrompt<int>($"[{Ui.Muted}]{Markup.Escape(label)}[/]").DefaultValue(dflt).ShowDefaultValue());

    // --- 2. Run current scheduled match ---

    private void ScheduledMatch()
    {
        var data = _session.Data;

        // Pull the very latest rosters/results/fixtures before forecasting (bypasses the on-disk
        // caches), so the squads and the schedule reflect the real world as of right now.
        if (_session.LiveConfigured &&
            Nav.Confirm("Pull the latest rosters & results from the live source first (bypass cache)?", false))
        {
            AnsiConsole.Status().Start("Pulling the latest data…", _ => _session.RefreshLatest());
            Ui.Info(_session.LiveDiagnostics);
            data = _session.Data; // squads/fixtures may have been rebuilt
        }

        var playedPairs = _session.PlayedResults
            .Select(r => Pair(r.HomeCode, r.AwayCode))
            .ToHashSet();

        var now = DateTime.UtcNow;
        var upcoming = data.GroupSchedule
            .Where(f => !playedPairs.Contains(Pair(f.HomeCode, f.AwayCode)))
            .OrderBy(f => f.KickoffUtc)
            .ToList();

        // Prefer a match currently in progress (kicked off within the last ~2h), else the next one.
        var fixture = upcoming.FirstOrDefault(f => f.KickoffUtc >= now.AddHours(-2)) ?? upcoming.LastOrDefault();
        if (fixture is null)
        {
            Ui.Warning("No upcoming fixtures remain — all matches have results.");
            return;
        }

        bool inProgress = fixture.KickoffUtc <= now && now < fixture.KickoffUtc.AddHours(2);
        string status = inProgress ? "[green3]in progress[/]" : now < fixture.KickoffUtc ? "upcoming" : "next";

        var home = data.Team(fixture.HomeCode);
        var away = data.Team(fixture.AwayCode);
        AnsiConsole.Clear();
        Ui.Header("Current / next scheduled match");
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(home.Name)}[/] vs [bold]{Markup.Escape(away.Name)}[/]  ({status})");
        AnsiConsole.MarkupLine($"[grey]Group {fixture.Group} · Matchday {fixture.Matchday} · {fixture.KickoffUtc:yyyy-MM-dd HH:mm} UTC[/]");
        Ui.Blank();

        // Show the line-ups the match will be played with (uses Current parameters — formation and
        // player availability are editable in the Parameters menu). Live announced XIs need a paid
        // data tier; otherwise this is the most-probable XI derived from the squads.
        LineupFormatter.PrintProjectedLineups(home, away, _session.Current);
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Edit formation / mark players unavailable in Parameters. Live line-ups need a paid data tier.[/]");

        // The free data tier lists rosters but not the announced XI, so the keeper shown is a best
        // guess from the squad order and can be a backup. Let the user pin the real starting keeper —
        // it feeds the simulation, not just the display.
        if (Nav.Confirm("Line-up not right (e.g. wrong goalkeeper)? Set the starting keeper now?", false))
        {
            var which = Nav.Show(new SelectionPrompt<string>()
                .Title("Whose goalkeeper?")
                .AddChoices(home.Name, away.Name, "Both", "Cancel"));
            if (which == home.Name || which == "Both") ConsoleHelpers.SetStartingGoalkeeper(_session, home);
            if (which == away.Name || which == "Both") ConsoleHelpers.SetStartingGoalkeeper(_session, away);
            if (which != "Cancel")
            {
                Ui.Blank();
                LineupFormatter.PrintProjectedLineups(home, away, _session.Current);
            }
        }

        // When the match is in progress, offer a live in-play prediction that updates with the score/clock.
        if (inProgress && Nav.Confirm("Show LIVE in-play prediction (updates with the current score & clock)?", true))
        {
            InPlayPrediction(home, away, _session.Current, !IsHost(home), fixture.KickoffUtc);
        }

        var p = PromptParameterSet();

        // Law of large numbers: run the fixture many times and condense to one forecast.
        long n = ConsoleHelpers.PromptIterations("How many simulations to aggregate?", 100_000);
        if (!ConsoleHelpers.ConfirmLongRun(n))
        {
            return;
        }

        bool neutral = !IsHost(home);
        Ui.RunConfig($"{home.Name} vs {away.Name} (scheduled)", p.Label, p.Global.Seed, "Detailed (aggregated)", n);
        var report = ConsoleHelpers.RunWithProgress("Simulating fixture", n,
            (counter, ct) => MonteCarloMatchRunner.RunAggregate(home, away, p, n, Stage.Group, neutral, counter, ct));

        MatchReportFormatter.PrintAggregate(report);

        if (Nav.Confirm("View example matches?", true))
        {
            ShowExampleMatches(home, away, p, neutral, report);
        }

        ConsoleHelpers.OfferExports($"fixture_{home.Code}_{away.Code}",
            path => Exporters.ToJson(report, path), path => Exporters.MatchAggregateToCsv(report, path),
            path => HtmlExporter.MatchAggregateToHtml(report, path));
        ConsoleHelpers.Pause();
    }

    // --- 3. Run all scheduled games ---

    /// <summary>
    /// Forecast EVERY remaining scheduled group fixture (those without a result yet) over a big Monte
    /// Carlo each, and present one scannable table of the most-likely result for every game.
    /// </summary>
    private void RunAllScheduledGames()
    {
        var data = _session.Data;

        // Pull the very latest results first so the "remaining" set is accurate.
        if (_session.LiveConfigured &&
            Nav.Confirm("Pull the latest results from the live source first (so the remaining set is current)?", false))
        {
            AnsiConsole.Status().Start("Pulling the latest data…", _ => _session.RefreshLatest());
            Ui.Info(_session.LiveDiagnostics);
            data = _session.Data;
        }

        var playedPairs = _session.PlayedResults.Select(r => Pair(r.HomeCode, r.AwayCode)).ToHashSet();
        var remaining = data.GroupSchedule
            .Where(f => !playedPairs.Contains(Pair(f.HomeCode, f.AwayCode)))
            .OrderBy(f => f.KickoffUtc)
            .ToList();

        if (remaining.Count == 0)
        {
            Ui.Warning("No remaining fixtures — every scheduled group game already has a result.");
            return;
        }

        Ui.Info($"{remaining.Count} scheduled group game(s) still to play.");

        // Narrow to a single day (handy day-to-day — "today", "tomorrow" or any date with fixtures)
        // or forecast the whole remaining slate. Grouped by LOCAL calendar day (kickoffs are stored in
        // UTC), so an evening game whose UTC date is tomorrow still counts as "today" for the viewer.
        static DateTime LocalDay(DateTime kickoffUtc) => kickoffUtc.ToLocalTime().Date;
        var today = DateTime.Now.Date;
        var scopeByLabel = new Dictionary<string, DateTime?>();
        string allLabel = $"All remaining fixtures ({remaining.Count})";
        scopeByLabel[allLabel] = null;
        var labels = new List<string> { allLabel };
        foreach (var d in remaining.Select(f => LocalDay(f.KickoffUtc)).Distinct().OrderBy(d => d))
        {
            int count = remaining.Count(f => LocalDay(f.KickoffUtc) == d);
            string tag = d == today ? " (today)" : d == today.AddDays(1) ? " (tomorrow)" : "";
            string label = $"{d:ddd MMM d}{tag} — {count} game(s)";
            labels.Add(label);
            scopeByLabel[label] = d;
        }

        string tz = TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? TimeZoneInfo.Local.DaylightName : TimeZoneInfo.Local.StandardName;
        DateTime? scopeDay = scopeByLabel[Nav.Show(new SelectionPrompt<string>()
            .Title($"Which fixtures to forecast? [grey](days are your local time — {Markup.Escape(tz)})[/]")
            .PageSize(16)
            .WrapAround()
            .AddChoices(labels))];
        var fixtures = scopeDay is null ? remaining : remaining.Where(f => LocalDay(f.KickoffUtc) == scopeDay.Value).ToList();
        string scopeLabel = scopeDay is null ? "all remaining" : $"{scopeDay:yyyy-MM-dd} (local)";
        string fileTag = scopeDay is null ? "all" : $"{scopeDay:yyyyMMdd}";

        string mode = Nav.Show(new SelectionPrompt<string>()
            .Title("What would you like to do with these fixtures?")
            .WrapAround()
            .AddChoices(
                "Forecast — Monte Carlo odds (W/D/L, scorelines, markets) for each game",
                "Play out once — a full detailed instance of each game (all stats + commentary) as HTML"));

        if (mode.StartsWith("Play out"))
        {
            PlayOutScheduledGames(data, fixtures, scopeLabel, fileTag);
            return;
        }

        long n = ConsoleHelpers.PromptIterations("How many simulations per game?", 1_000_000);
        long totalSims = n * fixtures.Count;
        if (!Nav.Confirm(
            $"This will run [bold]{fixtures.Count}[/] game(s) × [bold]{n:N0}[/] = [bold]{totalSims:N0}[/] simulations. Proceed?", true))
        {
            return;
        }

        var p = PromptParameterSet();
        AnsiConsole.Clear();
        Ui.RunConfig($"Run scheduled games — {scopeLabel}", p.Label, p.Global.Seed, "Fast (W/D/L + score)", n);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var forecasts = ConsoleHelpers.RunWithProgress("Forecasting fixtures", totalSims, (counter, ct) =>
        {
            var list = new List<ScheduledGameForecast>(fixtures.Count);
            foreach (var f in fixtures)
            {
                var home = data.Team(f.HomeCode);
                var away = data.Team(f.AwayCode);
                bool neutral = !IsHost(home);
                var report = MonteCarloMatchRunner.RunFast(home, away, p, n, neutral, counter, ct);
                list.Add(new ScheduledGameForecast(f.Group, f.Matchday, f.KickoffUtc, neutral, report));
                ct.ThrowIfCancellationRequested();
            }

            return list;
        });
        sw.Stop();

        var batch = new ScheduledForecastReport(n, p.Label, p.Global.Seed, sw.Elapsed.TotalSeconds, forecasts);

        AnsiConsole.Clear();
        MatchReportFormatter.PrintScheduledForecasts(batch);

        Ui.Blank();
        if (Nav.Confirm("💾 Download these forecasts as a styled HTML page and open it?", true))
        {
            string path = OutputFolder.Resolve($"scheduled_forecasts_{fileTag}.html");
            HtmlExporter.ScheduledForecastsToHtml(batch, path);
            Ui.Success($"Wrote {path}");
            ConsoleHelpers.OpenInBrowser(path);
        }

        ConsoleHelpers.OfferExports($"scheduled_forecasts_{fileTag}",
            path => Exporters.ToJson(batch, path),
            path => Exporters.ScheduledForecastsToCsv(batch, path),
            path => HtmlExporter.ScheduledForecastsToHtml(batch, path));
        ConsoleHelpers.Pause();
    }

    /// <summary>Play out ONE detailed instance of every scheduled fixture and write a full HTML report
    /// (box score, every event, commentary) for each, linked from an index page.</summary>
    private void PlayOutScheduledGames(TournamentData data, IReadOnlyList<GroupFixture> fixtures, string scopeLabel, string fileTag)
    {
        var p = PromptParameterSet();
        AnsiConsole.Clear();
        Ui.RunConfig($"Play out scheduled games — {scopeLabel}", p.Label, p.Global.Seed, "Detailed (one instance each)", fixtures.Count);

        var results = ConsoleHelpers.RunWithProgress("Playing out each game", fixtures.Count, (counter, ct) =>
        {
            var list = new List<MatchResult>(fixtures.Count);
            var rng = new Xoshiro256(NextRunSeed());
            foreach (var f in fixtures)
            {
                var home = data.Team(f.HomeCode);
                var away = data.Team(f.AwayCode);
                list.Add(MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rng, neutralVenue: !IsHost(home)));
                counter.Add(1);
                ct.ThrowIfCancellationRequested();
            }

            return list;
        });

        AnsiConsole.Clear();
        Ui.Header($"Scheduled games — played out ({scopeLabel})");
        var table = Ui.Table("[bold]Results[/]");
        table.AddColumn("Match");
        table.AddColumn(new TableColumn("Score").Centered());
        table.AddColumn("Notes");
        foreach (var r in results)
        {
            string method = r.Method switch
            {
                MatchMethod.ExtraTime => " [grey](a.e.t.)[/]",
                MatchMethod.Penalties => $" [grey](pens {r.HomePens}-{r.AwayPens})[/]",
                _ => string.Empty,
            };
            var notes = new List<string>();
            int y = r.Cards.Count(c => !c.IsRed), rd = r.Cards.Count(c => c.IsRed);
            if (rd > 0) notes.Add($"[{Ui.Bad}]{rd}🟥[/]");
            if (y > 0) notes.Add($"{y}🟨");
            if (r.Penalties.Count > 0) notes.Add($"{r.Penalties.Count} pen");
            if (r.Miracle is not null) notes.Add($"[{Ui.Gold}]✨ miracle[/]");
            if (r.Confrontations.Any(c => c.Level >= WorldCup.Engine.Simulation.ConfrontationLevel.Scuffle)) notes.Add("🤬");
            table.AddRow(
                $"{Flags.Of(r.HomeCode)} {Markup.Escape(r.HomeName)} v {Markup.Escape(r.AwayName)} {Flags.Of(r.AwayCode)}",
                $"[bold]{r.HomeGoals}-{r.AwayGoals}[/]{method}",
                string.Join(" · ", notes));
        }

        AnsiConsole.Write(table);

        Ui.Blank();
        if (Nav.Confirm("💾 Write a full HTML report for every game (with an index) and open it?", true))
        {
            string dir = OutputFolder.Subdir($"scheduled_games_{fileTag}");
            string index = HtmlExporter.ScheduledInstancesBundle(results, dir);
            Ui.Success($"Wrote {results.Count} full game reports (+ commentary transcripts) and an index to {dir}");
            ConsoleHelpers.OpenInBrowser(index);
        }

        ConsoleHelpers.Pause();
    }

    // --- 4 & 5. Full tournament ---

    private void TournamentScenario(bool currentState)
    {
        var data = _session.Data;
        string scenarioName = currentState
            ? "Full tournament — current state (continue from now)"
            : "Full tournament — official groups (from the start)";

        IReadOnlyList<PlayedResult>? locked = null;
        if (currentState)
        {
            if (_session.PlayedResults.Count == 0)
            {
                Ui.Warning("No real results are loaded. Add them to data/results_2026.json (or use the live provider).");
                return;
            }

            string refreshLabel = _session.LiveConfigured
                ? "Re-pull the latest results from the live source now?"
                : "Re-read results from the bundled file?";
            if (Nav.Confirm(refreshLabel, false))
            {
                _session.RefreshResults();
                Ui.Info(_session.LiveDiagnostics);
            }

            locked = _session.PlayedResults;
            Ui.Info($"{locked.Count} real results will be locked as fixed.");
        }

        var mode = Nav.Show(new SelectionPrompt<string>()
            .Title($"[bold]{scenarioName}[/] — how?")
            .AddChoices(
                "Single playthrough (detailed: full bracket + stats)",
                "Walk through game by game (forecast each game over many runs)",
                "Monte Carlo — fast probabilities (millions)",
                "Monte Carlo — detailed stats (hundreds–thousands)"));

        var p = PromptParameterSet();

        if (mode.StartsWith("Single"))
        {
            SingleTournament(p, locked, scenarioName, currentState);
        }
        else if (mode.StartsWith("Walk"))
        {
            WalkThrough(p);
        }
        else if (mode.Contains("fast"))
        {
            FastTournamentMonteCarlo(p, locked, scenarioName, currentState);
        }
        else
        {
            DetailedTournamentMonteCarlo(p, locked, scenarioName);
        }

        ConsoleHelpers.Pause();
    }

    private void SingleTournament(SimulationParameters p, IReadOnlyList<PlayedResult>? locked, string scenario, bool currentState)
    {
        var data = _session.Data;
        ulong seed = NextRunSeed();
        AnsiConsole.Clear();
        Ui.RunConfig(scenario, p.Label, seed, "Detailed");
        if (currentState)
        {
            Ui.Info("Locked (real) results are marked; all other matches are simulated.");
        }

        var sim = new TournamentSimulator(data, p, _session.IncludeThirdPlacePlayoff, locked);
        var rng = new Xoshiro256(seed);
        var result = sim.Simulate(Fidelity.Detailed, ref rng);

        TournamentReportFormatter.PrintSinglePlaythrough(result, data);

        var agg = new TournamentStatsAggregator(data, p.Global.Mvp);
        agg.Add(result);
        var stats = agg.Build();

        // Showpiece: download the FULL report as a linked HTML bundle (bracket + stats + index).
        Ui.Blank();
        if (Nav.Confirm("💾 Download the full tournament report (bracket + stats, linked) as an HTML bundle and open it?", true))
        {
            string championName = result.ChampionCode.Length > 0 ? data.Team(result.ChampionCode).Name : "bracket";
            string dir = OutputFolder.Subdir($"worldcup_2026_{championName.Replace(' ', '_')}");
            string index = HtmlExporter.TournamentBundle(result, stats, data, dir);
            Ui.Success($"Wrote {index} (+ bracket.html, stats.html)");
            ConsoleHelpers.OpenInBrowser(index);
        }

        if (Nav.Confirm("💾 Save this tournament to a file (revisit it later without re-running)?", false))
        {
            string champ = result.ChampionCode.Length > 0 ? data.Team(result.ChampionCode).Name.Replace(' ', '_') : "tournament";
            string sp = OutputFolder.Resolve($"tournament_{champ}.json");
            TournamentSnapshot.Save(result, sp);
            Ui.Success($"Saved {sp} — reload it any time from the main menu (option 12).");
        }

        Ui.Blank();
        StatsReportFormatter.Print(stats);

        // Drill into any individual game to see what happened (full box score + events).
        InspectGames(result, data);

        ConsoleHelpers.OfferExports("tournament_bracket",
            path => Exporters.ToJson(new
            {
                result.ChampionCode,
                result.RunnerUpCode,
                result.ThirdPlaceCode,
                GroupStandings = result.GroupStandings.OrderBy(g => g.Key).Select(g => new
                {
                    Group = g.Key.ToString(),
                    Table = g.Value.Select(s => new
                    {
                        s.Rank, s.Code, Name = data.Team(s.Code).Name, s.Played, s.Won, s.Drawn, s.Lost,
                        s.GoalsFor, s.GoalsAgainst, s.GoalDifference, s.Points,
                    }),
                }),
                QualifiedThirds = result.QualifiedThirds.Select(s => new { s.Group, s.Code, Name = data.Team(s.Code).Name, s.Points, s.GoalDifference }),
                Bracket = result.KnockoutResults.Select(k => new
                {
                    k.MatchId, k.Label, Stage = k.Stage.ToString(),
                    k.Result.HomeCode, k.Result.AwayCode, k.Result.HomeGoals, k.Result.AwayGoals,
                    Method = k.Result.Method.ToString(), k.Result.HomePens, k.Result.AwayPens, k.Result.WinnerCode,
                }),
                FurthestStage = result.FurthestStage.OrderBy(kv => kv.Key).Select(kv => new { Code = kv.Key, Stage = kv.Value.ToString() }),
            }, path),
            path => Exporters.StatsToCsv(stats, path));
    }

    /// <summary>Write a played-out tournament's bracket (champion + every knockout round) to HTML and open it.</summary>
    private void WriteBracket(TournamentResult result, string fileTag)
    {
        if (string.IsNullOrEmpty(result.ChampionCode))
        {
            return;
        }

        string champ = _session.Data.Team(result.ChampionCode).Name.Replace(' ', '_');
        string path = Path.Combine(OutputFolder.Root, $"bracket_{fileTag}_{champ}.html");
        Directory.CreateDirectory(OutputFolder.Root);
        HtmlExporter.TournamentToHtml(result, _session.Data, path);
        Ui.Success($"Wrote {path}");
        ConsoleHelpers.OpenInBrowser(path);
    }

    /// <summary>Offer to download a single played-out tournament's bracket (who won, round by round).</summary>
    private void OfferBracketDownload(TournamentResult result, string fileTag, bool defaultYes = true)
    {
        if (string.IsNullOrEmpty(result.ChampionCode))
        {
            return;
        }

        Ui.Blank();
        if (Nav.Confirm("💾 Download this tournament's bracket (champion + every knockout round) as HTML and open it?", defaultYes))
        {
            WriteBracket(result, fileTag);
        }
    }

    /// <summary>For Monte-Carlo modes (many tournaments → no single bracket): offer to play out ONE sample
    /// tournament and download its bracket, so every World Cup sim can produce a "who won, how it went" tree.</summary>
    private void OfferSampleBracket(SimulationParameters p, IReadOnlyList<PlayedResult>? locked, string fileTag)
    {
        Ui.Blank();
        if (!Nav.Confirm("💾 Play out ONE sample tournament from these odds and download its bracket as HTML?", false))
        {
            return;
        }

        var rng = new Xoshiro256(p.Global.Seed);
        var sample = new TournamentSimulator(_session.Data, p, _session.IncludeThirdPlacePlayoff, locked).Simulate(Fidelity.Fast, ref rng);
        WriteBracket(sample, fileTag);
    }

    private void FastTournamentMonteCarlo(SimulationParameters p, IReadOnlyList<PlayedResult>? locked, string scenario, bool currentState)
    {
        long n = ConsoleHelpers.PromptIterations("How many tournaments?", 1_000_000);
        if (!ConsoleHelpers.ConfirmLongRun(n)) return;

        AnsiConsole.Clear();
        Ui.RunConfig(scenario, p.Label, p.Global.Seed, "Fast", n);
        var report = ConsoleHelpers.RunWithProgress("Simulating tournaments", n, (counter, ct) =>
            MonteCarloTournamentRunner.Run(_session.Data, p, n, _session.IncludeThirdPlacePlayoff, locked, counter, ct));

        TournamentReportFormatter.PrintMonteCarlo(report);
        ConsoleHelpers.OfferExports("tournament_mc",
            path => Exporters.ToJson(report, path), path => Exporters.TournamentOddsToCsv(report, path),
            path => HtmlExporter.TournamentMonteCarloToHtml(report, path));
        OfferSampleBracket(p, locked, "tournament_mc");
    }

    private void DetailedTournamentMonteCarlo(SimulationParameters p, IReadOnlyList<PlayedResult>? locked, string scenario)
    {
        long n = ConsoleHelpers.PromptIterations("How many detailed tournaments?", 200);
        if (!ConsoleHelpers.ConfirmLongRun(n)) return;

        var data = _session.Data;
        AnsiConsole.Clear();
        Ui.RunConfig(scenario, p.Label, p.Global.Seed, "Detailed", n);

        var (stats, champions, sample) = ConsoleHelpers.RunWithProgress("Simulating detailed tournaments", n, (counter, ct) =>
        {
            var agg = new TournamentStatsAggregator(data, p.Global.Mvp);
            var champ = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            TournamentResult? sampled = null;
            var rng = new Xoshiro256(p.Global.Seed);
            var sim = new TournamentSimulator(data, p, _session.IncludeThirdPlacePlayoff, locked);
            for (long i = 0; i < n; i++)
            {
                var result = sim.Simulate(Fidelity.Detailed, ref rng);
                agg.Add(result);
                if (result.ChampionCode.Length > 0)
                {
                    champ[result.ChampionCode] = champ.GetValueOrDefault(result.ChampionCode) + 1;
                    sampled = result; // keep one fully-played tournament to offer as a downloadable bracket
                }

                counter.Add(1);
                ct.ThrowIfCancellationRequested();
            }

            return (agg.Build(), champ, sampled);
        });

        // Champion frequency.
        var champTable = Ui.Table("[bold]Most frequent champions[/]");
        champTable.AddColumn("Team");
        champTable.AddColumn(new TableColumn("Titles").RightAligned());
        champTable.AddColumn(new TableColumn("Win %").RightAligned());
        foreach (var kv in champions.OrderByDescending(c => c.Value).Take(10))
        {
            champTable.AddRow(Markup.Escape(data.Team(kv.Key).Name), kv.Value.ToString(), Ui.Heat((double)kv.Value / n));
        }

        AnsiConsole.Write(champTable);
        Ui.Blank();
        StatsReportFormatter.Print(stats);
        ConsoleHelpers.OfferExports("tournament_detailed_mc",
            path => Exporters.ToJson(stats, path), path => Exporters.StatsToCsv(stats, path),
            path => HtmlExporter.StatsToHtml(stats, path));
        if (sample is not null)
        {
            OfferBracketDownload(sample, "tournament_detailed_mc");
        }
    }

    // --- Walk the whole World Cup game by game ---

    private void WalkThrough(SimulationParameters p)
    {
        var data = _session.Data;
        long n = ConsoleHelpers.PromptIterations("Runs per game (each game is forecast over this many sims)?", 50_000);
        if (!ConsoleHelpers.ConfirmLongRun(n))
        {
            return;
        }

        var games = BuildExpectedGames(data, p);
        int total = games.Count;

        // Pace once, up front — so you're not pressing a key 100+ times.
        bool auto = Nav.Show(new SelectionPrompt<string>()
            .Title($"[bold]{total} games[/] to walk through. How?")
            .AddChoices("Auto-advance (watch it unfold, no key presses)", "Step through (a key between each game)"))
            .StartsWith("Auto");

        for (int i = 0; i < total; i++)
        {
            var g = games[i];
            AnsiConsole.Clear();
            Ui.Header($"World Cup walkthrough — game {i + 1} of {total}");
            AnsiConsole.MarkupLine($"[{Ui.Accent}]{Markup.Escape(g.Label)}[/]");
            AnsiConsole.MarkupLine($"[bold]{Markup.Escape(g.Home.Name)}[/] vs [bold]{Markup.Escape(g.Away.Name)}[/]");
            Ui.Blank();

            var report = ConsoleHelpers.RunWithProgress("Forecasting this game", n,
                (counter, ct) => MonteCarloMatchRunner.RunAggregate(g.Home, g.Away, p, n, g.Stage, g.Neutral, counter, ct));
            Ui.PredictedOn();
            MatchReportFormatter.PrintAggregate(report);
            if (g.Stage != Stage.Group)
            {
                AnsiConsole.MarkupLine($"[{Ui.Muted}]Knockout — win % is the chance to ADVANCE (ties resolved by extra time / penalties).[/]");
            }

            if (i < total - 1)
            {
                if (auto)
                {
                    AnsiConsole.MarkupLine($"[{Ui.Muted}]Next: {Markup.Escape(games[i + 1].Home.Name)} vs {Markup.Escape(games[i + 1].Away.Name)}…  (Ctrl+C to stop)[/]");
                    System.Threading.Thread.Sleep(2200);
                }
                else
                {
                    var choice = Nav.Show(new SelectionPrompt<string>()
                        .Title($"[grey]Game {i + 1}/{total} done — next: {Markup.Escape(games[i + 1].Home.Name)} vs {Markup.Escape(games[i + 1].Away.Name)}[/]")
                        .AddChoices("Next game", "Quit walkthrough"));
                    if (choice.StartsWith("Quit"))
                    {
                        return;
                    }
                }
            }
        }

        AnsiConsole.Clear();
        Ui.Success("That's the whole World Cup, game by game! 🏆");
    }

    /// <summary>
    /// Build the expected fixture list for a full walkthrough: all 72 group games in schedule order,
    /// then the knockout games in bracket order. The knockout bracket is the "chalk" — group winners
    /// /runners-up and the favourite advancing each round (by effective strength) — so every game has
    /// a concrete matchup. Each game is still forecast over many real simulations when walked.
    /// </summary>
    private List<(string Label, Team Home, Team Away, bool Neutral, Stage Stage)> BuildExpectedGames(
        TournamentData data, SimulationParameters p)
    {
        var games = new List<(string, Team, Team, bool, Stage)>();

        foreach (var f in data.GroupSchedule)
        {
            var home = data.Team(f.HomeCode);
            var away = data.Team(f.AwayCode);
            games.Add(($"Group {f.Group} · Matchday {f.Matchday}", home, away, !IsHost(home), Stage.Group));
        }

        // Chalk qualifiers: top two by strength per group, best eight third-placed by strength.
        var winners = new Dictionary<char, string>();
        var runnersUp = new Dictionary<char, string>();
        var thirdByGroup = new Dictionary<char, string>();
        var thirdCandidates = new List<(char Group, double Strength)>();
        foreach (var group in data.Groups)
        {
            var sorted = data.TeamsInGroup(group).OrderByDescending(t => p.EffectiveStrength(t)).ToList();
            winners[group] = sorted[0].Code;
            runnersUp[group] = sorted[1].Code;
            thirdByGroup[group] = sorted[2].Code;
            thirdCandidates.Add((group, p.EffectiveStrength(sorted[2])));
        }

        var qualifyingThirds = thirdCandidates.OrderByDescending(x => x.Strength).Take(8).Select(x => x.Group).ToList();
        var assignment = ThirdPlaceAssigner.Assign(
            data.Bracket.ThirdPlaceWinnerGroups, qualifyingThirds, data.Bracket.ThirdPlaceEligibleGroups);
        var thirdForWinnerGroup = new Dictionary<char, string>();
        foreach (var (winnerGroup, sourceGroup) in assignment)
        {
            thirdForWinnerGroup[winnerGroup] = thirdByGroup[sourceGroup];
        }

        var resolver = new KnockoutResolver(winners, runnersUp, thirdForWinnerGroup);
        foreach (var def in data.Bracket.Matches)
        {
            if (def.Stage == Stage.ThirdPlacePlayoff && !_session.IncludeThirdPlacePlayoff)
            {
                continue;
            }

            var home = data.Team(resolver.Resolve(def.Top));
            var away = data.Team(resolver.Resolve(def.Bottom));
            string winnerCode = p.EffectiveStrength(home) >= p.EffectiveStrength(away) ? home.Code : away.Code;
            resolver.Record(def.Id, winnerCode, winnerCode == home.Code ? away.Code : home.Code);
            games.Add(($"{Stages.DisplayName(def.Stage)} · {def.Label}", home, away, true, def.Stage));
        }

        return games;
    }

    // --- 5. Group path to victory & defeat ---

    /// <summary>
    /// Take a group's current standings, pick a team, and lay out every way it can still win the group
    /// or get knocked out: the finishing-tier probabilities, what its own remaining game(s) need to
    /// yield, and the concrete result-combinations that win the group or finish it last.
    /// </summary>
    private void GroupPath()
    {
        var data = _session.Data;
        char group = PickGroup(data);

        // Offer to re-pull live results so the standings reflect the very latest scores.
        if (_session.PlayedResults.Count > 0 && _session.LiveConfigured &&
            Nav.Confirm("Re-pull the latest results from the live source first?", false))
        {
            _session.RefreshResults();
            Ui.Info(_session.LiveDiagnostics);
            data = _session.Data;
        }

        var pathMode = Nav.Show(new SelectionPrompt<string>()
            .Title($"[bold]Group {group}[/] — analyse what?")
            .WrapAround()
            .AddChoices(
                "The whole group (all four teams at once)",
                "A single team (full path to victory & defeat)",
                "Qualification scenarios grid (every remaining-results combination)"));

        if (pathMode.StartsWith("Qualification"))
        {
            QualificationScenarios(data, group);
            return;
        }

        bool wholeGroup = pathMode.StartsWith("The whole group");
        Team? team = wholeGroup ? null : ConsoleHelpers.PickTeamInGroup(data, group, "Pick a team to analyse:");

        var p = PromptParameterSet();
        long n = ConsoleHelpers.PromptIterations("How many simulations for the odds?", 100_000);
        if (!ConsoleHelpers.ConfirmLongRun(n)) return;
        ulong seed = NextRunSeed();
        AnsiConsole.Clear();

        if (wholeGroup)
        {
            Ui.RunConfig($"Group {group} — full outlook", p.Label, seed, "Fast (group odds)", n);
            var outlook = ConsoleHelpers.RunWithProgress("Simulating the whole group", n,
                (counter, _) => GroupPathAnalyzer.AnalyzeGroup(data, group, p, _session.PlayedResults, n, seed, counter));

            AnsiConsole.Clear();
            GroupPathFormatter.PrintGroupOutlook(outlook);

            Ui.Blank();
            if (Nav.Confirm("💾 Download this group outlook as a styled HTML page and open it?", true))
            {
                string path = OutputFolder.Resolve($"group_{group}_outlook.html");
                HtmlExporter.GroupOutlookToHtml(outlook, path);
                Ui.Success($"Wrote {path}");
                ConsoleHelpers.OpenInBrowser(path);
            }

            ConsoleHelpers.OfferExports($"group_{group}_outlook",
                path => Exporters.ToJson(outlook, path), null,
                path => HtmlExporter.GroupOutlookToHtml(outlook, path));
            ConsoleHelpers.Pause();
            return;
        }

        var t = team!;
        Ui.RunConfig($"Group {group} path — {t.Name}", p.Label, seed, "Fast (group odds)", n);
        var analysis = ConsoleHelpers.RunWithProgress("Mapping the paths", n,
            (counter, _) => GroupPathAnalyzer.Analyze(data, group, t.Code, p, _session.PlayedResults, n, seed, counter));

        AnsiConsole.Clear();
        GroupPathFormatter.Print(analysis);

        Ui.Blank();
        if (Nav.Confirm("💾 Download this path analysis as a styled HTML page and open it?", true))
        {
            string file = $"group_{group}_{t.Code}_path.html";
            string path = OutputFolder.Resolve(file);
            HtmlExporter.GroupPathToHtml(analysis, path);
            Ui.Success($"Wrote {path}");
            ConsoleHelpers.OpenInBrowser(path);
        }

        ConsoleHelpers.OfferExports($"group_{group}_{t.Code}_path",
            path => Exporters.ToJson(analysis, path), null,
            path => HtmlExporter.GroupPathToHtml(analysis, path));

        ConsoleHelpers.Pause();
    }

    private char PickGroup(TournamentData data)
    {
        var choices = data.Groups
            .Select(g => $"Group {g} — {string.Join(", ", data.TeamsInGroup(g).Select(t => t.Name))}")
            .ToList();
        string pick = Nav.Show(new SelectionPrompt<string>()
            .Title("Pick a [bold]group[/]:")
            .PageSize(12)
            .MoreChoicesText("[grey](scroll for more groups)[/]")
            .AddChoices(choices));
        return pick["Group ".Length];
    }

    // --- 6. Run until a team wins ---

    private void RunUntilWin()
    {
        var data = _session.Data;
        var team = ConsoleHelpers.PickTeam(data, "Pick the team you want to win:");
        var what = Nav.Show(new SelectionPrompt<string>()
            .Title($"Run until [bold]{Markup.Escape(team.Name)}[/] win what?")
            .AddChoices(
                "The World Cup (full tournament)",
                "Their next scheduled match",
                "A match vs a chosen opponent"));
        var p = PromptParameterSet();

        if (what.StartsWith("The World Cup"))
        {
            RunUntilCup(team, p);
            return;
        }

        Team home, away;
        bool neutral;
        if (what.StartsWith("Their next"))
        {
            var fixture = data.GroupSchedule
                .Where(f => string.Equals(f.HomeCode, team.Code, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(f.AwayCode, team.Code, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.KickoffUtc)
                .FirstOrDefault();
            if (fixture is null)
            {
                Ui.Warning("No scheduled fixture found for that team.");
                return;
            }

            home = data.Team(fixture.HomeCode);
            away = data.Team(fixture.AwayCode);
            neutral = !IsHost(home);
        }
        else
        {
            var opponent = ConsoleHelpers.PickTeam(data, "Pick the opponent:");
            if (opponent.Code == team.Code)
            {
                Ui.Warning("Pick a different opponent.");
                return;
            }

            home = team;
            away = opponent;
            neutral = true;
        }

        RunUntilMatch(team, home, away, neutral, p);
    }

    private void RunUntilCup(Team team, SimulationParameters p)
    {
        var data = _session.Data;
        const long maxAttempts = 250_000;
        var sim = new TournamentSimulator(data, p, _session.IncludeThirdPlacePlayoff);
        var rng = new Xoshiro256(NextRunSeed());
        long attempts = 0;
        Stage best = Stage.Group;
        TournamentResult? win = null;

        AnsiConsole.Status().Start($"Simulating until {team.Name} win the World Cup…", ctx =>
        {
            while (attempts < maxAttempts)
            {
                var result = sim.Simulate(Fidelity.Fast, ref rng);
                attempts++;
                if (string.Equals(result.ChampionCode, team.Code, StringComparison.OrdinalIgnoreCase))
                {
                    win = result;
                    break;
                }

                if (result.FurthestStage.TryGetValue(team.Code, out var st) && Stages.Rank(st) > Stages.Rank(best))
                {
                    best = st;
                }

                if (attempts % 1000 == 0)
                {
                    ctx.Status($"Simulating until {team.Name} win the cup… attempt {attempts:N0}");
                }
            }
        });

        AnsiConsole.Clear();
        if (win is not null)
        {
            Ui.Header($"🏆 {team.Name} are World Champions!");
            Ui.PredictedOn();
            Ui.Blank();
            TournamentReportFormatter.PrintSinglePlaythrough(win, data);

            // Repeat the iteration count at the bottom so it survives the long bracket output above.
            Ui.Blank();
            Ui.Hero(
                $"[{Ui.Gold}]{Markup.Escape(team.Name)} won the World Cup on simulation #{attempts:N0}[/]\n" +
                $"[{Ui.Muted}]{attempts:N0} full tournament(s) needed — about a 1-in-{attempts:N0} shot (≈ {100.0 / attempts:0.000}% title odds)[/]",
                "Iterations to a title", Ui.GoldColor);
            OfferBracketDownload(win, "run_until_win");
        }
        else
        {
            Ui.Warning($"{team.Name} did NOT win the cup in {maxAttempts:N0} tournaments — a serious long shot. " +
                       $"The best they managed was the {Stages.DisplayName(best).ToLowerInvariant()}.");
        }

        ConsoleHelpers.Pause();
    }

    private void RunUntilMatch(Team team, Team home, Team away, bool neutral, SimulationParameters p)
    {
        const long maxAttempts = 1_000_000;
        var rng = new Xoshiro256(NextRunSeed());
        long attempts = 0;
        MatchResult? win = null;

        AnsiConsole.Status().Start($"Simulating until {team.Name} beat {(team.Code == home.Code ? away.Name : home.Name)}…", ctx =>
        {
            while (attempts < maxAttempts)
            {
                var m = MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rng, neutral);
                attempts++;
                if (string.Equals(m.WinnerCode, team.Code, StringComparison.OrdinalIgnoreCase))
                {
                    win = m;
                    break;
                }

                if (attempts % 500 == 0)
                {
                    ctx.Status($"Simulating until {team.Name} win… attempt {attempts:N0}");
                }
            }
        });

        AnsiConsole.Clear();
        if (win is not null)
        {
            Ui.Header($"🏆 {team.Name} win!");
            Ui.Blank();
            MatchReportFormatter.PrintDetailed(win, showEventLog: false);

            // Repeat the iteration count at the bottom so it survives the box score above.
            Ui.Blank();
            Ui.Hero(
                $"[{Ui.Gold}]{Markup.Escape(team.Name)} won on simulation #{attempts:N0}[/]\n" +
                $"[{Ui.Muted}]{attempts:N0} simulated match(es) needed before the win[/]",
                "Iterations to a win", Ui.GoldColor);
        }
        else
        {
            Ui.Warning($"{team.Name} did not win in {maxAttempts:N0} matches — an extreme long shot.");
        }

        ConsoleHelpers.Pause();
    }

    // --- helpers ---

    /// <summary>Interactive drill-down: pick any game from the tournament and see its full box score.</summary>
    private void InspectGames(TournamentResult result, TournamentData data)
    {
        var games = new List<(string Label, MatchResult Match)>();
        foreach (var m in result.GroupResults)
        {
            char g = data.Team(m.HomeCode).Group;
            games.Add(($"Group {g} · {m.HomeName} {m.HomeGoals}-{m.AwayGoals} {m.AwayName}{(m.IsLocked ? " (real)" : "")}", m));
        }

        foreach (var k in result.KnockoutResults.OrderBy(k => k.MatchId))
        {
            var r = k.Result;
            games.Add(($"{k.Label} · {r.HomeName} {r.HomeGoals}-{r.AwayGoals} {r.AwayName}", r));
        }

        while (Nav.Confirm("Inspect a single game to see what happened (full box score)?", false))
        {
            string label = Nav.Show(new SelectionPrompt<string>()
                .Title("Pick a game [grey](type to search by team)[/]:")
                .PageSize(20)
                .EnableSearch()
                .MoreChoicesText("[grey](scroll, or type to filter)[/]")
                .AddChoices(games.Select(x => x.Label)));

            var match = games.First(x => x.Label == label).Match;
            bool log = Nav.Confirm("Show minute-by-minute event log?", false);
            AnsiConsole.Clear();
            MatchReportFormatter.PrintDetailed(match, log);
        }
    }

    /// <summary>
    /// Watch example matches drawn from the forecast distribution. The user chooses what to see — the
    /// most likely scoreline, the most likely result for the favourite, any exact scoreline, or a
    /// random game — and every shown match reports how likely that exact scoreline and result were.
    /// </summary>
    private void ShowExampleMatches(Team home, Team away, SimulationParameters p, bool neutral, MatchAggregateReport report)
    {
        var modal = report.TopScorelines.Count > 0 ? report.TopScorelines[0] : null;
        int favSign = report.HomeWin >= report.Draw && report.HomeWin >= report.AwayWin ? 1
            : report.AwayWin >= report.Draw && report.AwayWin >= report.HomeWin ? -1 : 0;
        var favScore = report.TopScorelines.FirstOrDefault(s => Math.Sign(s.HomeGoals - s.AwayGoals) == favSign) ?? modal;
        string favName = favSign == 1 ? report.HomeName : favSign == -1 ? report.AwayName : "a draw";

        while (true)
        {
            string? modalLabel = modal is not null
                ? $"📈 Most likely scoreline — {modal.HomeGoals}–{modal.AwayGoals} ({Ui.Pct(modal.Probability)})"
                : null;
            string? favLabel = favScore is not null && modal is not null
                && (favScore.HomeGoals != modal.HomeGoals || favScore.AwayGoals != modal.AwayGoals)
                ? $"🏆 Most likely {favName} result — {favScore.HomeGoals}–{favScore.AwayGoals} ({Ui.Pct(favScore.Probability)})"
                : null;
            const string randomLabel = "🎲 A random game (anything can happen)";
            const string pickLabel = "🎯 Pick an exact scoreline to watch…";
            const string doneLabel = "Done viewing examples";

            var options = new List<string>();
            if (modalLabel is not null) options.Add(modalLabel);
            if (favLabel is not null) options.Add(favLabel);
            options.Add(randomLabel);
            if (report.TopScorelines.Count > 0) options.Add(pickLabel);
            options.Add(doneLabel);

            string choice = Nav.Show(new SelectionPrompt<string>()
                .Title("Watch an example match — which one?")
                .WrapAround()
                .AddChoices(options));

            if (choice == doneLabel)
            {
                return;
            }

            MatchResult m;
            if (choice == modalLabel)
            {
                m = SampleMatchWithScore(home, away, p, neutral, modal!.HomeGoals, modal.AwayGoals);
            }
            else if (choice == favLabel)
            {
                m = SampleMatchWithScore(home, away, p, neutral, favScore!.HomeGoals, favScore.AwayGoals);
            }
            else if (choice == pickLabel)
            {
                var s = PickScoreline(report);
                m = SampleMatchWithScore(home, away, p, neutral, s.HomeGoals, s.AwayGoals);
            }
            else
            {
                var rng = new Xoshiro256(NextRunSeed());
                m = MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rng, neutral);
            }

            AnsiConsole.Clear();
            Ui.Header($"Example match — {home.Name} vs {away.Name}");
            MatchReportFormatter.PrintDetailed(m, showEventLog: false);
            PrintExampleProbability(m, report);

            if (Nav.Confirm("💾 Download THIS match as a styled HTML page (full stats: goals, cards, penalties, fouls, vergazo…)?", false))
            {
                string path = OutputFolder.Resolve($"match_{home.Code}_{away.Code}_{m.HomeGoals}-{m.AwayGoals}.html");
                HtmlExporter.MatchResultToHtml(m, path);
                Ui.Success($"Wrote {path}");
                if (Nav.Confirm("Open it now?", true))
                {
                    ConsoleHelpers.OpenInBrowser(path);
                }
            }
        }
    }

    /// <summary>Report how likely the shown match's exact scoreline and its result were across the run.</summary>
    private static void PrintExampleProbability(MatchResult m, MatchAggregateReport report)
    {
        var sl = report.TopScorelines.FirstOrDefault(s => s.HomeGoals == m.HomeGoals && s.AwayGoals == m.AwayGoals);
        string scoreProb = sl is not null ? Ui.Pct(sl.Probability)
            : report.TopScorelines.Count > 0 ? $"under {Ui.Pct(report.TopScorelines[^1].Probability)}"
            : "very rare";

        bool homeWin = m.HomeGoals > m.AwayGoals, awayWin = m.HomeGoals < m.AwayGoals;
        double outcomeProb = homeWin ? report.HomeWin : awayWin ? report.AwayWin : report.Draw;
        string outcomeName = homeWin ? $"{report.HomeName} win" : awayWin ? $"{report.AwayName} win" : "draw";

        Ui.Blank();
        AnsiConsole.MarkupLine(
            $"[{Ui.Accent}]How likely was this?[/] This exact scoreline ([bold]{m.HomeGoals}–{m.AwayGoals}[/]) came up in " +
            $"[bold]{scoreProb}[/] of {report.Iterations:N0} simulations · this result ([bold]{Markup.Escape(outcomeName)}[/]) in [bold]{Ui.Pct(outcomeProb)}[/].");
    }

    /// <summary>Let the user choose any of the most common scorelines (shown with its probability) to watch played out.</summary>
    private ScorelineFrequency PickScoreline(MatchAggregateReport report)
    {
        var byLabel = new Dictionary<string, ScorelineFrequency>();
        var labels = new List<string>();
        foreach (var s in report.TopScorelines.Take(12))
        {
            string outc = s.HomeGoals > s.AwayGoals ? $"{report.HomeName} win"
                : s.HomeGoals < s.AwayGoals ? $"{report.AwayName} win" : "draw";
            string label = $"{s.HomeGoals}–{s.AwayGoals}   ·   {Ui.Pct(s.Probability)}   ·   {outc}";
            labels.Add(label);
            byLabel[label] = s;
        }

        string pick = Nav.Show(new SelectionPrompt<string>()
            .Title("Pick a scoreline to see played out [grey](probability shown)[/]:")
            .PageSize(14)
            .WrapAround()
            .AddChoices(labels));
        return byLabel[pick];
    }

    /// <summary>Rejection-sample a full detailed match that ends in a target scoreline (drawn from the distribution).</summary>
    private MatchResult SampleMatchWithScore(Team home, Team away, SimulationParameters p, bool neutral, int targetH, int targetA)
    {
        var rng = new Xoshiro256(NextRunSeed());
        MatchResult? last = null;
        for (int i = 0; i < 12000; i++)
        {
            var m = MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rng, neutral);
            last = m;
            if (m.HomeGoals == targetH && m.AwayGoals == targetA)
            {
                return m;
            }
        }

        return last!;
    }

    private SimulationParameters PromptParameterSet()
    {
        // When the user hasn't edited anything, "Current" == "Starting", so don't bother asking.
        if (!_session.ParametersEdited)
        {
            return _session.Current;
        }

        int overrides = _session.Current.TeamStrengthOverrides.Count + _session.Current.PlayerAttributeOverrides.Count
            + _session.Current.FormationOverrides.Count + _session.Current.UnavailablePlayers.Count;
        var choice = Nav.Show(new SelectionPrompt<string>()
            .Title("Which parameter set?")
            .AddChoices(
                "Current parameters (your edits)",
                "Starting parameters (pristine defaults)"));
        return choice.StartsWith("Starting") ? _session.Starting : _session.Current;
    }

    private bool IsHost(Team t) =>
        _session.Data.Metadata.Hosts.Contains(t.Name, StringComparer.OrdinalIgnoreCase);

    private static (string, string) Pair(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
}
