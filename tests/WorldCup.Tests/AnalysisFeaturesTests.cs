using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Tournament;
using WorldCup.Reporting;
using Xunit;

namespace WorldCup.Tests;

/// <summary>Covers the analysis features: odds conversion, road-to-glory, and the model-accuracy backtest.</summary>
public class AnalysisFeaturesTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    [Theory]
    [InlineData(0.5, "2.00", "1/1", "-100")]
    [InlineData(0.25, "4.00", "3/1", "+300")]
    [InlineData(0.8, "1.25", "1/4", "-400")]
    public void OddsConverter_Produces_Standard_Formats(double prob, string dec, string frac, string us)
    {
        Assert.Equal(dec, OddsConverter.Decimal(prob));
        Assert.Equal(frac, OddsConverter.Fractional(prob));
        Assert.Equal(us, OddsConverter.American(prob));
    }

    [Fact]
    public void OddsConverter_Is_Robust_To_Degenerate_Probabilities()
    {
        Assert.Equal("—", OddsConverter.Decimal(0));
        Assert.Equal("—", OddsConverter.American(0));
        Assert.Equal("—", OddsConverter.American(1));
        Assert.Equal("—", OddsConverter.Fractional(-0.1));
    }

    [Fact]
    public void RoadToGlory_Is_Monotonic_And_Bounded()
    {
        var p = SimulationParameters.CreateStarting();
        var r = RoadToGloryAnalyzer.Analyze(Data, "BRA", p, locked: null, iterations: 3000, seed: 42, includeThirdPlacePlayoff: true);

        Assert.Equal("BRA", r.TeamCode);
        Assert.InRange(r.ChampionProbability, 0.0, 1.0);
        Assert.Equal(5, r.Stages.Count); // R32, R16, QF, SF, Final

        // Reaching a later round can never be more likely than reaching an earlier one.
        for (int i = 1; i < r.Stages.Count; i++)
        {
            Assert.True(r.Stages[i].ReachProbability <= r.Stages[i - 1].ReachProbability + 1e-9,
                $"{r.Stages[i].StageName} reach {r.Stages[i].ReachProbability} > {r.Stages[i - 1].StageName} {r.Stages[i - 1].ReachProbability}");
        }

        // The champion probability cannot exceed the chance of reaching the final.
        Assert.True(r.ChampionProbability <= r.FinalProbability + 1e-9);
    }

    [Fact]
    public void Backtest_Scores_Are_Coherent()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        // A few hand-built results (orientation matches the schedule's home/away).
        var played = new List<PlayedResult>
        {
            new("BRA", "HAI", 4, 0),
            new("ENG", "USA", 1, 1),
            new("ARG", "MAR", 2, 1),
        };

        var r = BacktestAnalyzer.Analyze(data, played, p);

        Assert.Equal(3, r.Matches);
        Assert.InRange(r.FavouriteHitRate, 0.0, 1.0);
        Assert.InRange(r.BrierScore, 0.0, 2.0);   // 3-class Brier is bounded by 2
        Assert.True(r.LogLoss >= 0.0);
        Assert.Equal(3, r.Rows.Count);
        Assert.All(r.Calibration, b => Assert.InRange(b.ObservedFreq, 0.0, 1.0));
    }

    [Fact]
    public void Analysis_Html_Reports_Render()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();

        var road = RoadToGloryAnalyzer.Analyze(data, "FRA", p, null, 1500, 7, true);
        var odds = MonteCarloTournamentRunner.Run(data, p, 30_000);
        var back = BacktestAnalyzer.Analyze(data, new List<PlayedResult> { new("BRA", "HAI", 3, 0) }, p);

        var perms = GroupPathAnalyzer.Permutations(data, 'A', new List<PlayedResult>(), 3, selectedCode: null);

        AssertRenders(path => HtmlExporter.RoadToGloryToHtml(road, path), "road.html", "road to glory");
        AssertRenders(path => HtmlExporter.OddsBoardToHtml(odds, path), "odds.html", "win the World Cup");
        AssertRenders(path => HtmlExporter.BacktestToHtml(back, path), "back.html", "Calibration");
        AssertRenders(path => HtmlExporter.GroupPermutationsToHtml(perms, path), "perms.html", "QUALIFICATION SCENARIOS");
    }

    [Fact]
    public void Permutations_Enumerate_Every_Combination_With_Two_Qualifiers()
    {
        var data = Data;
        // No results loaded => all 6 group games remain => 3^6 = 729 combinations.
        var g = GroupPathAnalyzer.Permutations(data, 'A', new List<PlayedResult>(), seed: 5, selectedCode: null);

        Assert.Equal(6, g.Fixtures.Count);
        Assert.Equal(729, g.TotalCombinations);
        Assert.Equal(729, g.Rows.Count);
        Assert.All(g.Rows, r =>
        {
            Assert.Equal(6, r.Outcomes.Count);
            Assert.All(r.Outcomes, o => Assert.True(o is -1 or 0 or 1));
            Assert.NotEqual(r.FirstCode, r.SecondCode); // two distinct qualifiers
            Assert.NotEqual(r.SecondCode, r.ThirdCode);
        });

        // With one game left in a group, there are exactly three scenarios.
        var played = data.GroupSchedule.Where(f => f.Group == 'B').Take(5)
            .Select(f => new PlayedResult(f.HomeCode, f.AwayCode, 1, 0)).ToList();
        var one = GroupPathAnalyzer.Permutations(data, 'B', played, seed: 5, selectedCode: null);
        Assert.Single(one.Fixtures);
        Assert.Equal(3, one.TotalCombinations);
    }

    private static void AssertRenders(Action<string> writer, string name, string mustContain)
    {
        string path = Path.Combine(Path.GetTempPath(), "wc_" + name);
        writer(path);
        string html = File.ReadAllText(path);
        File.Delete(path);
        Assert.Contains(mustContain, html);
    }
}
