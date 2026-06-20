using WorldCup.Engine.Parameters;
using WorldCup.Engine.Simulation;
using Xunit;

namespace WorldCup.Tests;

public class PredictionTests
{
    private static readonly GlobalParameters G = new();

    [Fact]
    public void InPlay_AtKickoff_MatchesFullMatchOdds()
    {
        var grid = MatchModel.ScoreGrid(1.6, 1.2, G.DrawCoupling);
        var (fh, fd, fa) = MatchModel.OutcomeProbabilities(grid);
        var (h, d, a) = MatchModel.InPlayOutcome(1.6, 1.2, G.DrawCoupling, 0, 0, 0);
        Assert.Equal(fh, h, 6);
        Assert.Equal(fd, d, 6);
        Assert.Equal(fa, a, 6);
    }

    [Fact]
    public void InPlay_AtFullTime_ConvergesToCurrentScore()
    {
        var (h, d, a) = MatchModel.InPlayOutcome(1.6, 1.2, G.DrawCoupling, 2, 1, 90);
        Assert.True(h > 0.999);
        Assert.True(d < 0.001 && a < 0.001);
    }

    [Fact]
    public void InPlay_LateLead_RaisesWinProbability()
    {
        var (preH, _, _) = MatchModel.InPlayOutcome(1.6, 1.2, G.DrawCoupling, 0, 0, 0);
        var (lateH, _, _) = MatchModel.InPlayOutcome(1.6, 1.2, G.DrawCoupling, 1, 0, 80);
        Assert.True(lateH > preH);
        Assert.True(lateH > 0.85);
    }

    [Fact]
    public void MiracleRating_IncreasesWithSurprise()
    {
        double chalk = MatchModel.MiracleRating(0.60, 0.11);
        double upset = MatchModel.MiracleRating(0.10, 0.08);
        double shock = MatchModel.MiracleRating(0.02, 0.04);
        Assert.True(chalk < upset && upset < shock);
        Assert.True(chalk <= 1.5);
        Assert.True(shock >= 9.5);
    }
}
