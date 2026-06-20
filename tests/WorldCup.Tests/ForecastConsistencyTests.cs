using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

/// <summary>
/// The forecast probabilities and the scoreline distribution come from the same simulated matches, so
/// they must be internally consistent — and the displayed "predicted score" must agree with the win
/// odds even though the single most-common exact score is often a draw / 1–0 for a favourite.
/// </summary>
public class ForecastConsistencyTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    [Fact]
    public void Wdl_Probabilities_Sum_To_One()
    {
        var p = SimulationParameters.CreateStarting();
        var r = MonteCarloMatchRunner.RunFast(Data.Team("BRA"), Data.Team("ENG"), p, 200_000, neutralVenue: true);
        Assert.Equal(1.0, r.HomeWin + r.Draw + r.AwayWin, 6);
    }

    [Fact]
    public void A_Modest_Favourite_Can_Have_A_Draw_As_The_Most_Common_Score()
    {
        // Document the (correct, realistic) phenomenon the user asked about: a clear favourite by win %
        // whose single most-common exact scoreline is a draw or 1–0, because the win is spread over many
        // winning scorelines. We only assert the maths is coherent, not the specific scoreline.
        var p = SimulationParameters.CreateStarting();
        var r = MonteCarloMatchRunner.RunFast(Data.Team("USA"), Data.Team("AUS"), p, 300_000, neutralVenue: true);

        Assert.True(r.HomeWin > r.Draw && r.HomeWin > r.AwayWin); // home is the favourite
        var modal = r.TopScorelines[0];
        // The modal is genuinely the most frequent exact score (sanity: it's the first, highest count).
        Assert.True(r.TopScorelines.All(s => s.Count <= modal.Count));
    }

    [Fact]
    public void PredictedScore_Agrees_With_The_Favoured_Result()
    {
        var p = SimulationParameters.CreateStarting();
        var report = MonteCarloMatchRunner.RunFast(Data.Team("BRA"), Data.Team("HAI"), p, 200_000, neutralVenue: true);
        var f = new ScheduledGameForecast('C', 2, new DateTime(2026, 6, 25, 18, 0, 0, DateTimeKind.Utc), true, report);

        Assert.Equal(1, f.Favourite); // Brazil are heavy favourites over Haiti
        Assert.NotNull(f.PredictedScore);
        // The headline predicted score must be a Brazil win, matching the win odds.
        Assert.True(f.PredictedScore!.HomeGoals > f.PredictedScore.AwayGoals,
            $"predicted score {f.PredictedScore.HomeGoals}-{f.PredictedScore.AwayGoals} should be a home win");
        // And it must be a real scoreline from the simulated distribution.
        Assert.Contains(report.TopScorelines, s => s.HomeGoals == f.PredictedScore.HomeGoals && s.AwayGoals == f.PredictedScore.AwayGoals);
    }
}
