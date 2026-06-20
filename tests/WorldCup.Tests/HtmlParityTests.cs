using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Stats;
using WorldCup.Engine.Tournament;
using WorldCup.Reporting;
using Xunit;

namespace WorldCup.Tests;

/// <summary>Guards that the HTML reports carry the same heavy stats the console shows.</summary>
public class HtmlParityTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    private static string Write(Action<string> writer, string name)
    {
        string path = Path.Combine(Path.GetTempPath(), name);
        writer(path);
        string html = File.ReadAllText(path);
        File.Delete(path);
        return html;
    }

    [Fact]
    public void StatsHtml_Includes_Injury_List_Assists_And_Discipline()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(7);
        var result = new TournamentSimulator(data, p).Simulate(Fidelity.Detailed, ref rng);
        var agg = new TournamentStatsAggregator(data, p.Global.Mvp);
        agg.Add(result);
        var stats = agg.Build();

        string html = Write(path => HtmlExporter.StatsToHtml(stats, path), "wc_test_stats.html");

        Assert.Contains("Most assists", html);
        Assert.Contains("Most yellow cards", html);
        Assert.Contains("Injury list", html);     // surfaces the diagnosis/recovery data
    }

    [Fact]
    public void TournamentHtml_Includes_ThirdPlace_Race_And_Placings()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(11);
        var result = new TournamentSimulator(data, p).Simulate(Fidelity.Fast, ref rng);

        string html = Write(path => HtmlExporter.TournamentToHtml(result, data, path), "wc_test_bracket.html");

        Assert.Contains("Best third-placed teams", html);
        Assert.Contains("How far each team got", html);
        Assert.Contains("<td class='r'>GF</td>", html); // full group-standings columns restored
    }

    [Fact]
    public void ForecastHtml_Includes_Markets_And_Confidence()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var report = MonteCarloMatchRunner.RunAggregate(data.Team("BRA"), data.Team("ENG"), p, 4000, Stage.Group, neutralVenue: true);

        string html = Write(path => HtmlExporter.MatchAggregateToHtml(report, path), "wc_test_forecast.html");

        Assert.Contains("Under 2.5 goals", html);
        Assert.Contains("Best goal across all sims", html);
        Assert.Contains("&#177;", html); // 95% confidence interval on the W/D/L bars (encoded ±)
        Assert.Contains("Scoreline heatmap", html);
    }

    [Fact]
    public void ScheduledForecastsHtml_Includes_Highlights_And_Kickoff()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var games = new List<ScheduledGameForecast>
        {
            new('C', 2, new DateTime(2026, 6, 25, 18, 0, 0, DateTimeKind.Utc), true,
                MonteCarloMatchRunner.RunFast(data.Team("BRA"), data.Team("HAI"), p, 20_000, true)),
            new('C', 2, new DateTime(2026, 6, 25, 21, 0, 0, DateTimeKind.Utc), true,
                MonteCarloMatchRunner.RunFast(data.Team("SCO"), data.Team("MAR"), p, 20_000, true)),
        };
        var batch = new ScheduledForecastReport(20_000, p.Label, p.Global.Seed, 1.2, games);

        string html = Write(path => HtmlExporter.ScheduledForecastsToHtml(batch, path), "wc_test_sched.html");

        Assert.Contains("Highlights", html);
        Assert.Contains("Kickoff", html);
    }
}
