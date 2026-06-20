using Spectre.Console;
using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;

namespace WorldCup.Reporting;

/// <summary>Displays the projected starting XI for both teams of a fixture.</summary>
public static class LineupFormatter
{
    public static void PrintProjectedLineups(Team home, Team away, SimulationParameters p)
    {
        var homeProj = LineupProjector.Project(home, p.Formation(home), p.IsAvailable, p.PreferredStarters);
        var awayProj = LineupProjector.Project(away, p.Formation(away), p.IsAvailable, p.PreferredStarters);
        var homeXi = homeProj.Xi;
        var awayXi = awayProj.Xi;

        var table = Ui.Table("[bold]Projected starting XI[/] [grey](derived from current squad ratings)[/]");
        table.AddColumn(new TableColumn("Pos").Centered());
        table.AddColumn(new TableColumn($"[bold]{Markup.Escape(home.Name)}[/] [grey]{homeProj.Formation}[/]"));
        table.AddColumn(new TableColumn($"[bold]{Markup.Escape(away.Name)}[/] [grey]{awayProj.Formation}[/]"));

        table.AddRow("GK", Line(homeXi, Position.GK), Line(awayXi, Position.GK));
        table.AddRow("DEF", Line(homeXi, Position.DEF), Line(awayXi, Position.DEF));
        table.AddRow("MID", Line(homeXi, Position.MID), Line(awayXi, Position.MID));
        table.AddRow("FWD", Line(homeXi, Position.FWD), Line(awayXi, Position.FWD));

        AnsiConsole.Write(table);
    }

    private static string Line(IReadOnlyList<Player> xi, Position pos) =>
        Markup.Escape(string.Join(" · ", xi.Where(p => p.Position == pos).Select(p => p.Name)));
}
