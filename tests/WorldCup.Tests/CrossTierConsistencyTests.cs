using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using Xunit;

namespace WorldCup.Tests;

/// <summary>
/// The fast (strength-only Poisson) and detailed (minute-by-minute event) simulators share one goal
/// model, so for the same matchup they must agree on goals/match and W/D/L. This guards that the two
/// tiers stay consistent (previously only asserted by convention).
/// </summary>
public class CrossTierConsistencyTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    [Fact]
    public void Fast_And_Detailed_Agree_On_Goals_And_Outcomes()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var home = data.Team("BRA");
        var away = data.Team("ENG");
        double sh = p.EffectiveStrength(home), sa = p.EffectiveStrength(away);
        const int n = 25_000;

        var rngFast = new Xoshiro256(101);
        double fastGoals = 0;
        int fh = 0, fd = 0, fa = 0;
        for (int i = 0; i < n; i++)
        {
            var r = FastMatchSimulator.SimulateRegulation(ref rngFast, sh, sa, p.Global, neutralVenue: true);
            fastGoals += r.HomeGoals + r.AwayGoals;
            if (r.WinnerIsHome == true) fh++;
            else if (r.WinnerIsHome == false) fa++;
            else fd++;
        }

        var rngDet = new Xoshiro256(202);
        double detGoals = 0;
        int dh = 0, dd = 0, da = 0;
        for (int i = 0; i < n; i++)
        {
            var r = MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rngDet, neutralVenue: true);
            detGoals += r.HomeGoals + r.AwayGoals;
            if (r.HomeGoals > r.AwayGoals) dh++;
            else if (r.AwayGoals > r.HomeGoals) da++;
            else dd++;
        }

        // Goals/match agree within ~0.12 (Monte Carlo noise + the detailed path's surge/momentum colour).
        Assert.True(Math.Abs(fastGoals / n - detGoals / n) < 0.12,
            $"fast {fastGoals / n:0.000} vs detailed {detGoals / n:0.000}");

        // W/D/L agree within ~3.5 percentage points each.
        Assert.True(Math.Abs((double)fh / n - (double)dh / n) < 0.035, $"home win fast {(double)fh / n:0.000} vs detailed {(double)dh / n:0.000}");
        Assert.True(Math.Abs((double)fd / n - (double)dd / n) < 0.035, $"draw fast {(double)fd / n:0.000} vs detailed {(double)dd / n:0.000}");
        Assert.True(Math.Abs((double)fa / n - (double)da / n) < 0.035, $"away win fast {(double)fa / n:0.000} vs detailed {(double)da / n:0.000}");
    }
}
