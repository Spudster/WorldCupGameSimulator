using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using Xunit;

namespace WorldCup.Tests;

/// <summary>
/// The "miracle" is now a real in-game CAUSE of upsets (an underdog catching fire), not just a label.
/// These guard that it only fires for a clear underdog, fires rarely (realistic), boosts the right side,
/// and produces some upsets without letting minnows beat giants too often.
/// </summary>
public class MiracleTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    [Fact]
    public void Miracle_Never_Fires_For_An_Even_Match()
    {
        var g = SimulationParameters.CreateStarting().Global;
        var rng = new Xoshiro256(1);
        int fired = 0;
        for (int i = 0; i < 10_000; i++)
        {
            if (MatchModel.RollMiracle(70, 70, g, ref rng).Fired)
            {
                fired++;
            }
        }

        Assert.Equal(0, fired); // no clear underdog → no miracle
    }

    [Fact]
    public void Miracle_Fires_Rarely_For_A_Big_Gap_And_Closes_It()
    {
        var g = SimulationParameters.CreateStarting().Global;
        var rng = new Xoshiro256(2);
        int fired = 0;
        const int n = 40_000;
        for (int i = 0; i < n; i++)
        {
            if (MatchModel.RollMiracle(82, 55, g, ref rng).Fired)
            {
                fired++;
            }
        }

        double rate = fired / (double)n;
        Assert.InRange(rate, 0.03, 0.15); // rare, realistic

        // When it fires it lifts the underdog and dents the favourite, narrowing (not reversing) the gap.
        var o = MatchModel.RollMiracle(82, 55, g, ref rng);
        while (!o.Fired)
        {
            o = MatchModel.RollMiracle(82, 55, g, ref rng);
        }

        Assert.False(o.ForHome);                       // away (55) is the underdog
        Assert.True(o.AwayStrength > 55);              // boosted
        Assert.True(o.HomeStrength < 82);              // favourite rattled
        Assert.True(o.AwayStrength < o.HomeStrength);  // still the underdog, just closer
    }

    [Fact]
    public void Upsets_Happen_But_The_Favourite_Still_Usually_Wins()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var fav = data.Teams.OrderByDescending(t => p.EffectiveStrength(t)).First();
        var dog = data.Teams.OrderBy(t => p.EffectiveStrength(t)).First();
        double sf = p.EffectiveStrength(fav), sd = p.EffectiveStrength(dog);

        var rng = new Xoshiro256(7);
        int favWins = 0, dogWins = 0;
        const int n = 60_000;
        for (int i = 0; i < n; i++)
        {
            var r = FastMatchSimulator.SimulateRegulation(ref rng, sf, sd, p.Global, neutralVenue: true);
            if (r.WinnerIsHome == true) favWins++;
            else if (r.WinnerIsHome == false) dogWins++;
        }

        Assert.True(favWins > n * 0.55, $"favourite should dominate, won {favWins}/{n}");
        Assert.True(dogWins > 0, "but the underdog must win sometimes (upsets happen)");
        Assert.True(dogWins < n * 0.25, $"minnow shouldn't beat the giant too often, won {dogWins}/{n}");
    }
}
