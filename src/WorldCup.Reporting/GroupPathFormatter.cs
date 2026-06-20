using Spectre.Console;
using WorldCup.Engine.Tournament;

namespace WorldCup.Reporting;

/// <summary>Console rendering for the group "path to victory / path to defeat" analysis.</summary>
public static class GroupPathFormatter
{
    /// <summary>The qualification-scenarios grid: every remaining-results combination → who qualifies.</summary>
    public static void PrintPermutations(GroupPermutations g)
    {
        var t = Ui.Table($"[bold]Group {g.Group} — who qualifies under each result[/]");
        foreach (var f in g.Fixtures)
        {
            t.AddColumn(new TableColumn($"{f.HomeCode} v {f.AwayCode}").Centered());
        }

        t.AddColumn(new TableColumn($"[{Ui.Good}]1st[/]").Centered());
        t.AddColumn(new TableColumn($"[{Ui.Good}]2nd[/]").Centered());
        t.AddColumn(new TableColumn("3rd").Centered());

        const int cap = 60;
        int shown = 0;
        foreach (var row in g.Rows)
        {
            if (shown++ >= cap)
            {
                break;
            }

            var cells = new List<string>();
            for (int i = 0; i < g.Fixtures.Count; i++)
            {
                var f = g.Fixtures[i];
                cells.Add(row.Outcomes[i] switch { 1 => f.HomeCode, -1 => f.AwayCode, _ => "[grey]draw[/]" });
            }

            cells.Add(Qualifier(row.FirstCode, g.SelectedCode));
            cells.Add(Qualifier(row.SecondCode, g.SelectedCode));
            cells.Add($"[{Ui.Muted}]{Markup.Escape(row.ThirdCode)}[/]");
            t.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(t);
        if (g.Rows.Count > cap)
        {
            AnsiConsole.MarkupLine($"[{Ui.Muted}]Showing the first {cap} of {g.TotalCombinations} combinations — download the HTML for all of them.[/]");
        }

        AnsiConsole.MarkupLine($"[{Ui.Muted}]{g.TotalCombinations} combination(s) of {g.Fixtures.Count} remaining game(s). Hypothetical games use 1–0 / 0–0 / 0–1 margins; real scores can shuffle close ties.[/]");
    }

    private static string Qualifier(string code, string? selected)
    {
        bool hit = selected is not null && string.Equals(code, selected, StringComparison.OrdinalIgnoreCase);
        return hit ? $"[{Ui.Gold}]{Markup.Escape(code)} ◀[/]" : $"[{Ui.Good}]{Markup.Escape(code)}[/]";
    }

    public static void Print(GroupPathAnalysis a)
    {
        Ui.Header($"Group {a.Group} — path to victory & defeat", Flags.Named(a.TeamCode, a.TeamName));
        Ui.Blank();

        PrintStandings(a);
        Ui.Blank();

        if (a.GroupComplete)
        {
            Ui.Hero(
                $"[{Ui.Gold}]Group {a.Group} is complete[/]\n" +
                $"[{Ui.Muted}]{Markup.Escape(a.TeamName)} finished {Ordinal(a.FinalRankIfComplete)} — " +
                $"{TierWord(a.FinalRankIfComplete)}.[/]",
                "Final position", Ui.GoldColor);
            return;
        }

        PrintRemainingFixtures(a);
        Ui.Blank();
        PrintOutlook(a);

        if (a.OwnResultBranches.Count > 0)
        {
            Ui.Blank();
            PrintOwnResultBranches(a);
        }

        Ui.Blank();
        PrintScenarios("🏆 PATH TO VICTORY — combinations that WIN the group", a.VictoryScenarios, a.VictoryMass, a, Ui.GoodColor);
        Ui.Blank();
        PrintScenarios("❌ PATH TO DEFEAT — combinations that finish LAST (eliminated)", a.DefeatScenarios, a.DefeatMass, a, Color.Red3);
    }

    private static void PrintStandings(GroupPathAnalysis a)
    {
        var table = Ui.Table($"[bold]Group {a.Group} — current standings[/]");
        table.AddColumn("#");
        table.AddColumn("Team");
        table.AddColumn(new TableColumn("P").Centered());
        table.AddColumn(new TableColumn("W").Centered());
        table.AddColumn(new TableColumn("D").Centered());
        table.AddColumn(new TableColumn("L").Centered());
        table.AddColumn(new TableColumn("GF").Centered());
        table.AddColumn(new TableColumn("GA").Centered());
        table.AddColumn(new TableColumn("GD").Centered());
        table.AddColumn(new TableColumn("Pts").RightAligned());

        foreach (var s in a.Standings)
        {
            // Top two advance directly; mark the qualification line and the selected team.
            string colour = s.Rank <= 2 ? Ui.Good : Ui.Muted;
            string name = Flags.Named(s.Code, s.Name);
            string nameCell = s.IsSelected
                ? $"[{Ui.Gold}]➤ {Markup.Escape(name)}[/]"
                : $"[{colour}]{Markup.Escape(name)}[/]";
            string gd = (s.GoalDifference >= 0 ? "+" : "") + s.GoalDifference;
            table.AddRow(
                s.Rank.ToString(), nameCell, s.Played.ToString(), s.Won.ToString(), s.Drawn.ToString(),
                s.Lost.ToString(), s.GoalsFor.ToString(), s.GoalsAgainst.ToString(), gd,
                $"[bold]{s.Points}[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Top two qualify directly · 3rd may advance as one of the eight best third-placed teams.[/]");
    }

    private static void PrintRemainingFixtures(GroupPathAnalysis a) => PrintRemainingFixtures(a.RemainingFixtures);

    private static void PrintRemainingFixtures(IReadOnlyList<RemainingFixtureOdds> fixtures)
    {
        var table = Ui.Table("[bold]Remaining group fixtures — win/draw/win odds[/]");
        table.AddColumn("Fixture");
        table.AddColumn(new TableColumn("Home").RightAligned());
        table.AddColumn(new TableColumn("Draw").Centered());
        table.AddColumn(new TableColumn("Away").RightAligned());

        foreach (var f in fixtures)
        {
            string names = $"{Flags.Named(f.HomeCode, f.HomeName)}  v  {Flags.Named(f.AwayCode, f.AwayName)}";
            string cell = (f.InvolvesSelected ? $"[{Ui.Gold}]➤[/] " : "") + Markup.Escape(names);
            table.AddRow(cell, Ui.Heat(f.HomeWin), Ui.Heat(f.Draw), Ui.Heat(f.AwayWin));
        }

        AnsiConsole.Write(table);
    }

    /// <summary>Whole-group overview: all four teams' standings and finishing probabilities in one table.</summary>
    public static void PrintGroupOutlook(GroupOutlook g)
    {
        Ui.Header($"Group {g.Group} — full outlook",
            g.GroupComplete ? "group complete" : $"all four teams · {g.Iterations:N0} simulations");
        Ui.Blank();

        var table = Ui.Table($"[bold]Group {g.Group} — standings & finishing probabilities[/]");
        table.AddColumn("#");
        table.AddColumn("Team");
        table.AddColumn(new TableColumn("Pts").RightAligned());
        table.AddColumn(new TableColumn("🥇 Win").RightAligned());
        table.AddColumn(new TableColumn("🥈 2nd").RightAligned());
        table.AddColumn(new TableColumn("✅ Adv").RightAligned());
        table.AddColumn(new TableColumn("⚠ 3rd").RightAligned());
        table.AddColumn(new TableColumn("❌ Out").RightAligned());
        table.AddColumn("Status");

        foreach (var t in g.Teams)
        {
            var s = t.Standing;
            string colour = s.Rank <= 2 ? Ui.Good : Ui.Muted;
            string name = $"[{colour}]{Markup.Escape(Flags.Named(s.Code, s.Name))}[/]";
            string outCell = t.Eliminated > 0 ? $"[{Ui.Bad}]{Ui.Pct(t.Eliminated)}[/]" : Ui.Pct(t.Eliminated);
            table.AddRow(
                s.Rank.ToString(), name, $"[bold]{s.Points}[/]",
                Ui.Heat(t.WinGroup), Ui.Heat(t.RunnerUp), Ui.Heat(t.AdvanceDirect),
                Ui.Heat(t.ThirdPlace), outCell, StatusMarkup(t));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Win/2nd/Adv = qualify directly · 3rd may advance as one of the eight best third-placed teams.[/]");

        if (!g.GroupComplete && g.RemainingFixtures.Count > 0)
        {
            Ui.Blank();
            PrintRemainingFixtures(g.RemainingFixtures);
        }
    }

    private static string StatusMarkup(GroupTeamOutlook t)
    {
        string c = t.ClinchedWinGroup || t.ClinchedAdvance ? Ui.Good
            : t.Status == "Eliminated" ? Ui.Bad
            : t.CannotAdvance ? Ui.Warn
            : Ui.Muted;
        return $"[{c}]{Markup.Escape(t.Status)}[/]";
    }

    private static void PrintOutlook(GroupPathAnalysis a)
    {
        string clinch = a.ClinchedWinGroup ? $"\n[{Ui.Good}]✓ Already guaranteed top spot.[/]"
            : a.ClinchedAdvance ? $"\n[{Ui.Good}]✓ Already qualified for the knockouts.[/]"
            : a.CannotAdvance ? $"\n[{Ui.Bad}]✗ Can no longer finish in the top two.[/]"
            : a.CannotWinGroup ? $"\n[{Ui.Warn}]! Can no longer win the group.[/]"
            : "";

        Ui.Hero(
            $"[{Ui.Gold}]How {Markup.Escape(a.TeamName)} finish — {a.Iterations:N0} simulations[/]\n" +
            $"🥇 Win group   [bold]{Ui.Pct(a.WinGroup)}[/]\n" +
            $"🥈 Runner-up   [bold]{Ui.Pct(a.RunnerUp)}[/]\n" +
            $"✅ Advance (top 2)   [bold]{Ui.Pct(a.AdvanceDirect)}[/]\n" +
            $"⚠️  Finish 3rd (best-third lottery)   [bold]{Ui.Pct(a.ThirdPlace)}[/]\n" +
            $"❌ Eliminated (last)   [bold]{Ui.Pct(a.Eliminated)}[/]" +
            clinch,
            "Outlook", Ui.GoldColor);
    }

    private static void PrintOwnResultBranches(GroupPathAnalysis a)
    {
        string heading = a.OwnRemaining == 1
            ? "[bold]What it means for your last group game[/]"
            : "[bold]What your remaining games need to yield[/]";
        var table = Ui.Table(heading);
        table.AddColumn("Your result");
        table.AddColumn(new TableColumn("Chance").RightAligned());
        table.AddColumn(new TableColumn("→ Win group").RightAligned());
        table.AddColumn(new TableColumn("→ Advance").RightAligned());
        table.AddColumn(new TableColumn("→ Last").RightAligned());
        table.AddColumn("Verdict");

        foreach (var br in a.OwnResultBranches)
        {
            table.AddRow(
                $"[bold]{Markup.Escape(br.Label)}[/]",
                Ui.Pct(br.Probability),
                Ui.Heat(br.WinGroup),
                Ui.Heat(br.Advance),
                br.Eliminated > 0 ? $"[{Ui.Bad}]{Ui.Pct(br.Eliminated)}[/]" : Ui.Pct(br.Eliminated),
                Markup.Escape(br.Verdict));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Chance = how likely your result is · the arrows are conditional on it (other matches still vary).[/]");
    }

    private static void PrintScenarios(
        string title, IReadOnlyList<GroupPathScenario> scenarios, double mass, GroupPathAnalysis a, Color border)
    {
        if (scenarios.Count == 0)
        {
            Ui.Hero($"[{Ui.Muted}]No combination of the remaining results leads here — it is off the table.[/]",
                title, border);
            return;
        }

        var table = Ui.Table($"[bold]{Markup.Escape(title)}[/]");
        table.AddColumn("If…");
        table.AddColumn(new TableColumn("Finish").Centered());
        table.AddColumn(new TableColumn("Likelihood").RightAligned());

        foreach (var s in scenarios.Take(10))
        {
            string combo = string.Join("  ·  ", s.Outcomes.Select(o => o.Description));
            string finish = s.BestRank == s.WorstRank
                ? Ordinal(s.BestRank)
                : $"{Ordinal(s.BestRank)}–{Ordinal(s.WorstRank)}*";
            table.AddRow(Markup.Escape(combo), finish, Ui.Pct(s.Probability));
        }

        AnsiConsole.Write(table);
        string note = scenarios.Count > 10 ? $" (showing top 10 of {scenarios.Count})" : "";
        AnsiConsole.MarkupLine(
            $"[{Ui.Muted}]{scenarios.Count} combination(s){note} · together ≈ {Ui.Pct(mass)} likely. " +
            $"* = place then settled on goal difference / head-to-head.[/]");
    }

    private static string Ordinal(int rank) => rank switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{rank}th",
    };

    private static string TierWord(int rank) => rank switch
    {
        1 => "won the group",
        2 => "qualified as runner-up",
        3 => "finished 3rd (into the best-third lottery)",
        _ => "eliminated",
    };
}
