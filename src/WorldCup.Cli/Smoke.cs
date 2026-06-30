using WorldCup.Data.Models;
using WorldCup.Engine.Calibration;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Stats;
using WorldCup.Engine.Tournament;
using WorldCup.Reporting;

namespace WorldCup.Cli;

/// <summary>
/// Non-interactive end-to-end check (run via <c>--smoke</c>): exercises every report path without
/// any prompts, so the full pipeline can be validated in CI or from a redirected console.
/// </summary>
public static class Smoke
{
    public static void Run(Session session)
    {
        var data = session.Data;
        var p = session.Starting;

        Ui.Banner();
        Ui.Info(session.ProviderDiagnostics);
        Ui.Info(session.LiveDiagnostics);
        Ui.Info(data.Teams.Any(t => t.IsSyntheticSquad) ? "Squads: synthetic" : "Squads: REAL players loaded");

        // 0) Next scheduled match (verifies the real fixture list).
        Ui.Header("Smoke 0 — next scheduled fixture");
        var playedPairs = session.PlayedResults
            .Select(r => string.CompareOrdinal(r.HomeCode, r.AwayCode) <= 0 ? (r.HomeCode, r.AwayCode) : (r.AwayCode, r.HomeCode))
            .ToHashSet();
        var now = DateTime.UtcNow;
        var upcoming = data.GroupSchedule
            .Where(f => !playedPairs.Contains(string.CompareOrdinal(f.HomeCode, f.AwayCode) <= 0 ? (f.HomeCode, f.AwayCode) : (f.AwayCode, f.HomeCode)))
            .OrderBy(f => f.KickoffUtc)
            .ToList();
        var next = upcoming.FirstOrDefault(f => f.KickoffUtc >= now.AddHours(-2)) ?? upcoming.LastOrDefault();
        if (next is not null)
        {
            Ui.Info($"Next: {data.Team(next.HomeCode).Name} v {data.Team(next.AwayCode).Name} " +
                    $"(Group {next.Group}, MD{next.Matchday}, {next.KickoffUtc:yyyy-MM-dd HH:mm} UTC)");
            LineupFormatter.PrintProjectedLineups(data.Team(next.HomeCode), data.Team(next.AwayCode), p);
        }

        // 1) Detailed single match.
        Ui.Header("Smoke 1 — detailed single match");
        var rng = new Xoshiro256(p.Global.Seed);
        var match = MatchSimulator.Simulate(data.Team("BRA"), data.Team("ENG"), Stage.Final, Fidelity.Detailed, p, ref rng, false);
        MatchReportFormatter.PrintDetailed(match, showEventLog: false);

        // 2) Fast single-match Monte Carlo.
        Ui.Header("Smoke 2 — fast match Monte Carlo (100k)");
        var mc = MonteCarloMatchRunner.RunFast(data.Team("ARG"), data.Team("FRA"), p, 100_000, true);
        MatchReportFormatter.PrintFastMonteCarlo(mc);

        // 3) Single detailed tournament playthrough + stats.
        Ui.Header("Smoke 3 — single detailed tournament");
        var sim = new TournamentSimulator(data, p);
        var trng = new Xoshiro256(p.Global.Seed);
        var tournament = sim.Simulate(Fidelity.Detailed, ref trng);
        TournamentReportFormatter.PrintSinglePlaythrough(tournament, data);
        var agg = new TournamentStatsAggregator(data, p.Global.Mvp);
        agg.Add(tournament);
        StatsReportFormatter.Print(agg.Build());

        // 4) Fast full-tournament Monte Carlo.
        Ui.Header("Smoke 4 — fast tournament Monte Carlo (200k)");
        var report = MonteCarloTournamentRunner.Run(data, p, 200_000);
        TournamentReportFormatter.PrintMonteCarlo(report);

        // 5) Calibration diagnostics.
        Ui.Header("Smoke 5 — calibration");
        ParametersFormatter.PrintCalibration(CalibrationRunner.Measure(data, p, 3_000));

        // 6) "World Cup as-is" — current-state tournament locking the real results.
        Ui.Header("Smoke 6 — current-state tournament (locks real results)");
        var locked = session.PlayedResults;
        var csSim = new TournamentSimulator(data, p, session.IncludeThirdPlacePlayoff, locked);
        var csRng = new Xoshiro256(p.Global.Seed);
        var cs = csSim.Simulate(Fidelity.Fast, ref csRng);
        Ui.Info($"Locked {locked.Count} real results; continued to a simulated champion: " +
                (cs.ChampionCode.Length > 0 ? data.Team(cs.ChampionCode).Name : "(n/a)"));

        // 7) "Current game" — full aggregate over 1,000,000 runs: most probable score + all stats.
        if (next is not null)
        {
            Ui.Header("Smoke 7 — current match aggregated over 1,000,000 runs (score + full stats)");
            var matchAgg = MonteCarloMatchRunner.RunAggregate(
                data.Team(next.HomeCode), data.Team(next.AwayCode), p, 1_000_000, Stage.Group, neutralVenue: true);
            MatchReportFormatter.PrintAggregate(matchAgg);
        }

        // 8) Group path to victory & defeat (current standings → one team's paths + HTML export).
        Ui.Header("Smoke 8 — group path to victory & defeat");
        var pathAnalysis = GroupPathAnalyzer.Analyze(
            data, 'C', "BRA", p, session.PlayedResults, 50_000, p.Global.Seed);
        GroupPathFormatter.Print(pathAnalysis);
        string pathHtml = Path.Combine(Path.GetTempPath(), "smoke_group_path.html");
        HtmlExporter.GroupPathToHtml(pathAnalysis, pathHtml);
        Ui.Info($"Wrote group-path HTML to {pathHtml} " +
                $"(win group {pathAnalysis.WinGroup:P1}, advance {pathAnalysis.AdvanceDirect:P1}, " +
                $"{pathAnalysis.VictoryScenarios.Count} victory / {pathAnalysis.DefeatScenarios.Count} defeat combos).");

        // 9) Run all scheduled games — forecast every remaining fixture (small N for the smoke).
        Ui.Header("Smoke 9 — run all scheduled games (forecast every remaining fixture)");
        var playedPairs9 = session.PlayedResults
            .Select(r => string.CompareOrdinal(r.HomeCode, r.AwayCode) <= 0 ? (r.HomeCode, r.AwayCode) : (r.AwayCode, r.HomeCode))
            .ToHashSet();
        var remaining9 = data.GroupSchedule
            .Where(f => !playedPairs9.Contains(string.CompareOrdinal(f.HomeCode, f.AwayCode) <= 0 ? (f.HomeCode, f.AwayCode) : (f.AwayCode, f.HomeCode)))
            .OrderBy(f => f.KickoffUtc)
            .Take(8) // keep the smoke quick
            .ToList();
        var forecasts9 = remaining9.Select(f =>
        {
            var report = MonteCarloMatchRunner.RunFast(data.Team(f.HomeCode), data.Team(f.AwayCode), p, 50_000, neutralVenue: true);
            return new ScheduledGameForecast(f.Group, f.Matchday, f.KickoffUtc, true, report);
        }).ToList();
        var batch9 = new ScheduledForecastReport(50_000, p.Label, p.Global.Seed, 0, forecasts9);
        MatchReportFormatter.PrintScheduledForecasts(batch9);
        string schedHtml = Path.Combine(Path.GetTempPath(), "smoke_scheduled_forecasts.html");
        HtmlExporter.ScheduledForecastsToHtml(batch9, schedHtml);
        Ui.Info($"Wrote scheduled-forecast HTML to {schedHtml} ({forecasts9.Count} games).");

        // 9b) Run knockout games — resolve the dated bracket from current results (projected where groups
        // are unfinished) and forecast a small slate (e.g. the Round of 32) with decisive advance odds.
        Ui.Header("Smoke 9b — run knockout games (resolve bracket + forecast advance odds)");
        var koSchedule = KnockoutScheduleResolver.Resolve(
            data, session.PlayedResults, p, p.Global.Seed, session.IncludeThirdPlacePlayoff);
        Ui.Info(koSchedule.AllGroupsComplete
            ? "All groups complete — Round-of-32 matchups are settled."
            : $"Group(s) {string.Join(", ", koSchedule.IncompleteGroups)} unfinished — those matchups are projected (※).");
        var koRunnable = koSchedule.Fixtures
            .Where(f => !f.Played && f.IsResolved && f.Stage == Stage.RoundOf32)
            .OrderBy(f => f.KickoffUtc)
            .Take(3) // keep the smoke quick (≈ one day of the R32)
            .ToList();
        var koGames = koRunnable.Select(f =>
        {
            var report = MonteCarloMatchRunner.RunAggregate(
                data.Team(f.HomeCode!), data.Team(f.AwayCode!), p, 4_000, f.Stage, neutralVenue: true);
            return new KnockoutGameForecast(f.MatchId, f.Stage, Stages.DisplayName(f.Stage), f.Label, f.KickoffUtc, f.Projected, report);
        }).ToList();
        var koBatch = new KnockoutForecastReport(
            4_000, p.Label, p.Global.Seed, 0, $"Round of 32 (first {koGames.Count})", koGames.Any(g => g.Projected), koGames);
        MatchReportFormatter.PrintKnockoutForecasts(koBatch);
        string koHtml = Path.Combine(Path.GetTempPath(), "smoke_knockout_forecasts.html");
        HtmlExporter.KnockoutForecastsToHtml(koBatch, koHtml);
        Ui.Info($"Wrote knockout-forecast HTML to {koHtml} ({koGames.Count} ties).");

        // 10) Play-by-play announcer transcript for a single match (+ sibling .txt written on HTML download).
        Ui.Header("Smoke 10 — play-by-play announcer commentary");
        var commRng = new Xoshiro256(p.Global.Seed ^ 0xC0FFEEUL);
        var commMatch = MatchSimulator.Simulate(data.Team("BRA"), data.Team("ARG"), Stage.SemiFinal, Fidelity.Detailed, p, ref commRng, true);
        var commentary = CommentaryGenerator.Generate(commMatch);
        MatchReportFormatter.PrintCommentary(commentary);
        string commHtml = Path.Combine(Path.GetTempPath(), "smoke_match.html");
        HtmlExporter.MatchResultToHtml(commMatch, commHtml); // also writes smoke_match_commentary.txt alongside
        Ui.Info($"Wrote {commHtml} and its sibling _commentary.txt transcript ({commentary.Count} lines).");

        // 11) Whole-group outlook — all four teams of a group analysed at once.
        Ui.Header("Smoke 11 — whole-group outlook (all four teams)");
        var groupOutlook = GroupPathAnalyzer.AnalyzeGroup(data, 'C', p, session.PlayedResults, 30_000, p.Global.Seed);
        GroupPathFormatter.PrintGroupOutlook(groupOutlook);
        string outlookHtml = Path.Combine(Path.GetTempPath(), "smoke_group_outlook.html");
        HtmlExporter.GroupOutlookToHtml(groupOutlook, outlookHtml);
        Ui.Info($"Wrote whole-group outlook HTML to {outlookHtml} ({groupOutlook.Teams.Count} teams).");

        // 12) Tournament knockout bracket HTML (the downloadable "who won, how it went" graphic).
        Ui.Header("Smoke 12 — tournament bracket HTML");
        string bracketHtml = Path.Combine(Path.GetTempPath(), "smoke_bracket.html");
        HtmlExporter.TournamentToHtml(tournament, data, bracketHtml);
        Ui.Info($"Wrote tournament bracket HTML to {bracketHtml} " +
                $"(champion {(tournament.ChampionCode.Length > 0 ? data.Team(tournament.ChampionCode).Name : "n/a")}).");

        Ui.Success("Smoke run complete.");
    }
}
