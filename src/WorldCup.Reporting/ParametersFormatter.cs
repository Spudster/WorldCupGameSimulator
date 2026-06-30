using Spectre.Console;
using WorldCup.Engine.Calibration;
using WorldCup.Engine.Parameters;

namespace WorldCup.Reporting;

/// <summary>Formats the parameter set and the calibration diagnostics.</summary>
public static class ParametersFormatter
{
    public static void Print(SimulationParameters p)
    {
        var g = p.Global;
        Ui.Header($"Parameters: {p.Label}");

        var model = Ui.Table("[bold]Match model[/]");
        model.AddColumn("Knob");
        model.AddColumn(new TableColumn("Value").RightAligned());
        model.AddRow("Goal baseline (λ, even teams)", $"{g.GoalBaseline:0.000}");
        model.AddRow("Strength sensitivity", $"{g.StrengthSensitivity:0.000}");
        model.AddRow("Home advantage", $"{g.HomeAdvantage:0.000}");
        model.AddRow("Draw coupling", $"{g.DrawCoupling:0.000}");
        model.AddRow("Upset variance (form/luck)", $"{g.UpsetVariance:0.000}");
        model.AddRow("Match tempo variance", $"{g.MatchTempoVariance:0.000}");
        model.AddRow("Momentum strength", $"{g.MomentumStrength:0.000}");
        model.AddRow("Squad quality weight", $"{g.SquadQualityWeight:0.000}");
        model.AddRow("Recent-form weight", $"{g.FormWeight:0.000}");
        model.AddRow("Extra-time goal scale", $"{g.ExtraTimeGoalScale:0.000}");
        model.AddRow("Shootout strength weight", $"{g.ShootoutStrengthWeight:0.000}");
        model.AddRow("RNG seed", g.Seed.ToString());
        AnsiConsole.Write(model);

        var ev = g.Events;
        var events = Ui.Table("[bold]Event rates (per match)[/]");
        events.AddColumn("Rate");
        events.AddColumn(new TableColumn("Value").RightAligned());
        events.AddRow("Yellow cards", $"{ev.YellowCardsPerMatch:0.00}");
        events.AddRow("Direct red cards", $"{ev.DirectRedCardsPerMatch:0.000}");
        events.AddRow("Penalties", $"{ev.PenaltiesPerMatch:0.00}");
        events.AddRow("Corners", $"{ev.CornersPerMatch:0.0}");
        events.AddRow("Throw-ins", $"{ev.ThrowInsPerMatch:0.0}");
        events.AddRow("Goal kicks", $"{ev.GoalKicksPerMatch:0.0}");
        events.AddRow("Injuries", $"{ev.InjuriesPerMatch:0.00}");
        events.AddRow("Fouls", $"{ev.FoulsPerMatch:0.0}");
        events.AddRow("Penalty conversion", $"{ev.PenaltyConversionBase:0.00}");
        AnsiConsole.Write(events);

        var mistakes = Ui.Table("[bold]Mistakes & officiating[/]");
        mistakes.AddColumn("Rate");
        mistakes.AddColumn(new TableColumn("Value").RightAligned());
        mistakes.AddRow("Defensive error → goal share", $"{ev.DefensiveErrorGoalShare:0.000}");
        mistakes.AddRow("Goalkeeper error → goal share", $"{ev.GoalkeeperErrorGoalShare:0.000}");
        mistakes.AddRow("Unpunished errors / team", $"{ev.UnpunishedErrorsPerMatch:0.00}");
        mistakes.AddRow("Wrong-penalty share", $"{ev.WrongPenaltyShare:0.000}");
        mistakes.AddRow("Wrong-card share", $"{ev.WrongCardShare:0.000}");
        mistakes.AddRow("Referee mistakes / match", $"{ev.RefereeMistakesPerMatch:0.00}");
        AnsiConsole.Write(mistakes);

        AnsiConsole.MarkupLine(
            $"[{Ui.Muted}]Overrides: {p.TeamStrengthOverrides.Count} team strength(s), {p.PlayerAttributeOverrides.Count} player attribute(s), " +
            $"{p.FormationOverrides.Count} formation(s); default formation {Markup.Escape(g.DefaultFormation)}; " +
            $"{p.UnavailablePlayers.Count} player(s) marked unavailable; {p.PreferredStarters.Count} pinned starter(s).[/]");
    }

    public static void PrintCalibration(CalibrationReport report)
    {
        Ui.Header("Calibration diagnostics",
            $"{report.Matches:N0} detailed matches per measurement" + (report.AutoTuned ? $" · auto-tuned in {report.TuningIterations} iteration(s)" : ""));

        var t = Ui.Table("[bold]Calibration[/]");
        t.AddColumn("Metric");
        t.AddColumn(new TableColumn("Measured").RightAligned());
        t.AddColumn(new TableColumn("Target").RightAligned());
        t.AddColumn(new TableColumn("Delta").RightAligned());
        t.AddColumn(new TableColumn("Status").Centered());

        foreach (var m in report.Metrics)
        {
            string status = m.InBand ? "[green3]✓ in band[/]" : "[red3]! off[/]";
            string delta = (m.Delta >= 0 ? "+" : "") + m.Delta.ToString("0.000");
            t.AddRow(m.Name, $"{m.Measured:0.000}", $"{m.Target:0.000}", delta, status);
        }

        AnsiConsole.Write(t);
    }
}
