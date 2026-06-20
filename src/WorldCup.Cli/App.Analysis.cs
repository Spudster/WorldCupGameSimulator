using Spectre.Console;
using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Stats;
using WorldCup.Engine.Tournament;
using WorldCup.Reporting;

namespace WorldCup.Cli;

/// <summary>Analysis features: road to glory, odds board, model-accuracy backtest, scenario comparison.</summary>
public sealed partial class App
{
    // --- 7. Road to glory (a team's tournament campaign) ---

    private void RoadToGlory()
    {
        var data = _session.Data;
        var team = ConsoleHelpers.PickTeam(data, "Whose road to glory?");

        IReadOnlyList<PlayedResult>? locked = null;
        if (_session.PlayedResults.Count > 0 &&
            Nav.Confirm($"Continue from the current real results ({_session.PlayedResults.Count} locked)?", true))
        {
            locked = _session.PlayedResults;
        }

        long n = ConsoleHelpers.PromptIterations("How many tournament simulations?", 25_000);
        if (!ConsoleHelpers.ConfirmLongRun(n)) return;

        var p = PromptParameterSet();
        AnsiConsole.Clear();
        Ui.RunConfig($"Road to glory — {team.Name}", p.Label, p.Global.Seed, "Fast tournaments", n);

        var report = ConsoleHelpers.RunWithProgress($"Simulating {team.Name}'s campaign", n, (counter, ct) =>
            RoadToGloryAnalyzer.Analyze(data, team.Code, p, locked, n, p.Global.Seed, _session.IncludeThirdPlacePlayoff, counter));

        PrintRoadToGlory(report);

        ConsoleHelpers.OfferExports($"road_to_glory_{team.Code}",
            path => Exporters.ToJson(report, path), null,
            path => HtmlExporter.RoadToGloryToHtml(report, path));
        ConsoleHelpers.Pause();
    }

    private static void PrintRoadToGlory(RoadToGloryReport r)
    {
        Ui.Hero(
            $"[{Ui.Gold}]{Markup.Escape(r.TeamName)} — road to glory[/]\n" +
            $"[{Ui.Muted}]Champions [bold]{Ui.Pct(r.ChampionProbability)}[/] ({OddsConverter.All(r.ChampionProbability)}) · " +
            $"reach the final [bold]{Ui.Pct(r.FinalProbability)}[/][/]",
            "Road to glory", Ui.GoldColor);

        var t = Ui.Table("[bold]Round-by-round[/]");
        t.AddColumn("Round");
        t.AddColumn(new TableColumn("Reach").RightAligned());
        t.AddColumn(new TableColumn("Odds").RightAligned());
        t.AddColumn(new TableColumn("Win it").RightAligned());
        t.AddColumn("Most likely opponents [grey](if they get there)[/]");
        foreach (var s in r.Stages)
        {
            string opps = s.LikelyOpponents.Count == 0
                ? "[grey]—[/]"
                : string.Join("  ", s.LikelyOpponents.Take(3)
                    .Select(o => $"{Markup.Escape(o.Name)} [grey]{Ui.Pct(o.Probability)}[/]"));
            t.AddRow(
                Markup.Escape(s.StageName),
                Ui.Heat(s.ReachProbability),
                OddsConverter.Decimal(s.ReachProbability),
                Ui.Heat(s.WinProbability),
                opps);
        }

        AnsiConsole.Write(t);
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Reach = chance of getting to that round · Win it = chance of winning that round · odds shown as decimal.[/]");
    }

    // --- 8. Tournament odds board ---

    private void OddsBoard()
    {
        var data = _session.Data;

        IReadOnlyList<PlayedResult>? locked = null;
        if (_session.PlayedResults.Count > 0 &&
            Nav.Confirm($"Continue from the current real results ({_session.PlayedResults.Count} locked)?", true))
        {
            locked = _session.PlayedResults;
        }

        long n = ConsoleHelpers.PromptIterations("How many tournaments?", 500_000);
        if (!ConsoleHelpers.ConfirmLongRun(n)) return;

        var p = PromptParameterSet();
        AnsiConsole.Clear();
        Ui.RunConfig("Tournament odds board", p.Label, p.Global.Seed, "Fast", n);

        var report = ConsoleHelpers.RunWithProgress("Simulating tournaments", n, (counter, ct) =>
            MonteCarloTournamentRunner.Run(data, p, n, _session.IncludeThirdPlacePlayoff, locked, counter, ct));

        PrintOddsBoard(report);

        // Player award odds need scorers, so they come from a (smaller) detailed Monte Carlo.
        if (Nav.Confirm("Also compute player award odds (Golden Boot / Golden Glove / MVP)? Runs a smaller detailed Monte Carlo.", false))
        {
            long m = ConsoleHelpers.PromptIterations("How many detailed tournaments (for awards)?", 300);
            var stats = ConsoleHelpers.RunWithProgress("Simulating detailed tournaments for awards", m, (counter, ct) =>
            {
                var agg = new TournamentStatsAggregator(data, p.Global.Mvp);
                var rng = new Xoshiro256(p.Global.Seed);
                var sim = new TournamentSimulator(data, p, _session.IncludeThirdPlacePlayoff, locked);
                for (long i = 0; i < m; i++)
                {
                    agg.Add(sim.Simulate(Fidelity.Detailed, ref rng));
                    counter.Add(1);
                    ct.ThrowIfCancellationRequested();
                }

                return agg.Build();
            });

            PrintAwardOdds(stats);
            if (Nav.Confirm("💾 Download these award stats as an HTML page and open it?", false))
            {
                string sp = OutputFolder.Resolve("award_odds.html");
                HtmlExporter.StatsToHtml(stats, sp);
                Ui.Success($"Wrote {sp}");
                ConsoleHelpers.OpenInBrowser(sp);
            }
        }

        ConsoleHelpers.OfferExports("odds_board",
            path => Exporters.ToJson(report, path), null,
            path => HtmlExporter.OddsBoardToHtml(report, path));
        ConsoleHelpers.Pause();
    }

    private static void PrintAwardOdds(StatsReport s)
    {
        Ui.Header("Player award odds");
        Board("🥇 Golden Boot (top scorer)", s.GoldenBootFrequency, "avg goals");
        Board("🧤 Golden Glove (best goalkeeper)", s.GoldenGloveFrequency, "avg clean sheets");
        Board("⭐ Golden Ball / MVP", s.MvpFrequency, "avg rating");

        static void Board(string title, IReadOnlyList<AwardFrequencyRow> rows, string tally)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var t = Ui.Table($"[bold]{title}[/]");
            t.AddColumn(new TableColumn("#").RightAligned());
            t.AddColumn("Player");
            t.AddColumn(new TableColumn("Team").Centered());
            t.AddColumn(new TableColumn("Win %").RightAligned());
            t.AddColumn(new TableColumn("Odds").RightAligned());
            t.AddColumn(new TableColumn(tally).RightAligned());
            int i = 1;
            foreach (var r in rows.Take(12))
            {
                t.AddRow(
                    (i++).ToString(),
                    Markup.Escape(r.Name),
                    Markup.Escape(r.TeamCode),
                    Ui.Heat(r.Frequency),
                    OddsConverter.Decimal(r.Frequency),
                    $"{r.AverageTally:0.0}");
            }

            AnsiConsole.Write(t);
        }
    }

    private static void PrintOddsBoard(TournamentMonteCarloReport r)
    {
        Ui.Hero(
            $"[{Ui.Gold}]Outright odds board[/]\n[{Ui.Muted}]{r.Iterations:N0} tournaments · {r.ParameterLabel}[/]",
            "Odds board", Ui.GoldColor);

        var t = Ui.Table("[bold]To win the World Cup[/]");
        t.AddColumn(new TableColumn("#").RightAligned());
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("Grp").Centered());
        t.AddColumn(new TableColumn("Title").RightAligned());
        t.AddColumn(new TableColumn("Decimal").RightAligned());
        t.AddColumn(new TableColumn("Fractional").RightAligned());
        t.AddColumn(new TableColumn("US").RightAligned());
        t.AddColumn(new TableColumn("Final").RightAligned());
        t.AddColumn(new TableColumn("Win grp").RightAligned());

        int i = 1;
        foreach (var team in r.Teams.Where(x => x.Champion > 0).OrderByDescending(x => x.Champion).Take(24))
        {
            t.AddRow(
                (i++).ToString(),
                Markup.Escape(team.Name),
                team.Group.ToString(),
                Ui.Heat(team.Champion),
                OddsConverter.Decimal(team.Champion),
                OddsConverter.Fractional(team.Champion),
                OddsConverter.American(team.Champion),
                Ui.Pct(team.ReachedFinal),
                Ui.Pct(team.TopGroup));
        }

        AnsiConsole.Write(t);
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Odds are fair (no bookmaker margin) — decimal = 1/p, fractional and American (moneyline) shown too.[/]");
    }

    // --- 9. Model accuracy (backtest vs real results) ---

    private void ModelAccuracy()
    {
        var data = _session.Data;
        if (_session.PlayedResults.Count == 0)
        {
            Ui.Warning("No real results are loaded yet — nothing to backtest. Add results (or use the live provider) and try again.");
            ConsoleHelpers.Pause();
            return;
        }

        if (_session.LiveConfigured && Nav.Confirm("Pull the latest results from the live source first?", false))
        {
            AnsiConsole.Status().Start("Pulling the latest data…", _ => _session.RefreshResults());
            data = _session.Data;
        }

        var p = PromptParameterSet();
        AnsiConsole.Clear();
        Ui.Header("Model accuracy — predictions vs real results");
        Ui.PredictedOn();

        var report = BacktestAnalyzer.Analyze(data, _session.PlayedResults, p);
        PrintBacktest(report);

        ConsoleHelpers.OfferExports("model_accuracy",
            path => Exporters.ToJson(report, path), null,
            path => HtmlExporter.BacktestToHtml(report, path));
        ConsoleHelpers.Pause();
    }

    private static void PrintBacktest(BacktestReport r)
    {
        if (r.Matches == 0)
        {
            Ui.Warning("None of the loaded results matched a known fixture — nothing to score.");
            return;
        }

        Ui.Hero(
            $"[{Ui.Gold}]Model accuracy over {r.Matches} played match(es)[/]\n" +
            $"[{Ui.Muted}]Favourite called correctly [bold]{Ui.Pct(r.FavouriteHitRate)}[/] · " +
            $"Brier [bold]{r.BrierScore:0.000}[/] · log-loss [bold]{r.LogLoss:0.000}[/] [grey](lower is better)[/][/]",
            "Backtest", Ui.GoldColor);

        // Calibration: do the model's probabilities mean what they say?
        var cal = Ui.Table("[bold]Calibration — predicted vs observed[/]");
        cal.AddColumn("Predicted band");
        cal.AddColumn(new TableColumn("Cases").RightAligned());
        cal.AddColumn(new TableColumn("Avg predicted").RightAligned());
        cal.AddColumn(new TableColumn("Actually happened").RightAligned());
        foreach (var b in r.Calibration)
        {
            cal.AddRow(
                $"{b.LowEdge:0%}–{b.HighEdge:0%}",
                b.Count.ToString(),
                Ui.Pct(b.PredictedAvg),
                Ui.Pct(b.ObservedFreq));
        }

        AnsiConsole.Write(cal);

        // The model's biggest misses (lowest probability assigned to what actually happened).
        var surprises = r.Rows
            .Select(row => (row, pActual: row.Actual == "Home win" ? row.PHome : row.Actual == "Away win" ? row.PAway : row.PDraw))
            .OrderBy(x => x.pActual)
            .Take(8)
            .ToList();
        if (surprises.Count > 0)
        {
            var s = Ui.Table("[bold]Biggest surprises (model least expected)[/]");
            s.AddColumn("Match");
            s.AddColumn(new TableColumn("Score").Centered());
            s.AddColumn("Result");
            s.AddColumn(new TableColumn("Model gave it").RightAligned());
            foreach (var (row, pActual) in surprises)
            {
                s.AddRow(
                    $"{Markup.Escape(row.HomeName)} v {Markup.Escape(row.AwayName)}",
                    $"{row.HomeGoals}–{row.AwayGoals}",
                    Markup.Escape(row.Actual),
                    Ui.Pct(pActual));
            }

            AnsiConsole.Write(s);
        }

        AnsiConsole.MarkupLine($"[{Ui.Muted}]Brier/log-loss score the calibrated probabilities (not just the pick); a well-calibrated model's bands match the 'actually happened' column.[/]");
    }

    // --- 10. Compare two scenarios ---

    private void CompareScenarios()
    {
        var data = _session.Data;
        var kind = Nav.Show(new SelectionPrompt<string>()
            .Title("What do you want to compare?")
            .WrapAround()
            .AddChoices(
                "A team with vs without a player (injury / suspension what-if)",
                "Current vs Starting parameters"));

        SimulationParameters pa, pb;
        string labelA, labelB;
        Team? focus = null;

        if (kind.StartsWith("A team"))
        {
            focus = ConsoleHelpers.PickTeam(data, "Which team?");
            var player = Nav.Show(new SelectionPrompt<string>()
                .Title($"Rule out which {Markup.Escape(focus.Name)} player?")
                .PageSize(16)
                .WrapAround()
                .EnableSearch()
                .AddChoices(focus.Squad.Select(pl => $"{pl.Id} — {pl.Name} ({pl.Position})")));
            string id = player.Split(' ')[0];
            string name = focus.Squad.First(pl => pl.Id == id).Name;

            pa = _session.Current;
            pb = _session.Current.Clone();
            pb.UnavailablePlayers.Add(id);
            labelA = "Full strength";
            labelB = $"without {name}";
        }
        else
        {
            pa = _session.Current;
            pb = _session.Starting;
            labelA = "Current params";
            labelB = "Starting params";
        }

        long n = ConsoleHelpers.PromptIterations("How many tournaments per scenario?", 200_000);
        if (!ConsoleHelpers.ConfirmLongRun(n * 2)) return;

        IReadOnlyList<PlayedResult>? locked = _session.PlayedResults.Count > 0 &&
            Nav.Confirm($"Continue from the current real results ({_session.PlayedResults.Count} locked)?", true)
            ? _session.PlayedResults : null;

        AnsiConsole.Clear();
        Ui.RunConfig($"Compare: {labelA} vs {labelB}", pa.Label, pa.Global.Seed, "Fast", n);

        var a = ConsoleHelpers.RunWithProgress($"Scenario A — {labelA}", n, (c, ct) =>
            MonteCarloTournamentRunner.Run(data, pa, n, _session.IncludeThirdPlacePlayoff, locked, c, ct));
        var b = ConsoleHelpers.RunWithProgress($"Scenario B — {labelB}", n, (c, ct) =>
            MonteCarloTournamentRunner.Run(data, pb, n, _session.IncludeThirdPlacePlayoff, locked, c, ct));

        PrintComparison(a, b, labelA, labelB, focus?.Code);
        ConsoleHelpers.Pause();
    }

    private static void PrintComparison(TournamentMonteCarloReport a, TournamentMonteCarloReport b, string labelA, string labelB, string? focusCode)
    {
        var byCodeB = b.Teams.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

        Ui.Header($"Scenario comparison — {labelA} vs {labelB}");

        if (focusCode is not null && a.Teams.FirstOrDefault(t => t.Code == focusCode) is { } fa && byCodeB.TryGetValue(focusCode, out var fb))
        {
            var ft = Ui.Table($"[bold]{Markup.Escape(fa.Name)} — impact[/]");
            ft.AddColumn("Outcome");
            ft.AddColumn(new TableColumn(labelA).RightAligned());
            ft.AddColumn(new TableColumn(labelB).RightAligned());
            ft.AddColumn(new TableColumn("Change").RightAligned());
            void Row(string what, double x, double y) => ft.AddRow(what, Ui.Pct(x), Ui.Pct(y), Delta(y - x));
            Row("Win the title", fa.Champion, fb.Champion);
            Row("Reach the final", fa.ReachedFinal, fb.ReachedFinal);
            Row("Reach the semis", fa.ReachedSemi, fb.ReachedSemi);
            Row("Win the group", fa.TopGroup, fb.TopGroup);
            AnsiConsole.Write(ft);
            Ui.Blank();
        }

        // Biggest title-odds movers across all teams.
        var movers = a.Teams
            .Select(t => (t.Name, A: t.Champion, B: byCodeB.TryGetValue(t.Code, out var tb) ? tb.Champion : 0.0))
            .Select(x => (x.Name, x.A, x.B, D: x.B - x.A))
            .Where(x => Math.Abs(x.D) > 0.0005)
            .OrderByDescending(x => Math.Abs(x.D))
            .Take(10)
            .ToList();
        if (movers.Count > 0)
        {
            var mt = Ui.Table("[bold]Biggest title-odds movers[/]");
            mt.AddColumn("Team");
            mt.AddColumn(new TableColumn(labelA).RightAligned());
            mt.AddColumn(new TableColumn(labelB).RightAligned());
            mt.AddColumn(new TableColumn("Change").RightAligned());
            foreach (var m in movers)
            {
                mt.AddRow(Markup.Escape(m.Name), Ui.Pct(m.A), Ui.Pct(m.B), Delta(m.D));
            }

            AnsiConsole.Write(mt);
        }

        AnsiConsole.MarkupLine($"[{Ui.Muted}]Both scenarios ran {a.Iterations:N0} tournaments from the same seed, so differences are the effect of the change, not noise.[/]");
    }

    // --- 11. Bracket challenge ---

    private void BracketChallenge()
    {
        var data = _session.Data;
        Ui.Header("Bracket challenge — make your picks");
        var champ = ConsoleHelpers.PickTeam(data, "Your pick to WIN the World Cup:");
        var runner = ConsoleHelpers.PickTeam(data, "Your pick for RUNNER-UP (loses the final):");
        var dark = ConsoleHelpers.PickTeam(data, "Your DARK HORSE (to reach the semi-finals):");

        IReadOnlyList<PlayedResult>? locked = _session.PlayedResults.Count > 0 &&
            Nav.Confirm($"Grade against the current real results ({_session.PlayedResults.Count} locked)?", true)
            ? _session.PlayedResults : null;

        long n = ConsoleHelpers.PromptIterations("How many tournaments to grade your picks?", 500_000);
        if (!ConsoleHelpers.ConfirmLongRun(n)) return;

        var p = PromptParameterSet();
        AnsiConsole.Clear();
        Ui.RunConfig("Bracket challenge", p.Label, p.Global.Seed, "Fast", n);
        var report = ConsoleHelpers.RunWithProgress("Grading your bracket", n, (c, ct) =>
            MonteCarloTournamentRunner.Run(data, p, n, _session.IncludeThirdPlacePlayoff, locked, c, ct));

        var byCode = report.Teams.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        double Odds(Team team, Func<TeamTournamentOdds, double> sel) => byCode.TryGetValue(team.Code, out var o) ? sel(o) : 0.0;

        Ui.Header("Your bracket vs the model");
        var t = Ui.Table("[bold]Your picks[/]");
        t.AddColumn("Pick");
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("Model odds").RightAligned());
        t.AddColumn("Verdict");
        void Row(string label, Team team, double odds, double chalk)
        {
            string verdict = odds >= chalk ? $"[{Ui.Good}]chalk — model agrees[/]"
                : odds >= chalk / 3 ? "[gold1]live longshot[/]"
                : "[red3]bold call[/]";
            t.AddRow(label, Markup.Escape(team.Name), Ui.Pct(odds), verdict);
        }

        double cOdds = Odds(champ, o => o.Champion);
        double rOdds = Odds(runner, o => o.ReachedFinal);
        double dOdds = Odds(dark, o => o.ReachedSemi);
        Row("🏆 Champion", champ, cOdds, 0.12);
        Row("🥈 Runner-up (reach final)", runner, rOdds, 0.18);
        Row("🐴 Dark horse (reach semis)", dark, dOdds, 0.20);
        AnsiConsole.Write(t);

        double consensus = (cOdds + rOdds + dOdds) / 3.0;
        AnsiConsole.MarkupLine($"[{Ui.Accent}]Consensus score:[/] [bold]{consensus * 100:0}/100[/] [grey](higher = closer to the model's favourites; lower = braver picks)[/]");
        if (report.MostLikelyChampion is { } fav)
        {
            AnsiConsole.MarkupLine($"[{Ui.Muted}]The model's favourite is {Markup.Escape(fav.Name)} ({Ui.Pct(fav.Champion)}).[/]");
        }

        if (Nav.Confirm("Play out ONE tournament to see how your picks did in a single reality?", true))
        {
            var rng = new Xoshiro256(NextRunSeed());
            var sim = new TournamentSimulator(data, p, _session.IncludeThirdPlacePlayoff, locked).Simulate(Fidelity.Fast, ref rng);
            Ui.Blank();
            Ui.Header("One simulated reality");
            string Mark(bool b) => b ? $"[{Ui.Good}]✓[/]" : "[red3]✗[/]";
            string Name(string code) => code.Length > 0 ? data.Team(code).Name : "—";
            bool champHit = string.Equals(sim.ChampionCode, champ.Code, StringComparison.OrdinalIgnoreCase);
            bool runnerHit = string.Equals(sim.RunnerUpCode, runner.Code, StringComparison.OrdinalIgnoreCase);
            bool darkHit = sim.FurthestStage.TryGetValue(dark.Code, out var st) && Stages.Rank(st) >= Stages.Rank(Stage.SemiFinal);
            AnsiConsole.MarkupLine($"Champion: [bold]{Markup.Escape(Name(sim.ChampionCode))}[/] — you picked {Markup.Escape(champ.Name)} {Mark(champHit)}");
            AnsiConsole.MarkupLine($"Runner-up: [bold]{Markup.Escape(Name(sim.RunnerUpCode))}[/] — you picked {Markup.Escape(runner.Name)} {Mark(runnerHit)}");
            AnsiConsole.MarkupLine($"Dark horse {Markup.Escape(dark.Name)} reached the semis? {Mark(darkHit)}");
            int hits = (champHit ? 1 : 0) + (runnerHit ? 1 : 0) + (darkHit ? 1 : 0);
            AnsiConsole.MarkupLine($"[{Ui.Accent}]You scored {hits}/3 in this reality.[/]");
            OfferBracketDownload(sim, "bracket_challenge");
        }

        ConsoleHelpers.Pause();
    }

    // --- 12. Live matchday dashboard ---

    private void LiveDashboard()
    {
        var data = _session.Data;
        while (true)
        {
            if (_session.LiveConfigured)
            {
                AnsiConsole.Status().Start("Pulling the latest results…", _ => _session.RefreshResults());
                data = _session.Data;
            }

            AnsiConsole.Clear();
            Ui.Header("Live matchday dashboard");
            Ui.Info(_session.LiveDiagnostics);
            Ui.PredictedOn();

            var today = DateTime.Now.Date; // local calendar day, so evening games aren't pushed to tomorrow
            var todays = data.GroupSchedule.Where(f => f.KickoffUtc.ToLocalTime().Date == today).OrderBy(f => f.KickoffUtc).ToList();
            var playedByPair = new Dictionary<(string, string), PlayedResult>();
            foreach (var r in _session.PlayedResults)
            {
                playedByPair[Pair(r.HomeCode, r.AwayCode)] = r;
            }

            if (todays.Count == 0)
            {
                Ui.Warning("No fixtures are scheduled today (UTC). The dashboard shows today's games and live qualification.");
            }
            else
            {
                var tbl = Ui.Table("[bold]Today's games[/]");
                tbl.AddColumn("Kickoff");
                tbl.AddColumn(new TableColumn("Grp").Centered());
                tbl.AddColumn("Match");
                tbl.AddColumn(new TableColumn("Status").RightAligned());
                foreach (var f in todays)
                {
                    var home = data.Team(f.HomeCode);
                    var away = data.Team(f.AwayCode);
                    string status;
                    if (playedByPair.TryGetValue(Pair(f.HomeCode, f.AwayCode), out var pr))
                    {
                        bool same = string.Equals(pr.HomeCode, f.HomeCode, StringComparison.OrdinalIgnoreCase);
                        int hg = same ? pr.HomeGoals : pr.AwayGoals;
                        int ag = same ? pr.AwayGoals : pr.HomeGoals;
                        status = $"[bold]{hg}–{ag}[/] FT";
                    }
                    else
                    {
                        var fc = MonteCarloMatchRunner.RunFast(home, away, _session.Current, 20_000, !IsHost(home));
                        double top = Math.Max(fc.HomeWin, Math.Max(fc.Draw, fc.AwayWin));
                        string who = fc.HomeWin >= fc.Draw && fc.HomeWin >= fc.AwayWin ? home.Code
                            : fc.AwayWin >= fc.Draw && fc.AwayWin >= fc.HomeWin ? away.Code : "draw";
                        status = $"[grey]forecast[/] {Markup.Escape(who)} {Ui.Pct(top)}";
                    }

                    tbl.AddRow(
                        f.KickoffUtc.ToLocalTime().ToString("HH:mm"),
                        f.Group.ToString(),
                        $"{Markup.Escape(home.Name)} [grey]v[/] {Markup.Escape(away.Name)}",
                        status);
                }

                AnsiConsole.Write(tbl);

                foreach (var grp in todays.Select(f => f.Group).Distinct().OrderBy(c => c))
                {
                    var outlook = GroupPathAnalyzer.AnalyzeGroup(data, grp, _session.Current, _session.PlayedResults, 30_000, NextRunSeed());
                    Ui.Blank();
                    GroupPathFormatter.PrintGroupOutlook(outlook);
                }
            }

            var act = Nav.Show(new SelectionPrompt<string>()
                .Title("Dashboard")
                .WrapAround()
                .AddChoices("🔄 Refresh now (re-pull results)", "Back to main menu"));
            if (act.Contains("Back"))
            {
                return;
            }
        }
    }

    // --- Group qualification scenarios grid (sub-option of Group path) ---

    private void QualificationScenarios(TournamentData data, char group)
    {
        Team? focus = Nav.Confirm("Highlight one team's qualification in the grid?", false)
            ? ConsoleHelpers.PickTeamInGroup(data, group, "Highlight which team?")
            : null;

        ulong seed = NextRunSeed();
        var perms = GroupPathAnalyzer.Permutations(data, group, _session.PlayedResults, seed, focus?.Code);

        AnsiConsole.Clear();
        if (perms.Fixtures.Count == 0)
        {
            Ui.Warning($"Group {group} is already complete — there are no remaining results to vary.");
            ConsoleHelpers.Pause();
            return;
        }

        Ui.Header($"Group {group} — qualification scenarios");
        GroupPathFormatter.PrintPermutations(perms);

        Ui.Blank();
        if (Nav.Confirm("💾 Download the full scenarios grid as a styled HTML page and open it?", true))
        {
            string path = OutputFolder.Resolve($"group_{group}_scenarios.html");
            HtmlExporter.GroupPermutationsToHtml(perms, path);
            Ui.Success($"Wrote {path}");
            ConsoleHelpers.OpenInBrowser(path);
        }

        ConsoleHelpers.OfferExports($"group_{group}_scenarios",
            path => Exporters.ToJson(perms, path), null,
            path => HtmlExporter.GroupPermutationsToHtml(perms, path));
        ConsoleHelpers.Pause();
    }

    // --- 12. Load a saved tournament ---

    private void LoadSavedTournament()
    {
        string name = Nav.Ask("Snapshot file to load", "tournament.json");
        string path = OutputFolder.Find(name);
        if (!File.Exists(path))
        {
            Ui.Warning($"File not found: {path}");
            ConsoleHelpers.Pause();
            return;
        }

        TournamentResult result;
        try
        {
            result = TournamentSnapshot.Load(path);
        }
        catch (Exception ex)
        {
            Ui.Warning($"Could not load: {ex.Message}");
            ConsoleHelpers.Pause();
            return;
        }

        var data = _session.Data;
        AnsiConsole.Clear();
        string champ = result.ChampionCode.Length > 0 ? data.Team(result.ChampionCode).Name : "?";
        Ui.Header($"Loaded tournament — {champ} champions");
        Ui.PredictedOn();
        TournamentReportFormatter.PrintSinglePlaythrough(result, data);

        var agg = new TournamentStatsAggregator(data, _session.Current.Global.Mvp);
        agg.Add(result);
        var stats = agg.Build();
        Ui.Blank();
        StatsReportFormatter.Print(stats);

        Ui.Blank();
        if (Nav.Confirm("💾 Download the full report (bracket + stats) as an HTML bundle and open it?", true))
        {
            string dir = OutputFolder.Subdir($"worldcup_2026_{champ.Replace(' ', '_')}");
            string index = HtmlExporter.TournamentBundle(result, stats, data, dir);
            Ui.Success($"Wrote {index}");
            ConsoleHelpers.OpenInBrowser(index);
        }

        InspectGames(result, data);
        ConsoleHelpers.Pause();
    }

    private static string Delta(double d)
    {
        if (Math.Abs(d) < 0.0005) return "[grey]–[/]";
        string c = d > 0 ? Ui.Good : "red3";
        string sign = d > 0 ? "+" : "−";
        return $"[{c}]{sign}{Math.Abs(d) * 100:0.0} pts[/]";
    }
}
