using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using Xunit;

namespace WorldCup.Tests;

public class MatchModelTests
{
    [Fact]
    public void Poisson_Sample_Mean_Approximates_Lambda()
    {
        var rng = new Xoshiro256(12345);
        const double lambda = 2.6;
        long sum = 0;
        const int n = 200_000;
        for (int i = 0; i < n; i++)
        {
            sum += Distributions.SamplePoisson(ref rng, lambda);
        }

        double mean = (double)sum / n;
        Assert.InRange(mean, lambda - 0.05, lambda + 0.05);
    }

    [Fact]
    public void EvenTeams_Produce_Roughly_Symmetric_ExpectedGoals()
    {
        var g = new GlobalParameters();
        var (home, away) = MatchModel.ExpectedGoals(70, 70, g, neutralVenue: true);
        Assert.Equal(home, away, 6);
        Assert.Equal(g.GoalBaseline, home, 6);
    }

    [Fact]
    public void StrongerTeam_Scores_More_On_Average()
    {
        var g = new GlobalParameters();
        var rng = new Xoshiro256(99);
        long strongGoals = 0, weakGoals = 0;
        const int n = 50_000;
        for (int i = 0; i < n; i++)
        {
            var r = FastMatchSimulator.SimulateRegulation(ref rng, 90, 50, g, neutralVenue: true);
            strongGoals += r.HomeGoals;
            weakGoals += r.AwayGoals;
        }

        Assert.True(strongGoals > weakGoals * 1.5, $"strong={strongGoals}, weak={weakGoals}");
    }

    [Fact]
    public void Scoreline_Mean_Tracks_Lambda()
    {
        var g = new GlobalParameters { DrawCoupling = 0.0 };
        var rng = new Xoshiro256(2024);
        var (lh, la) = MatchModel.ExpectedGoals(80, 60, g, neutralVenue: true);
        long home = 0, away = 0;
        const int n = 100_000;
        for (int i = 0; i < n; i++)
        {
            var (h, a) = MatchModel.SampleScoreline(ref rng, lh, la, g.DrawCoupling);
            home += h;
            away += a;
        }

        Assert.InRange((double)home / n, lh - 0.05, lh + 0.05);
        Assert.InRange((double)away / n, la - 0.05, la + 0.05);
    }

    [Fact]
    public void Knockout_Always_Produces_A_Winner()
    {
        var g = new GlobalParameters();
        var rng = new Xoshiro256(555);
        for (int i = 0; i < 5_000; i++)
        {
            var r = FastMatchSimulator.SimulateKnockout(ref rng, 75, 72, g);
            Assert.NotNull(r.WinnerIsHome);
            if (r.Method == MatchMethod.Penalties)
            {
                Assert.NotEqual(r.HomePens, r.AwayPens);
            }
        }
    }

    [Fact]
    public void Shootout_Is_Fair_For_Equal_Strength()
    {
        // Equal strength ⇒ equal per-kick conversion ⇒ ~50/50. The old early-exit bug biased the
        // home side upward (it could clinch before the away team took its matching kick).
        var g = new GlobalParameters();
        var rng = new Xoshiro256(2026);
        long homeWins = 0;
        const int n = 200_000;
        for (int i = 0; i < n; i++)
        {
            var r = ShootoutSimulator.Simulate(ref rng, 70, 70, g);
            Assert.True(r.HomeWon ? r.HomeScored > r.AwayScored : r.AwayScored > r.HomeScored);
            if (r.HomeWon)
            {
                homeWins++;
            }
        }

        double rate = (double)homeWins / n;
        Assert.InRange(rate, 0.485, 0.515);
    }

    [Fact]
    public void ShotDistance_Is_RightSkewed_With_Rare_LongRange()
    {
        var rng = new Xoshiro256(31);
        int longRange = 0;
        const int n = 100_000;
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            double d = Distributions.SampleShotDistance(ref rng);
            sum += d;
            if (d >= 25)
            {
                longRange++;
            }
        }

        double mean = sum / n;
        Assert.InRange(mean, 9.0, 14.0);            // most goals from inside/around the box
        Assert.InRange((double)longRange / n, 0.0005, 0.05); // 25m+ strikes are genuinely uncommon
    }
}
