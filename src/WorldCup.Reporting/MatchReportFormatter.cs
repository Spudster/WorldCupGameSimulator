using Spectre.Console;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Tournament;

namespace WorldCup.Reporting;

/// <summary>Formats single-match results, fast-mode matchup Monte Carlo, and detailed aggregates.</summary>
public static class MatchReportFormatter
{
    public static void PrintDetailed(MatchResult m, bool showEventLog)
    {
        string method = m.Method switch
        {
            MatchMethod.ExtraTime => " (a.e.t.)",
            MatchMethod.Penalties => $" (pens {m.HomePens}-{m.AwayPens})",
            _ => string.Empty,
        };

        var scoreLine = $"{Flags.Of(m.HomeCode)} [bold]{Markup.Escape(m.HomeName)}[/]   [{Ui.Accent}]{m.HomeGoals} – {m.AwayGoals}[/]   [bold]{Markup.Escape(m.AwayName)}[/] {Flags.Of(m.AwayCode)}{method}";
        Ui.Hero(scoreLine, Stages.DisplayName(m.Stage), Ui.AccentColor);
        Ui.PredictedOn();

        if (!string.IsNullOrEmpty(m.WinnerCode))
        {
            string winnerName = m.WinnerCode == m.HomeCode ? m.HomeName : m.AwayName;
            AnsiConsole.MarkupLine($"[{Ui.Good}]Winner:[/] {Markup.Escape(winnerName)}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[{Ui.Muted}]Result: draw[/]");
        }

        if (m.FirstHalfStoppage > 0 || m.SecondHalfStoppage > 0)
        {
            AnsiConsole.MarkupLine($"[{Ui.Muted}]⏱ Added time: +{m.FirstHalfStoppage}' (1st half) · +{m.SecondHalfStoppage}' (2nd half)[/]");
        }

        if (m.HomeBox is not null)
        {
            var crowd = new WorldCup.Engine.Simulation.Crowd(m);
            string heat = m.TemperatureC > 0 ? $" · {m.TemperatureC}°C{(m.CoolingBreaks.Count > 0 ? " 🥤 cooling breaks taken" : "")}" : "";
            AnsiConsole.MarkupLine($"[{Ui.Muted}]🎺 {Markup.Escape(crowd.Summary)}{heat}[/]");
        }

        if (m.Miracle is { } mira)
        {
            AnsiConsole.MarkupLine($"[{Ui.Gold}]✨ MIRACLE:[/] [bold]{Markup.Escape(mira.TeamName)}[/] caught fire — {Markup.Escape(mira.Description)}.");
        }

        if (m.Upset is { } u)
        {
            var (label, colour) = MiracleBand(u.MiracleRating);
            AnsiConsole.MarkupLine(
                $"[{Ui.Accent}]🎲 Shock rating:[/] [{colour}]{u.MiracleRating:0.0}/10[/] [{colour}]{label}[/]  " +
                $"[{Ui.Muted}](pre-match: {Markup.Escape(m.HomeName)} {Ui.Pct(u.PreMatchHomeWin)} · draw {Ui.Pct(u.PreMatchDraw)} · {Markup.Escape(m.AwayName)} {Ui.Pct(u.PreMatchAwayWin)}; this exact score {Ui.Pct(u.ResultProbability)})[/]");
        }

        Ui.Blank();
        PrintGoals(m);

        // Goal of the match (highest vergazo) + a goal-quality summary.
        var best = m.Goals.OrderByDescending(x => x.Vergazo).FirstOrDefault();
        if (best is not null)
        {
            AnsiConsole.MarkupLine(
                $"[{Ui.Accent}]⚡ Goal of the match:[/] {Markup.Escape(best.ScorerName)} ({Markup.Escape(best.TeamCode)}) — " +
                $"vergazo [bold]{best.Vergazo:0.0}/10[/] ({GoalTypeName(best.Type)}, {best.DistanceMeters:0.0}m, {best.DefendersPassed} beaten, {best.Minute}')");

            double avgV = m.Goals.Average(x => x.Vergazo);
            int worldies = m.Goals.Count(x => x.Vergazo >= 9.0);
            double longest = m.Goals.Where(x => !x.IsPenalty && !x.IsOwnGoal).Select(x => x.DistanceMeters).DefaultIfEmpty(0).Max();
            AnsiConsole.MarkupLine(
                $"[{Ui.Muted}]Goal quality: avg vergazo {avgV:0.0}/10 · {worldies} certified vergazo(s) · longest strike {longest:0.0}m[/]");
        }

        var motm = PlayerOfTheMatch(m);
        if (motm is not null)
        {
            AnsiConsole.MarkupLine($"[{Ui.Warn}]⭐ Player of the match:[/] {motm}");
        }

        var flow = MatchFlow.Analyze(m);
        if (flow.MaxHomeLead > 0 || flow.MaxAwayLead > 0)
        {
            string lead = flow.MaxHomeLead >= flow.MaxAwayLead
                ? $"{m.HomeName} +{flow.MaxHomeLead}"
                : $"{m.AwayName} +{flow.MaxAwayLead}";

            string? comeback =
                m.WinnerCode == m.HomeCode && flow.MaxAwayLead > 0 ? $"{m.HomeName} won from {flow.MaxAwayLead} down"
                : m.WinnerCode == m.AwayCode && flow.MaxHomeLead > 0 ? $"{m.AwayName} won from {flow.MaxHomeLead} down"
                : m.IsDraw && flow.MaxAwayLead > 0 && flow.MaxHomeLead == 0 ? $"{m.HomeName} clawed back a draw"
                : m.IsDraw && flow.MaxHomeLead > 0 && flow.MaxAwayLead == 0 ? $"{m.AwayName} clawed back a draw"
                : null;

            string line = $"📈 Match flow: biggest lead {Markup.Escape(lead)}";
            if (flow.LeadChanges > 0) line += $" · {flow.LeadChanges} lead change{(flow.LeadChanges == 1 ? "" : "s")}";
            if (comeback is not null) line += $" · comeback — {Markup.Escape(comeback)}";
            AnsiConsole.MarkupLine($"[{Ui.Muted}]{line}[/]");
        }

        // Save of the match (highest-rated goalkeeper save) + how many were "amazing".
        var bestSave = m.SaveEvents.OrderByDescending(sv => sv.Rating).FirstOrDefault();
        if (bestSave is not null)
        {
            int amazing = m.SaveEvents.Count(sv => sv.IsAmazing);
            AnsiConsole.MarkupLine(
                $"[{Ui.Accent}]🧤 Save of the match:[/] {Markup.Escape(bestSave.KeeperName)} ({Markup.Escape(bestSave.TeamCode)}) — " +
                $"[bold]{bestSave.Rating:0.0}/10[/] ({bestSave.ShotDistanceMeters:0.0}m, {bestSave.Minute}')" +
                (amazing > 0 ? $"  ·  {amazing} amazing save{(amazing == 1 ? "" : "s")} in the match" : ""));
        }

        if (m.HomeBox is not null && m.AwayBox is not null)
        {
            PrintBoxScore(m);
        }

        PrintCards(m);
        PrintPenalties(m);
        PrintShootout(m);
        PrintSaves(m);
        PrintInjuries(m);
        PrintConfrontations(m);
        PrintErrorsAndControversy(m);

        if (showEventLog)
        {
            PrintEventLog(m);
        }
    }

    private static void PrintErrorsAndControversy(MatchResult m)
    {
        if (m.Errors.Count == 0 && m.BadCalls.Count == 0)
        {
            return;
        }

        if (m.Errors.Count > 0)
        {
            var table = Ui.Table("[bold]🤦 Errors[/]");
            table.AddColumn("Min");
            table.AddColumn("Team");
            table.AddColumn("Player");
            table.AddColumn("Type");
            table.AddColumn("What happened");
            table.AddColumn("Outcome");
            foreach (var e in m.Errors)
            {
                string kind = e.Kind == ErrorKind.GoalkeeperError ? "🧤 keeper" : "defensive";
                string outcome = e.LedToGoal ? "[red3]led to a goal[/]" : "[grey]got away with it[/]";
                table.AddRow($"{e.Minute}'", Markup.Escape(e.TeamCode), Markup.Escape(e.PlayerName), kind,
                    Markup.Escape(e.Description), outcome);
            }

            AnsiConsole.Write(table);
        }

        if (m.BadCalls.Count > 0)
        {
            var table = Ui.Table("[bold]⚖️ Refereeing controversy[/]");
            table.AddColumn("Min");
            table.AddColumn("Call");
            table.AddColumn("Player");
            table.AddColumn("Detail");
            table.AddColumn(new TableColumn("VAR").Centered());
            foreach (var bc in m.BadCalls)
            {
                table.AddRow(
                    $"{bc.Minute}'",
                    $"[gold1]{BadCallLabel(bc.Type)}[/]",
                    string.IsNullOrEmpty(bc.PlayerName) ? "—" : Markup.Escape(bc.PlayerName),
                    Markup.Escape(bc.Description),
                    bc.VarChecked ? "[grey]checked[/]" : "[grey]—[/]");
            }

            AnsiConsole.Write(table);
        }
    }

    private static string BadCallLabel(BadCallType t) => t switch
    {
        BadCallType.WrongPenaltyAwarded => "Soft penalty",
        BadCallType.PenaltyDenied => "Penalty denied",
        BadCallType.WrongCard => "Wrong card",
        BadCallType.MissedCard => "Missed red",
        BadCallType.GoalWronglyDisallowed => "Goal chalked off",
        BadCallType.GoalWronglyAllowed => "Should've been off",
        _ => "Bad call",
    };

    private static void PrintGoals(MatchResult m)
    {
        if (m.Goals.Count == 0)
        {
            AnsiConsole.MarkupLine($"[{Ui.Muted}]No goals.[/]");
            return;
        }

        var table = Ui.Table("[bold]Goals[/]");
        table.AddColumn("Min");
        table.AddColumn("Team");
        table.AddColumn("Scorer");
        table.AddColumn("Assist");
        table.AddColumn("Type");
        table.AddColumn("Dist");
        table.AddColumn(new TableColumn("Beat").Centered());
        table.AddColumn(new TableColumn("Vergazo").RightAligned());

        foreach (var g in m.Goals)
        {
            string scorer = g.IsOwnGoal ? $"{g.ScorerName} (OG)" : g.ScorerName;
            string tag = g.CausedByError switch
            {
                ErrorKind.GoalkeeperError => "  [red3](GK error)[/]",
                ErrorKind.DefensiveError => "  [gold1](def. error)[/]",
                _ => string.Empty,
            };
            table.AddRow(
                $"{g.Minute}'",
                Markup.Escape(g.TeamCode),
                Markup.Escape(scorer) + tag,
                Markup.Escape(g.AssistName ?? "—"),
                GoalTypeName(g.Type),
                g.IsPenalty ? "—" : $"{g.DistanceMeters:0.0}m",
                g.DefendersPassed > 0 ? g.DefendersPassed.ToString() : "—",
                Vergazo(g.Vergazo));
        }

        AnsiConsole.Write(table);
    }

    /// <summary>Pick a man-of-the-match from the goal/assist contributions (best goals weighted up).</summary>
    private static string? PlayerOfTheMatch(MatchResult m)
    {
        var score = new Dictionary<string, (string Name, string Team, int G, int A, double Pts)>(StringComparer.Ordinal);
        void Add(string id, string name, string team, int g, int a, double pts)
        {
            var cur = score.TryGetValue(id, out var v) ? v : (Name: name, Team: team, G: 0, A: 0, Pts: 0.0);
            score[id] = (name, team, cur.G + g, cur.A + a, cur.Pts + pts);
        }

        foreach (var goal in m.Goals)
        {
            if (goal.IsOwnGoal)
            {
                continue;
            }

            Add(goal.ScorerId, goal.ScorerName, goal.TeamCode, 1, 0, 2.0 + goal.Vergazo * 0.1);
            if (goal.AssistId is not null && goal.AssistName is not null)
            {
                Add(goal.AssistId, goal.AssistName, goal.TeamCode, 0, 1, 1.0);
            }
        }

        if (score.Count == 0)
        {
            return null;
        }

        var best = score.Values.OrderByDescending(x => x.Pts).ThenByDescending(x => x.G).First();
        string line = $"{best.G} goal{(best.G == 1 ? "" : "s")}" + (best.A > 0 ? $", {best.A} assist{(best.A == 1 ? "" : "s")}" : "");
        return $"{Markup.Escape(best.Name)} ({Markup.Escape(best.Team)}) — {line}";
    }

    private static (string Label, string Colour) MiracleBand(double r) =>
        r >= 9 ? ("MIRACLE 🐐", "red3")
        : r >= 7 ? ("SHOCK", "red3")
        : r >= 5 ? ("UPSET", "gold1")
        : r >= 3 ? ("mild surprise", "green3")
        : ("as expected", "grey");

    private static string Vergazo(double v)
    {
        string colour = v >= 8.5 ? "red3" : v >= 6.5 ? "gold1" : v >= 4 ? "green3" : "grey";
        string flame = v >= 8.5 ? " 🔥" : string.Empty;
        return $"[{colour}]{v:0.0}[/]{flame}";
    }

    private static void PrintBoxScore(MatchResult m)
    {
        var h = m.HomeBox!;
        var a = m.AwayBox!;
        var table = Ui.Table("[bold]Box score[/]");
        table.AddColumn(new TableColumn($"[bold]{Markup.Escape(m.HomeName)}[/]").RightAligned());
        table.AddColumn(new TableColumn("Stat").Centered());
        table.AddColumn(new TableColumn($"[bold]{Markup.Escape(m.AwayName)}[/]").LeftAligned());

        void Row(string label, object hv, object av) =>
            table.AddRow(hv.ToString()!, $"[{Ui.Muted}]{label}[/]", av.ToString()!);

        int homePens = m.Penalties.Count(x => x.TeamCode == m.HomeCode);
        int awayPens = m.Penalties.Count(x => x.TeamCode == m.AwayCode);
        int homeInj = m.Injuries.Count(x => x.TeamCode == m.HomeCode);
        int awayInj = m.Injuries.Count(x => x.TeamCode == m.AwayCode);

        string Ratio(int num, int den) => den > 0 ? $"{100.0 * num / den:0}%" : "—";

        Row("Possession", $"{h.PossessionPercent:0}%", $"{a.PossessionPercent:0}%");
        Row("Shots", h.Shots, a.Shots);
        Row("On target", h.ShotsOnTarget, a.ShotsOnTarget);
        Row("Shot accuracy", Ratio(h.ShotsOnTarget, h.Shots), Ratio(a.ShotsOnTarget, a.Shots));
        Row("Conversion", Ratio(h.Goals, h.Shots), Ratio(a.Goals, a.Shots));
        Row("Corners", h.Corners, a.Corners);
        Row("Throw-ins", h.ThrowIns, a.ThrowIns);
        Row("Goal kicks", h.GoalKicks, a.GoalKicks);
        Row("Fouls", h.Fouls, a.Fouls);
        Row("Offsides", h.Offsides, a.Offsides);
        Row("Saves", h.Saves, a.Saves);
        Row("Penalties", homePens, awayPens);
        Row("Yellow cards", h.Yellows, a.Yellows);
        Row("Red cards", h.Reds, a.Reds);
        Row("Injuries", homeInj, awayInj);

        // Mistakes & officiating, per team — always shown so every box score carries them.
        int hErr = m.Errors.Count(e => e.TeamCode == m.HomeCode);
        int aErr = m.Errors.Count(e => e.TeamCode == m.AwayCode);
        int hErrG = m.Errors.Count(e => e.TeamCode == m.HomeCode && e.LedToGoal);
        int aErrG = m.Errors.Count(e => e.TeamCode == m.AwayCode && e.LedToGoal);
        Row("Errors (→ goal)", $"{hErr} ({hErrG})", $"{aErr} ({aErrG})");
        Row("Bad calls for / against",
            $"{m.BadCalls.Count(b => b.ForCode == m.HomeCode)} / {m.BadCalls.Count(b => b.AgainstCode == m.HomeCode)}",
            $"{m.BadCalls.Count(b => b.ForCode == m.AwayCode)} / {m.BadCalls.Count(b => b.AgainstCode == m.AwayCode)}");
        AnsiConsole.Write(table);
    }

    private static void PrintCards(MatchResult m)
    {
        if (m.Cards.Count == 0)
        {
            return;
        }

        var table = Ui.Table("[bold]Cards[/]");
        table.AddColumn("Min");
        table.AddColumn("Team");
        table.AddColumn("Player");
        table.AddColumn("Card");
        table.AddColumn("Reason");
        foreach (var c in m.Cards)
        {
            string card = c.IsRed
                ? c.IsSecondYellow ? "[red3]Red (2nd yellow)[/]" : "[red3]Red[/]"
                : "[gold1]Yellow[/]";
            string reason = Markup.Escape(string.IsNullOrEmpty(c.Reason) ? "—" : c.Reason)
                + (c.Controversial ? " [gold1](harsh)[/]" : "");
            table.AddRow($"{c.Minute}'", Markup.Escape(c.TeamCode), Markup.Escape(c.PlayerName), card, reason);
        }

        AnsiConsole.Write(table);
    }

    private static void PrintPenalties(MatchResult m)
    {
        if (m.Penalties.Count == 0)
        {
            return;
        }

        var table = Ui.Table("[bold]Penalties[/]");
        table.AddColumn("Min");
        table.AddColumn("Team");
        table.AddColumn("Taker");
        table.AddColumn("Outcome");
        table.AddColumn("Keeper");
        foreach (var pen in m.Penalties)
        {
            string outcome = pen.Outcome switch
            {
                PenaltyOutcome.Scored => "[green3]Scored[/]",
                PenaltyOutcome.Saved => "[gold1]Saved[/]",
                _ => "[red3]Missed[/]",
            };
            if (pen.Controversial)
            {
                outcome += " [gold1](soft)[/]";
            }

            table.AddRow($"{pen.Minute}'", Markup.Escape(pen.TeamCode), Markup.Escape(pen.TakerName), outcome, Markup.Escape(pen.KeeperName));
        }

        AnsiConsole.Write(table);
    }

    private static void PrintSaves(MatchResult m)
    {
        if (m.SaveEvents.Count == 0)
        {
            return;
        }

        var table = Ui.Table("[bold]🧤 Notable saves[/]");
        table.AddColumn("Min");
        table.AddColumn("Keeper");
        table.AddColumn("Team");
        table.AddColumn(new TableColumn("Dist").RightAligned());
        table.AddColumn(new TableColumn("Rating").RightAligned());
        foreach (var s in m.SaveEvents.OrderByDescending(x => x.Rating))
        {
            string r = s.IsAmazing ? $"[red3]{s.Rating:0.0} 🔥[/]" : s.Rating >= 6.5 ? $"[gold1]{s.Rating:0.0}[/]" : $"{s.Rating:0.0}";
            table.AddRow($"{s.Minute}'", Markup.Escape(s.KeeperName), Markup.Escape(s.TeamCode), $"{s.ShotDistanceMeters:0.0}m", r);
        }

        AnsiConsole.Write(table);
    }

    private static void PrintConfrontations(MatchResult m)
    {
        if (m.Confrontations.Count == 0)
        {
            return;
        }

        var table = Ui.Table("[bold]🤬 Flashpoints[/]");
        table.AddColumn(new TableColumn("Min").RightAligned());
        table.AddColumn("Level");
        table.AddColumn("What happened");
        foreach (var cf in m.Confrontations.OrderBy(x => x.Minute))
        {
            string lvl = cf.Level switch
            {
                WorldCup.Engine.Simulation.ConfrontationLevel.Handbags => "handbags",
                WorldCup.Engine.Simulation.ConfrontationLevel.FaceOff => $"[{Ui.Warn}]face-off[/]",
                WorldCup.Engine.Simulation.ConfrontationLevel.Scuffle => $"[{Ui.Warn}]scuffle[/]",
                _ => $"[{Ui.Bad}]BRAWL{(cf.BenchInvolved ? " · benches in" : "")}[/]",
            };
            table.AddRow($"{cf.Minute}'", lvl, Markup.Escape(cf.Description));
        }

        AnsiConsole.Write(table);

        foreach (var g in m.Goals)
        {
            if (!string.IsNullOrEmpty(g.Celebration))
            {
                AnsiConsole.MarkupLine($"[{Ui.Muted}]  🎉 {g.Minute}' {Markup.Escape(g.ScorerName)} {Markup.Escape(g.Celebration)}.[/]");
            }
        }
    }

    private static void PrintShootout(MatchResult m)
    {
        if (m.ShootoutKicks.Count == 0)
        {
            return;
        }

        var table = Ui.Table($"[bold]🥅 Penalty shootout — {Markup.Escape(m.HomeName)} {m.HomePens}-{m.AwayPens} {Markup.Escape(m.AwayName)}[/]");
        table.AddColumn(new TableColumn("#").RightAligned());
        table.AddColumn("Team");
        table.AddColumn("Taker");
        table.AddColumn("Result");
        table.AddColumn(new TableColumn("Score").Centered());
        int h = 0, a = 0;
        foreach (var k in m.ShootoutKicks)
        {
            if (k.Scored)
            {
                if (k.IsHome) h++; else a++;
            }

            string res = k.Scored ? $"[{Ui.Good}]✓ scored[/]" : $"[{Ui.Bad}]✗ missed[/]";
            table.AddRow(k.Number.ToString(), Markup.Escape(k.TeamCode), Markup.Escape(k.Player), res, $"{h}-{a}");
        }

        AnsiConsole.Write(table);
        string winner = m.HomePens > m.AwayPens ? m.HomeName : m.AwayName;
        AnsiConsole.MarkupLine($"[{Ui.Gold}]🏆 {Markup.Escape(winner)} win the shootout {Math.Max(m.HomePens, m.AwayPens)}-{Math.Min(m.HomePens, m.AwayPens)}.[/]");
    }

    private static void PrintInjuries(MatchResult m)
    {
        if (m.Injuries.Count == 0)
        {
            return;
        }

        var table = Ui.Table("[bold]Injuries[/]");
        table.AddColumn("Min");
        table.AddColumn("Team");
        table.AddColumn("Player");
        table.AddColumn("Diagnosis");
        table.AddColumn("Severity");
        table.AddColumn("Out for");
        table.AddColumn("Replaced");
        foreach (var inj in m.Injuries)
        {
            string diagnosis = string.IsNullOrEmpty(inj.Diagnosis) ? "—" : inj.Diagnosis;
            table.AddRow($"{inj.Minute}'", Markup.Escape(inj.TeamCode), Markup.Escape(inj.PlayerName),
                Markup.Escape(diagnosis), inj.Severity.ToString(),
                Markup.Escape(InjuryCatalog.RecoveryText(inj.RecoveryDays)), inj.CouldBeReplaced ? "yes" : "[red3]no[/]");
        }

        AnsiConsole.Write(table);
    }

    private static void PrintEventLog(MatchResult m)
    {
        var events = new List<(int Minute, string Text)>();
        foreach (var g in m.Goals)
        {
            events.Add((g.Minute, $"⚽ [green3]GOAL[/] {Markup.Escape(g.ScorerName)} ({Markup.Escape(g.TeamCode)})" + (g.IsPenalty ? " (pen)" : g.IsOwnGoal ? " (OG)" : "")));
        }

        foreach (var c in m.Cards)
        {
            string why = string.IsNullOrEmpty(c.Reason) ? "" : $" [grey]— {Markup.Escape(c.Reason)}[/]";
            events.Add((c.Minute, (c.IsRed ? "🟥 " : "🟨 ") + $"{Markup.Escape(c.PlayerName)} ({Markup.Escape(c.TeamCode)}){why}"));
        }

        foreach (var inj in m.Injuries)
        {
            events.Add((inj.Minute, $"🩹 Injury {Markup.Escape(inj.PlayerName)} ({Markup.Escape(inj.TeamCode)})"));
        }

        foreach (var s in m.Substitutions)
        {
            events.Add((s.Minute, $"🔄 [{Ui.Muted}]Sub[/] {Markup.Escape(s.OnName)} on for {Markup.Escape(s.OffName)} ({Markup.Escape(s.TeamCode)})" + (s.Injury ? " [grey](injury)[/]" : "")));
        }

        foreach (var sv in m.SaveEvents.Where(x => x.IsAmazing))
        {
            events.Add((sv.Minute, $"🧤 [{Ui.Accent}]Amazing save[/] {Markup.Escape(sv.KeeperName)} ({Markup.Escape(sv.TeamCode)}) — {sv.Rating:0.0}/10"));
        }

        foreach (var e in m.Errors.Where(x => !x.LedToGoal))
        {
            string icon = e.Kind == ErrorKind.GoalkeeperError ? "🧤" : "🤦";
            events.Add((e.Minute, $"{icon} [{Ui.Muted}]Error[/] {Markup.Escape(e.PlayerName)} ({Markup.Escape(e.TeamCode)}) — {Markup.Escape(e.Description)}"));
        }

        foreach (var bc in m.BadCalls)
        {
            events.Add((bc.Minute, $"⚖️ [gold1]{BadCallLabel(bc.Type)}[/] [{Ui.Muted}]{Markup.Escape(bc.Description)}[/]" + (bc.VarChecked ? " [grey](VAR)[/]" : "")));
        }

        Ui.Blank();
        Ui.Header("Minute-by-minute");
        foreach (var (minute, text) in events.OrderBy(e => e.Minute))
        {
            AnsiConsole.MarkupLine($"[{Ui.Muted}]{minute,3}'[/]  {text}");
        }
    }

    /// <summary>Condense a many-run Monte Carlo into one headline forecast (the "law of large numbers" result).</summary>
    public static void PrintForecast(MatchMonteCarloReport r)
    {
        string outcome;
        double prob;
        if (r.HomeWin >= r.Draw && r.HomeWin >= r.AwayWin)
        {
            outcome = $"{r.HomeName} win";
            prob = r.HomeWin;
        }
        else if (r.AwayWin >= r.Draw && r.AwayWin >= r.HomeWin)
        {
            outcome = $"{r.AwayName} win";
            prob = r.AwayWin;
        }
        else
        {
            outcome = "Draw";
            prob = r.Draw;
        }

        string scoreline = r.TopScorelines.Count > 0
            ? $"{r.TopScorelines[0].HomeGoals}-{r.TopScorelines[0].AwayGoals} ({Ui.Pct(r.TopScorelines[0].Probability)})"
            : "n/a";

        var text =
            $"[{Ui.Gold}]Forecast — aggregated over {r.Iterations:N0} simulations[/]\n" +
            $"Most likely outcome: [bold]{Markup.Escape(outcome)}[/] ({Ui.Pct(prob)})\n" +
            $"Expected score: [bold]{Markup.Escape(r.HomeName)} {r.AvgHomeGoals:0.0} – {r.AvgAwayGoals:0.0} {Markup.Escape(r.AwayName)}[/]\n" +
            $"Most common scoreline: [bold]{scoreline}[/]";

        Ui.Hero(text, "Forecast", Ui.GoldColor);
    }

    public static void PrintFastMonteCarlo(MatchMonteCarloReport r)
    {
        var table = Ui.Table("[bold]Result probabilities[/]");
        table.AddColumn("Outcome");
        table.AddColumn(new TableColumn("Probability").RightAligned());
        table.AddRow($"{Markup.Escape(r.HomeName)} win", Ui.Heat(r.HomeWin));
        table.AddRow("Draw", Ui.Heat(r.Draw));
        table.AddRow($"{Markup.Escape(r.AwayName)} win", Ui.Heat(r.AwayWin));
        table.AddRow($"[{Ui.Muted}]Avg goals[/]", $"[{Ui.Muted}]{r.AvgHomeGoals:0.00} – {r.AvgAwayGoals:0.00}[/]");
        AnsiConsole.Write(table);

        PrintScorelines(r.TopScorelines, r.HomeName, r.AwayName);
        AnsiConsole.MarkupLine($"[{Ui.Muted}]{r.Iterations:N0} sims in {r.ElapsedSeconds:0.00}s · {Ui.Throughput(r.SimsPerSecond)}[/]");
    }

    /// <summary>
    /// The full single-matchup Monte Carlo output: most-probable score forecast, W/D/L, scoreline
    /// frequencies, and the averaged box score (cards, penalties, corners, …) per side.
    /// </summary>
    public static void PrintAggregate(MatchAggregateReport r)
    {
        // Headline forecast.
        string outcome;
        double prob;
        if (r.HomeWin >= r.Draw && r.HomeWin >= r.AwayWin) { outcome = $"{r.HomeName} win"; prob = r.HomeWin; }
        else if (r.AwayWin >= r.Draw && r.AwayWin >= r.HomeWin) { outcome = $"{r.AwayName} win"; prob = r.AwayWin; }
        else { outcome = "Draw"; prob = r.Draw; }

        // Most probable exact score overall, and the most probable score for the favoured outcome.
        int favOutcome = r.HomeWin >= r.Draw && r.HomeWin >= r.AwayWin ? 1
            : r.AwayWin >= r.Draw && r.AwayWin >= r.HomeWin ? -1 : 0;
        var modal = r.TopScorelines.Count > 0 ? r.TopScorelines[0] : null;
        ScorelineFrequency? favScore = null;
        foreach (var s in r.TopScorelines)
        {
            int so = s.HomeGoals > s.AwayGoals ? 1 : s.HomeGoals < s.AwayGoals ? -1 : 0;
            if (so == favOutcome) { favScore = s; break; }
        }

        string topScores = string.Join("  ·  ",
            r.TopScorelines.Take(3).Select(s => $"[bold]{s.HomeGoals}-{s.AwayGoals}[/] ({Ui.Pct(s.Probability)})"));

        var lines = new List<string>
        {
            $"[{Ui.Gold}]Forecast — aggregated over {r.Iterations:N0} simulations[/]",
            $"Most likely result: [bold]{Markup.Escape(outcome)}[/] ({Ui.Pct(prob)})",
            r.TopScorelines.Count > 0 ? $"Most probable scores: {topScores}" : "",
        };
        if (favScore is not null && modal is not null && (favScore.HomeGoals != modal.HomeGoals || favScore.AwayGoals != modal.AwayGoals))
        {
            lines.Add($"Most probable [bold]{Markup.Escape(outcome)}[/]: [bold]{favScore.HomeGoals}-{favScore.AwayGoals}[/] ({Ui.Pct(favScore.Probability)})");
        }

        int avgH = (int)Math.Round(r.AvgHomeGoals, MidpointRounding.AwayFromZero);
        int avgA = (int)Math.Round(r.AvgAwayGoals, MidpointRounding.AwayFromZero);
        lines.Add($"[{Ui.Muted}]Average goals: {Markup.Escape(r.HomeName)} {r.AvgHomeGoals:0.0} – {r.AvgAwayGoals:0.0} {Markup.Escape(r.AwayName)} (rounds to {avgH}-{avgA})[/]");

        Ui.Hero(string.Join("\n", lines.Where(l => l.Length > 0)), "Forecast", Ui.GoldColor);

        // W/D/L with a 95% confidence interval (Monte Carlo noise: ±1.96·√(p(1−p)/N)).
        var wdl = Ui.Table("[bold]Result probabilities[/]");
        wdl.AddColumn("Outcome");
        wdl.AddColumn(new TableColumn("Probability").RightAligned());
        wdl.AddColumn(new TableColumn("95% CI").RightAligned());
        string Ci(double pr) => $"[{Ui.Muted}]±{Ui.Pct(1.96 * Math.Sqrt(Math.Max(0, pr * (1 - pr)) / Math.Max(1, r.Iterations)))}[/]";
        wdl.AddRow($"{Markup.Escape(r.HomeName)} win", Ui.Heat(r.HomeWin), Ci(r.HomeWin));
        wdl.AddRow("Draw", Ui.Heat(r.Draw), Ci(r.Draw));
        wdl.AddRow($"{Markup.Escape(r.AwayName)} win", Ui.Heat(r.AwayWin), Ci(r.AwayWin));
        AnsiConsole.Write(wdl);

        // Match markets (derived from the simulated scoreline distribution).
        var markets = Ui.Table("[bold]Match markets[/]");
        markets.AddColumn("Market");
        markets.AddColumn(new TableColumn("Probability").RightAligned());
        markets.AddRow("Both teams to score", Ui.Heat(r.BttsPercent / 100.0));
        markets.AddRow("Over 2.5 goals", Ui.Heat(r.Over25Percent / 100.0));
        markets.AddRow("Under 2.5 goals", Ui.Heat(1.0 - r.Over25Percent / 100.0));
        markets.AddRow($"{Markup.Escape(r.HomeName)} clean sheet", Ui.Heat(r.HomeCleanSheetPercent / 100.0));
        markets.AddRow($"{Markup.Escape(r.AwayName)} clean sheet", Ui.Heat(r.AwayCleanSheetPercent / 100.0));
        markets.AddRow("Upset (underdog wins)", Ui.Heat(Math.Min(r.HomeWin, r.AwayWin)));
        AnsiConsole.Write(markets);

        PrintScorelines(r.TopScorelines, r.HomeName, r.AwayName);

        // Averaged box score (per-match means over all runs).
        var box = Ui.Table("[bold]Average match stats (per game)[/]");
        box.AddColumn(new TableColumn($"[bold]{Markup.Escape(r.HomeName)}[/]").RightAligned());
        box.AddColumn(new TableColumn("Stat").Centered());
        box.AddColumn(new TableColumn($"[bold]{Markup.Escape(r.AwayName)}[/]").LeftAligned());
        void Row(string label, double h, double a, string fmt = "0.00") =>
            box.AddRow(h.ToString(fmt), $"[{Ui.Muted}]{label}[/]", a.ToString(fmt));

        Row("Goals (avg)", r.Home.Goals, r.Away.Goals);
        Row("Possession %", r.Home.Possession, r.Away.Possession, "0.0");
        Row("Shots", r.Home.Shots, r.Away.Shots, "0.0");
        Row("Shots on target", r.Home.ShotsOnTarget, r.Away.ShotsOnTarget, "0.0");
        Row("Shot accuracy %", 100.0 * r.Home.ShotsOnTarget / Math.Max(1.0, r.Home.Shots), 100.0 * r.Away.ShotsOnTarget / Math.Max(1.0, r.Away.Shots), "0.0");
        Row("Conversion %", 100.0 * r.Home.Goals / Math.Max(1.0, r.Home.Shots), 100.0 * r.Away.Goals / Math.Max(1.0, r.Away.Shots), "0.0");
        Row("Corners", r.Home.Corners, r.Away.Corners, "0.0");
        Row("Throw-ins", r.Home.ThrowIns, r.Away.ThrowIns, "0.0");
        Row("Goal kicks", r.Home.GoalKicks, r.Away.GoalKicks, "0.0");
        Row("Fouls", r.Home.Fouls, r.Away.Fouls, "0.0");
        Row("Offsides", r.Home.Offsides, r.Away.Offsides, "0.0");
        Row("Yellow cards", r.Home.Yellows, r.Away.Yellows);
        Row("Red cards", r.Home.Reds, r.Away.Reds, "0.000");
        Row("Penalties", r.Home.Penalties, r.Away.Penalties, "0.000");
        Row("Injuries", r.Home.Injuries, r.Away.Injuries);
        Row("Saves", r.Home.Saves, r.Away.Saves, "0.0");
        AnsiConsole.Write(box);

        // Mistakes & refereeing controversy (averaged per game; both sides combined).
        var c = r.Controversy;
        var mist = Ui.Table("[bold]🤦 Mistakes & officiating (per game)[/]");
        mist.AddColumn("Event");
        mist.AddColumn(new TableColumn("Per game").RightAligned());
        mist.AddRow("Goalkeeper errors → goal", $"{c.KeeperErrorGoals:0.000}");
        mist.AddRow("Defensive errors → goal", $"{c.DefensiveErrorGoals:0.000}");
        mist.AddRow("Errors (no goal conceded)", $"{c.UnpunishedErrors:0.00}");
        mist.AddRow("Soft / wrong penalties", $"{c.ControversialPenalties:0.000}");
        mist.AddRow("Harsh / wrong cards", $"{c.ControversialCards:0.00}");
        mist.AddRow("Other referee mistakes", $"{c.RefereeMistakes:0.00}");
        AnsiConsole.Write(mist);

        // Most likely scorers (average goals per match).
        if (r.TopScorers.Count > 0)
        {
            var st = Ui.Table("[bold]Most likely scorers[/]");
            st.AddColumn("#");
            st.AddColumn("Player");
            st.AddColumn("Team");
            st.AddColumn(new TableColumn("Goals / match").RightAligned());
            int i = 1;
            foreach (var s in r.TopScorers.Take(8))
            {
                st.AddRow((i++).ToString(), Markup.Escape(s.Name), Markup.Escape(s.TeamCode), $"{s.GoalsPerMatch:0.000}");
            }

            AnsiConsole.Write(st);
        }

        // Goal quality (vergazo) aggregated — average and how rare a "worldie" is. The single best
        // goal over a million runs is essentially always ~10/10, so it is not shown here.
        AnsiConsole.MarkupLine(
            $"[{Ui.Accent}]Goal quality:[/] average vergazo [bold]{r.AverageVergazo:0.0}/10[/] · " +
            $"certified vergazos (9+/10) [bold]{r.WorldiePercent:0.00}%[/] of goals");

        AnsiConsole.MarkupLine($"[{Ui.Muted}]{r.Iterations:N0} detailed sims in {r.ElapsedSeconds:0.00}s · {Ui.Throughput(r.SimsPerSecond)}[/]");
    }

    /// <summary>One scannable table forecasting every remaining scheduled fixture (each over a big Monte Carlo).</summary>
    public static void PrintScheduledForecasts(ScheduledForecastReport r)
    {
        Ui.Hero(
            $"[{Ui.Gold}]Scheduled fixtures forecast[/]\n" +
            $"[{Ui.Muted}]{r.Games.Count} remaining game(s) · {r.IterationsPerGame:N0} simulations each · {r.ParameterLabel}[/]",
            "Run all scheduled games", Ui.GoldColor);

        if (r.Games.Count == 0)
        {
            Ui.Warning("No remaining fixtures to forecast.");
            return;
        }

        var table = Ui.Table("[bold]Remaining fixtures — most likely result[/]");
        table.AddColumn(new TableColumn("Grp").Centered());
        table.AddColumn(new TableColumn("MD").Centered());
        table.AddColumn("Match [grey](favourite highlighted)[/]");
        table.AddColumn(new TableColumn("Score").Centered());
        table.AddColumn(new TableColumn("Home").RightAligned());
        table.AddColumn(new TableColumn("Draw").RightAligned());
        table.AddColumn(new TableColumn("Away").RightAligned());
        table.AddColumn(new TableColumn("xG").Centered());

        foreach (var f in r.Games.OrderBy(g => g.KickoffUtc).ThenBy(g => g.Group).ThenBy(g => g.Matchday))
        {
            var m = f.Report;
            string home = f.Favourite == 1 ? $"[{Ui.Good}]{Markup.Escape(m.HomeName)}[/]" : Markup.Escape(m.HomeName);
            string away = f.Favourite == -1 ? $"[{Ui.Good}]{Markup.Escape(m.AwayName)}[/]" : Markup.Escape(m.AwayName);
            string flagH = Flags.Of(m.HomeCode);
            string flagA = Flags.Of(m.AwayCode);
            string match = $"{(flagH.Length > 0 ? flagH + " " : "")}{home} [grey]v[/] {away}{(flagA.Length > 0 ? " " + flagA : "")}";
            // Show the most likely scoreline for the forecast RESULT, so the score agrees with the win
            // odds (the single most-common score is often a draw/1–0 even for a favourite).
            string score = f.PredictedScore is { } s ? $"{s.HomeGoals}–{s.AwayGoals}" : "–";

            table.AddRow(
                f.Group.ToString(),
                f.Matchday.ToString(),
                match,
                score,
                Ui.Heat(m.HomeWin),
                Ui.Heat(m.Draw),
                Ui.Heat(m.AwayWin),
                $"{m.AvgHomeGoals:0.0}–{m.AvgAwayGoals:0.0}");
        }

        AnsiConsole.Write(table);

        // A couple of quick highlights to orient the eye.
        var biggest = r.Games.MaxBy(g => Math.Max(g.Report.HomeWin, g.Report.AwayWin));
        var tightest = r.Games.MinBy(g => Math.Max(g.Report.HomeWin, g.Report.AwayWin));
        var drawiest = r.Games.MaxBy(g => g.Report.Draw);
        if (biggest is not null && tightest is not null && drawiest is not null)
        {
            string Fav(ScheduledGameForecast g) => g.Favourite == 1 ? g.Report.HomeName : g.Favourite == -1 ? g.Report.AwayName : "neither";
            double FavPct(ScheduledGameForecast g) => Math.Max(g.Report.HomeWin, g.Report.AwayWin);
            AnsiConsole.MarkupLine(
                $"[{Ui.Muted}]Biggest favourite:[/] [{Ui.Good}]{Markup.Escape(Fav(biggest))}[/] " +
                $"({Ui.Pct(FavPct(biggest))}) v {Markup.Escape(biggest.Favourite == 1 ? biggest.Report.AwayName : biggest.Report.HomeName)}");
            AnsiConsole.MarkupLine(
                $"[{Ui.Muted}]Closest match:[/] {Markup.Escape(tightest.Report.HomeName)} v {Markup.Escape(tightest.Report.AwayName)} " +
                $"({Ui.Pct(tightest.Report.HomeWin)} / {Ui.Pct(tightest.Report.Draw)} / {Ui.Pct(tightest.Report.AwayWin)})");
            AnsiConsole.MarkupLine(
                $"[{Ui.Muted}]Most likely draw:[/] {Markup.Escape(drawiest.Report.HomeName)} v {Markup.Escape(drawiest.Report.AwayName)} " +
                $"({Ui.Pct(drawiest.Report.Draw)} draw)");
        }

        AnsiConsole.MarkupLine($"[{Ui.Muted}]Forecast over {r.IterationsPerGame:N0} sims/game · completed in {r.ElapsedSeconds:0.0}s. Heat: green = likely → grey = remote.[/]");
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Score = most likely scoreline for the forecast result. A favourite's win is spread over many scorelines, so the single most-common score is often a draw or 1–0.[/]");
    }

    /// <summary>Print the play-by-play commentary transcript to the console (two voices).</summary>
    public static void PrintCommentary(IReadOnlyList<CommentaryLine> lines)
    {
        Ui.Header("📻 Play-by-play commentary");
        foreach (var line in lines)
        {
            bool analyst = line.Speaker == CommentaryGenerator.Analyst;
            string who = analyst ? $"[{Ui.Gold}]Analyst    [/]" : $"[{Ui.Accent}]Commentator[/]";
            AnsiConsole.MarkupLine($"[{Ui.Muted}]{line.Minute,3}'[/]  {who}  {Markup.Escape(line.Text)}");
        }
    }

    private static string GoalTypeName(GoalType type) => type switch
    {
        GoalType.Penalty => "penalty",
        GoalType.LongRange => "long range",
        GoalType.BicycleKick => "bicycle kick",
        GoalType.FreeKick => "free kick",
        GoalType.Header => "header",
        GoalType.OwnGoal => "own goal",
        _ => "open play",
    };

    public static void PrintDetailedAggregate(MatchDetailedAggregate r)
    {
        var table = Ui.Table("[bold]Detailed aggregate[/]");
        table.AddColumn("Metric");
        table.AddColumn(new TableColumn("Value").RightAligned());
        table.AddRow($"{Markup.Escape(r.HomeName)} win", Ui.Heat(r.HomeWin));
        table.AddRow("Draw", Ui.Heat(r.Draw));
        table.AddRow($"{Markup.Escape(r.AwayName)} win", Ui.Heat(r.AwayWin));
        table.AddRow("Avg goals", $"{r.AvgHomeGoals:0.00} - {r.AvgAwayGoals:0.00}");
        table.AddRow("Yellows / game", $"{r.YellowsPerGame:0.00}");
        table.AddRow("Reds / game", $"{r.RedsPerGame:0.00}");
        table.AddRow("Penalties / game", $"{r.PenaltiesPerGame:0.00}");
        table.AddRow("Corners / game", $"{r.CornersPerGame:0.0}");
        table.AddRow("Injuries / game", $"{r.InjuriesPerGame:0.00}");
        table.AddRow("Shots / game", $"{r.ShotsPerGame:0.0}");
        AnsiConsole.Write(table);
        PrintScorelines(r.TopScorelines, r.HomeName, r.AwayName);
        AnsiConsole.MarkupLine($"[{Ui.Muted}]Best-of-{r.Iterations:N0} detailed simulations[/]");
    }

    private static void PrintScorelines(IReadOnlyList<ScorelineFrequency> scorelines, string home, string away)
    {
        if (scorelines.Count == 0)
        {
            return;
        }

        var table = Ui.Table("[bold]Most common scorelines[/]");
        table.AddColumn("Scoreline");
        table.AddColumn(new TableColumn("Frequency").RightAligned());
        foreach (var s in scorelines)
        {
            table.AddRow($"{s.HomeGoals}-{s.AwayGoals}", Ui.Pct2(s.Probability));
        }

        AnsiConsole.Write(table);
    }
}
