using Spectre.Console;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Stats;

namespace WorldCup.Reporting;

/// <summary>Formats the detailed-mode leaderboards, awards, crazy-stats records and injury list.</summary>
public static class StatsReportFormatter
{
    public static void Print(StatsReport r)
    {
        bool aggregated = r.Tournaments > 1;
        Ui.Header("Awards & leaderboards", aggregated ? $"Aggregated across {r.Tournaments:N0} detailed tournaments" : "Single tournament");

        PrintGoldenBoot(r);
        PrintAssists(r);
        PrintMvp(r);
        PrintGoldenGlove(r);
        PrintDiscipline(r);
        PrintTeams(r);
        PrintPenaltyTakers(r);
        PrintGoalsByType(r);
        PrintRecords(r);
        PrintInjuries(r);

        if (aggregated)
        {
            PrintAwardFrequency("Golden Boot wins", r.GoldenBootFrequency, "goals");
            PrintAwardFrequency("MVP / Golden Ball wins", r.MvpFrequency, "score");
            PrintAwardFrequency("Golden Glove wins", r.GoldenGloveFrequency, "clean sheets");
        }
    }

    private static void PrintGoldenBoot(StatsReport r)
    {
        if (r.GoldenBoot.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[gold1]🥇 Golden Boot[/]");
        t.AddColumn("#");
        t.AddColumn("Player");
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("Goals").RightAligned());
        t.AddColumn(new TableColumn("Assists").RightAligned());
        t.AddColumn(new TableColumn("Mins").RightAligned());
        int rank = 1;
        foreach (var p in r.GoldenBoot)
        {
            t.AddRow((rank++).ToString(), Markup.Escape(p.Name), Markup.Escape(p.TeamCode),
                $"[bold]{p.Goals}[/]", p.Assists.ToString(), p.Minutes.ToString());
        }

        AnsiConsole.Write(t);
    }

    private static void PrintAssists(StatsReport r)
    {
        if (r.TopAssists.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[bold]Most assists[/]");
        t.AddColumn("#");
        t.AddColumn("Player");
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("Assists").RightAligned());
        t.AddColumn(new TableColumn("Goals").RightAligned());
        int rank = 1;
        foreach (var p in r.TopAssists)
        {
            t.AddRow((rank++).ToString(), Markup.Escape(p.Name), Markup.Escape(p.TeamCode), $"[bold]{p.Assists}[/]", p.Goals.ToString());
        }

        AnsiConsole.Write(t);
    }

    private static void PrintMvp(StatsReport r)
    {
        if (r.Mvp.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[gold1]⭐ MVP / Golden Ball[/]");
        t.AddColumn("#");
        t.AddColumn("Player");
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("Score").RightAligned());
        t.AddColumn(new TableColumn("G").RightAligned());
        t.AddColumn(new TableColumn("A").RightAligned());
        t.AddColumn(new TableColumn("CS").RightAligned());
        t.AddColumn("Reached");
        int rank = 1;
        foreach (var p in r.Mvp)
        {
            t.AddRow((rank++).ToString(), Markup.Escape(p.Name), Markup.Escape(p.TeamCode),
                $"[bold]{p.Score:0.0}[/]", p.Goals.ToString(), p.Assists.ToString(), p.CleanSheets.ToString(),
                Markup.Escape(p.FurthestStage));
        }

        AnsiConsole.Write(t);
    }

    private static void PrintGoldenGlove(StatsReport r)
    {
        if (r.GoldenGlove.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[bold]🧤 Golden Glove[/]");
        t.AddColumn("#");
        t.AddColumn("Goalkeeper");
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("Clean sheets").RightAligned());
        t.AddColumn(new TableColumn("Saves").RightAligned());
        t.AddColumn(new TableColumn("Amazing").RightAligned());
        t.AddColumn(new TableColumn("Conceded").RightAligned());
        int rank = 1;
        foreach (var p in r.GoldenGlove)
        {
            string amazing = p.AmazingSaves > 0 ? $"[gold1]{p.AmazingSaves} 🔥[/]" : "0";
            t.AddRow((rank++).ToString(), Markup.Escape(p.Name), Markup.Escape(p.TeamCode),
                $"[bold]{p.CleanSheets}[/]", p.Saves.ToString(), amazing, p.GoalsConceded.ToString());
        }

        AnsiConsole.Write(t);
    }

    private static void PrintDiscipline(StatsReport r)
    {
        if (r.MostYellows.Count == 0 && r.MostReds.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[bold]Discipline[/]");
        t.AddColumn("Most yellows");
        t.AddColumn(new TableColumn("Y").RightAligned());
        t.AddColumn("Most reds");
        t.AddColumn(new TableColumn("R").RightAligned());
        int rows = Math.Max(r.MostYellows.Count, r.MostReds.Count);
        for (int i = 0; i < Math.Min(rows, 8); i++)
        {
            string yName = i < r.MostYellows.Count ? $"{r.MostYellows[i].Name} ({r.MostYellows[i].TeamCode})" : "";
            string yVal = i < r.MostYellows.Count ? r.MostYellows[i].Yellows.ToString() : "";
            string rName = i < r.MostReds.Count ? $"{r.MostReds[i].Name} ({r.MostReds[i].TeamCode})" : "";
            string rVal = i < r.MostReds.Count ? r.MostReds[i].Reds.ToString() : "";
            t.AddRow(Markup.Escape(yName), yVal, Markup.Escape(rName), rVal);
        }

        AnsiConsole.Write(t);
    }

    private static void PrintTeams(StatsReport r)
    {
        if (r.Teams.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[bold]Team stats[/]");
        t.AddColumn(new TableColumn("Team").NoWrap());
        t.AddColumn(new TableColumn("GF").RightAligned());
        t.AddColumn(new TableColumn("GA").RightAligned());
        t.AddColumn(new TableColumn("CS").RightAligned());
        t.AddColumn(new TableColumn("Shots").RightAligned());
        t.AddColumn(new TableColumn("Acc%").RightAligned());
        t.AddColumn(new TableColumn("Corners").RightAligned());
        t.AddColumn(new TableColumn("Poss").RightAligned());
        t.AddColumn(new TableColumn("Y").RightAligned());
        t.AddColumn(new TableColumn("R").RightAligned());
        foreach (var team in r.Teams.Take(16))
        {
            string acc = team.Shots > 0 ? $"{100.0 * team.ShotsOnTarget / team.Shots:0}%" : "—";
            t.AddRow(Markup.Escape(team.Name), team.GoalsFor.ToString(), team.GoalsAgainst.ToString(),
                team.CleanSheets.ToString(), team.Shots.ToString(), acc, team.Corners.ToString(),
                $"{team.PossessionAvg:0}%", team.Yellows.ToString(), team.Reds.ToString());
        }

        AnsiConsole.Write(t);
    }

    private static void PrintPenaltyTakers(StatsReport r)
    {
        if (r.PenaltyTakers.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[bold]⚽ Penalty takers[/]");
        t.AddColumn("#");
        t.AddColumn("Player");
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("Scored").RightAligned());
        t.AddColumn(new TableColumn("Missed").RightAligned());
        t.AddColumn(new TableColumn("Record").RightAligned());
        int rank = 1;
        foreach (var p in r.PenaltyTakers)
        {
            int total = p.Scored + p.Missed;
            string pct = total > 0 ? $"{100.0 * p.Scored / total:0}%" : "—";
            string missed = p.Missed > 0 ? $"[red3]{p.Missed}[/]" : "0";
            t.AddRow((rank++).ToString(), Markup.Escape(p.Name), Markup.Escape(p.TeamCode),
                $"[bold]{p.Scored}[/]", missed, pct);
        }

        AnsiConsole.Write(t);
    }

    private static void PrintGoalsByType(StatsReport r)
    {
        if (r.GoalsByType.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[bold]Goals by type[/]");
        t.AddColumn("Type");
        t.AddColumn(new TableColumn("Goals").RightAligned());
        t.AddColumn(new TableColumn("Share").RightAligned());
        foreach (var row in r.GoalsByType)
        {
            t.AddRow(Markup.Escape(row.Type), row.Count.ToString(), $"{row.Percent:0.0}%");
        }

        AnsiConsole.Write(t);
    }

    private static void PrintRecords(StatsReport r)
    {
        if (r.CrazyStats.Count == 0)
        {
            return;
        }

        var t = Ui.Table("[deepskyblue1]🤯 Crazy stats & records[/]");
        t.AddColumn("Record");
        t.AddColumn("Detail");
        foreach (var rec in r.CrazyStats)
        {
            t.AddRow($"[bold]{Markup.Escape(rec.Category)}[/]", Markup.Escape(rec.Description));
        }

        AnsiConsole.Write(t);
    }

    private static void PrintInjuries(StatsReport r)
    {
        if (r.TotalInjuries == 0)
        {
            return;
        }

        var t = Ui.Table($"[bold]Injury list[/] [grey]({r.TotalInjuries} total)[/]");
        t.AddColumn("Min");
        t.AddColumn("Player");
        t.AddColumn("Team");
        t.AddColumn("Diagnosis");
        t.AddColumn("Severity");
        t.AddColumn("Out for");
        t.AddColumn("Replaced");
        foreach (var inj in r.Injuries)
        {
            string diagnosis = string.IsNullOrEmpty(inj.Diagnosis) ? "—" : inj.Diagnosis;
            t.AddRow($"{inj.Minute}'", Markup.Escape(inj.PlayerName), Markup.Escape(inj.TeamCode),
                Markup.Escape(diagnosis), inj.Severity.ToString(),
                Markup.Escape(InjuryCatalog.RecoveryText(inj.RecoveryDays)), inj.Replaced ? "yes" : "[red3]no[/]");
        }

        AnsiConsole.Write(t);
    }

    private static void PrintAwardFrequency(string title, IReadOnlyList<AwardFrequencyRow> rows, string tallyLabel)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var t = Ui.Table($"[bold]{Markup.Escape(title)}[/]");
        t.AddColumn("Player");
        t.AddColumn("Team");
        t.AddColumn(new TableColumn("Win %").RightAligned());
        t.AddColumn(new TableColumn($"Avg {tallyLabel}").RightAligned());
        foreach (var row in rows.Take(10))
        {
            t.AddRow(Markup.Escape(row.Name), Markup.Escape(row.TeamCode), Ui.Pct(row.Frequency), $"{row.AverageTally:0.0}");
        }

        AnsiConsole.Write(t);
    }
}
