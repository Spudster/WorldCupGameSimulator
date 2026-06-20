using Spectre.Console;
using WorldCup.Data.Models;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Tournament;

namespace WorldCup.Reporting;

/// <summary>Formats group standings, the knockout bracket, single playthroughs and the MC odds report.</summary>
public static class TournamentReportFormatter
{
    public static void PrintSinglePlaythrough(TournamentResult result, TournamentData data)
    {
        Ui.Header("Group stage — final standings");
        var qualifiedThirdGroups = result.QualifiedThirds.Select(t => t.Group).ToHashSet();
        foreach (var group in data.Groups)
        {
            PrintGroup(group, result.GroupStandings[group], data, qualifiedThirdGroups);
        }

        PrintThirdPlaceRanking(result, data);

        Ui.Blank();
        PrintGroupResults(result, data);

        Ui.Blank();
        Ui.Header("Knockout bracket");
        PrintBracket(result, data);

        Ui.Blank();
        PrintChampion(result, data);

        Ui.Blank();
        PrintFinalPlacings(result, data);
    }

    /// <summary>
    /// Per-team "how far did they get" table: the stage each team reached / was eliminated in,
    /// from champion down to the group-stage exits.
    /// </summary>
    public static void PrintFinalPlacings(TournamentResult result, TournamentData data)
    {
        var rows = data.Teams
            .Select(t =>
            {
                var (rank, label, colour) = Placing(t.Code, result);
                return (Team: t, Rank: rank, Label: label, Colour: colour);
            })
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Team.Group)
            .ThenBy(x => x.Team.Name)
            .ToList();

        var table = Ui.Table("[bold]How far each team got[/]");
        table.AddColumn("#");
        table.AddColumn("Team");
        table.AddColumn(new TableColumn("Grp").Centered());
        table.AddColumn("Result / eliminated in");

        int pos = 1;
        foreach (var row in rows)
        {
            table.AddRow(
                pos.ToString(),
                $"[{row.Colour}]{Markup.Escape(row.Team.Name)}[/]",
                row.Team.Group.ToString(),
                $"[{row.Colour}]{Markup.Escape(row.Label)}[/]");
            pos++;
        }

        AnsiConsole.Write(table);
    }

    private static (int Rank, string Label, string Colour) Placing(string code, TournamentResult result)
    {
        if (string.Equals(code, result.ChampionCode, StringComparison.OrdinalIgnoreCase))
        {
            return (0, "🏆 Champion", "gold1");
        }

        if (string.Equals(code, result.RunnerUpCode, StringComparison.OrdinalIgnoreCase))
        {
            return (1, "Runner-up (lost final)", "grey85");
        }

        if (string.Equals(code, result.ThirdPlaceCode, StringComparison.OrdinalIgnoreCase))
        {
            return (2, "Third place", "darkorange3");
        }

        Stage stage = result.FurthestStage.TryGetValue(code, out var s) ? s : Stage.Group;
        return stage switch
        {
            Stage.SemiFinal or Stage.ThirdPlacePlayoff => (3, "Fourth (lost semi-final)", "grey70"),
            Stage.QuarterFinal => (4, "Quarter-finals", "grey70"),
            Stage.RoundOf16 => (5, "Round of 16", "grey58"),
            Stage.RoundOf32 => (6, "Round of 32", "grey46"),
            _ => (7, "Group stage", "grey37"),
        };
    }

    private static void PrintGroup(
        char group, IReadOnlyList<TeamStanding> table, TournamentData data, HashSet<char> qualifiedThirdGroups)
    {
        var t = Ui.Table($"[bold]Group {group}[/]");
        t.AddColumn("#");
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("P").Centered());
        t.AddColumn(new TableColumn("W").Centered());
        t.AddColumn(new TableColumn("D").Centered());
        t.AddColumn(new TableColumn("L").Centered());
        t.AddColumn(new TableColumn("GF").Centered());
        t.AddColumn(new TableColumn("GA").Centered());
        t.AddColumn(new TableColumn("GD").Centered());
        t.AddColumn(new TableColumn("Pts").Centered());

        foreach (var s in table)
        {
            string name = data.Team(s.Code).Name;
            bool advances = s.Rank <= 2 || (s.Rank == 3 && qualifiedThirdGroups.Contains(group));
            string marker = s.Rank <= 2 ? "[green3]●[/]" : s.Rank == 3 && qualifiedThirdGroups.Contains(group) ? "[gold1]◐[/]" : " ";
            string flag = Flags.Of(s.Code);
            string label = (flag.Length > 0 ? flag + " " : "") + Markup.Escape(name);
            string teamCell = advances ? $"[bold]{label}[/]" : label;
            t.AddRow(
                $"{marker} {s.Rank}",
                teamCell,
                s.Played.ToString(), s.Won.ToString(), s.Drawn.ToString(), s.Lost.ToString(),
                s.GoalsFor.ToString(), s.GoalsAgainst.ToString(),
                (s.GoalDifference >= 0 ? "+" : "") + s.GoalDifference,
                $"[bold]{s.Points}[/]");
        }

        AnsiConsole.Write(t);
    }

    private static void PrintThirdPlaceRanking(TournamentResult result, TournamentData data)
    {
        var t = Ui.Table("[bold]Best third-placed teams[/]");
        t.AddColumn("#");
        t.AddColumn("Team");
        t.AddColumn("Grp");
        t.AddColumn(new TableColumn("Pts").Centered());
        t.AddColumn(new TableColumn("GD").Centered());
        t.AddColumn(new TableColumn("GF").Centered());
        t.AddColumn("Qualified");

        int rank = 1;
        foreach (var s in result.QualifiedThirds.Concat(result.EliminatedThirds))
        {
            bool q = rank <= 8;
            t.AddRow(
                rank.ToString(),
                (q ? $"[bold]{Markup.Escape(data.Team(s.Code).Name)}[/]" : Markup.Escape(data.Team(s.Code).Name)),
                s.Group.ToString(),
                s.Points.ToString(),
                (s.GoalDifference >= 0 ? "+" : "") + s.GoalDifference,
                s.GoalsFor.ToString(),
                q ? "[green3]✓[/]" : "[grey]—[/]");
            rank++;
        }

        AnsiConsole.Write(t);
    }

    /// <summary>Lists every group-stage game played, grouped by group (chronological within each).</summary>
    public static void PrintGroupResults(TournamentResult result, TournamentData data)
    {
        Ui.Header("Group-stage results", $"{result.GroupResults.Count} matches played");
        var table = Ui.Table();
        table.AddColumn(new TableColumn("Grp").Centered());
        table.AddColumn(new TableColumn("Home").RightAligned());
        table.AddColumn(new TableColumn("Score").Centered());
        table.AddColumn("Away");

        foreach (var m in result.GroupResults.OrderBy(m => data.Team(m.HomeCode).Group))
        {
            char g = data.Team(m.HomeCode).Group;
            bool homeWon = m.WinnerCode == m.HomeCode;
            bool awayWon = m.WinnerCode == m.AwayCode;
            string homeCell = homeWon ? $"[bold]{Markup.Escape(m.HomeName)}[/]" : Markup.Escape(m.HomeName);
            string awayCell = awayWon ? $"[bold]{Markup.Escape(m.AwayName)}[/]" : Markup.Escape(m.AwayName);
            string score = $"{m.HomeGoals}-{m.AwayGoals}" + (m.IsLocked ? " [grey](real)[/]" : string.Empty);
            table.AddRow(g.ToString(), homeCell, score, awayCell);
        }

        AnsiConsole.Write(table);
    }

    public static void PrintBracket(TournamentResult result, TournamentData data)
    {
        foreach (var stage in new[] { Stage.RoundOf32, Stage.RoundOf16, Stage.QuarterFinal, Stage.SemiFinal, Stage.ThirdPlacePlayoff, Stage.Final })
        {
            var matches = result.KnockoutResults.Where(k => k.Stage == stage).ToList();
            if (matches.Count == 0)
            {
                continue;
            }

            var t = Ui.Table($"[{Ui.Accent}]{Stages.DisplayName(stage)}[/]");
            t.AddColumn("Match");
            t.AddColumn(new TableColumn("Home").RightAligned());
            t.AddColumn(new TableColumn("Score").Centered());
            t.AddColumn("Away");

            foreach (var k in matches)
            {
                var r = k.Result;
                string score = $"{r.HomeGoals}-{r.AwayGoals}";
                if (r.Method == MatchMethod.ExtraTime) score += " aet";
                else if (r.Method == MatchMethod.Penalties) score += $" ({r.HomePens}-{r.AwayPens}p)";

                string homeName = data.Team(r.HomeCode).Name;
                string awayName = data.Team(r.AwayCode).Name;
                bool homeWon = r.WinnerCode == r.HomeCode;
                t.AddRow(
                    $"[{Ui.Muted}]{k.Label}[/]",
                    homeWon ? $"[bold]{Markup.Escape(homeName)}[/]" : Markup.Escape(homeName),
                    score,
                    !homeWon ? $"[bold]{Markup.Escape(awayName)}[/]" : Markup.Escape(awayName));
            }

            AnsiConsole.Write(t);
        }
    }

    private static void PrintChampion(TournamentResult result, TournamentData data)
    {
        if (string.IsNullOrEmpty(result.ChampionCode))
        {
            return;
        }

        string champ = data.Team(result.ChampionCode).Name;
        string runnerUp = string.IsNullOrEmpty(result.RunnerUpCode) ? "" : data.Team(result.RunnerUpCode).Name;
        var lines = $"[{Ui.Gold}]🏆 CHAMPION: {Markup.Escape(champ).ToUpperInvariant()}[/]";
        if (runnerUp.Length > 0)
        {
            lines += $"\n[{Ui.Muted}]Runner-up: {Markup.Escape(runnerUp)}[/]";
        }

        if (!string.IsNullOrEmpty(result.ThirdPlaceCode))
        {
            lines += $"\n[{Ui.Muted}]Third place: {Markup.Escape(data.Team(result.ThirdPlaceCode).Name)}[/]";
        }

        Ui.Hero(lines, "World Champions", Ui.GoldColor);
    }

    public static void PrintMonteCarlo(TournamentMonteCarloReport r)
    {
        var champ = r.MostLikelyChampion;
        var final = r.MostLikelyFinal;
        if (champ is not null)
        {
            string summary = $"[{Ui.Gold}]Most likely champion: {Markup.Escape(champ.Name)} ({Ui.Pct(champ.Champion)})[/]";
            if (final is not null)
            {
                summary += $"\n[{Ui.Muted}]Most likely final: {Markup.Escape(final.NameA)} v {Markup.Escape(final.NameB)} ({Ui.Pct(final.Probability)})[/]";
            }

            Ui.Hero(summary, "Tournament forecast", Ui.GoldColor);
        }

        var table = Ui.Table("[bold]Title & advancement probabilities[/]");
        table.AddColumn(new TableColumn("Team").NoWrap());
        table.AddColumn(new TableColumn("Grp").Centered());
        table.AddColumn(new TableColumn("Champ").RightAligned());
        table.AddColumn(new TableColumn("Final").RightAligned());
        table.AddColumn(new TableColumn("SF").RightAligned());
        table.AddColumn(new TableColumn("QF").RightAligned());
        table.AddColumn(new TableColumn("Top").RightAligned());
        table.AddColumn(new TableColumn("xPts").RightAligned());

        foreach (var o in r.Teams.Where(t => t.ReachedR32 > 0.0005 || t.Champion > 0).Take(32))
        {
            table.AddRow(
                $"{(Flags.Of(o.Code) is var fl && fl.Length > 0 ? fl + " " : "")}[bold]{Markup.Escape(o.Name)}[/]",
                $"[{Ui.Muted}]{o.Group}[/]",
                Ui.Heat(o.Champion),
                Ui.Heat(o.ReachedFinal),
                Ui.Heat(o.ReachedSemi),
                Ui.Heat(o.ReachedQuarter),
                Ui.Heat(o.TopGroup),
                $"{o.ExpectedGroupPoints:0.0}");
        }

        AnsiConsole.Write(table);

        if (r.TopFinalMatchups.Count > 0)
        {
            var ft = Ui.Table("[bold]Most likely final matchups[/]");
            ft.AddColumn("Matchup");
            ft.AddColumn(new TableColumn("Probability").RightAligned());
            foreach (var f in r.TopFinalMatchups.Take(8))
            {
                ft.AddRow($"{Markup.Escape(f.NameA)} v {Markup.Escape(f.NameB)}", Ui.Pct2(f.Probability));
            }

            AnsiConsole.Write(ft);
        }

        AnsiConsole.MarkupLine($"[{Ui.Muted}]{r.Iterations:N0} tournaments in {r.ElapsedSeconds:0.00}s · {Ui.Throughput(r.SimsPerSecond)}{(r.CurrentState ? " · current-state" : "")}[/]");
    }
}
