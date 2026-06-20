using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using Xunit;

namespace WorldCup.Tests;

public class MatchDetailTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    [Fact]
    public void Every_Card_Has_A_Specific_Reason()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(5);
        int cards = 0;
        for (int i = 0; i < 600; i++)
        {
            var r = MatchSimulator.Simulate(data.Team("BRA"), data.Team("ARG"), Stage.Group, Fidelity.Detailed, p, ref rng, true);
            foreach (var c in r.Cards)
            {
                cards++;
                Assert.False(string.IsNullOrWhiteSpace(c.Reason), "every card should record why it was shown");
                Assert.True(c.Reason.Length > 4);
            }
        }

        Assert.True(cards > 100, $"expected plenty of cards, saw {cards}");
    }

    [Fact]
    public void ThrowIns_And_GoalKicks_Are_In_A_Realistic_Range()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(9);
        long throwIns = 0, goalKicks = 0;
        const int n = 1500;
        for (int i = 0; i < n; i++)
        {
            var r = MatchSimulator.Simulate(data.Team("GER"), data.Team("ECU"), Stage.Group, Fidelity.Detailed, p, ref rng, true);
            throwIns += (r.HomeBox?.ThrowIns ?? 0) + (r.AwayBox?.ThrowIns ?? 0);
            goalKicks += (r.HomeBox?.GoalKicks ?? 0) + (r.AwayBox?.GoalKicks ?? 0);
        }

        double tiPerGame = (double)throwIns / n;
        double gkPerGame = (double)goalKicks / n;
        Assert.InRange(tiPerGame, 32.0, 48.0); // real ≈ 40
        Assert.InRange(gkPerGame, 10.0, 18.0); // real ≈ 14
    }

    [Fact]
    public void Momentum_And_Tempo_Widen_The_Score_Distribution_Without_Moving_The_Mean()
    {
        var data = Data;
        var home = data.Team("NED");
        var away = data.Team("JPN");

        var on = SimulateSpread(SimulationParameters.CreateStarting(), home, away, seed: 31);

        var flatParams = SimulationParameters.CreateStarting();
        flatParams.Global.MomentumStrength = 0;     // momentum off
        flatParams.Global.MatchTempoVariance = 0;   // tempo off
        var off = SimulateSpread(flatParams, home, away, seed: 31);

        // The mean is preserved (it's a redistribution, not an inflation) ...
        Assert.InRange(on.MeanTotal - off.MeanTotal, -0.25, 0.25);
        // ... but both the total-goals spread and the winning-margin spread are wider with them on.
        Assert.True(on.VarTotal > off.VarTotal, $"total-goal variance on {on.VarTotal:F3} should exceed off {off.VarTotal:F3}");
        Assert.True(on.VarMargin > off.VarMargin, $"margin variance on {on.VarMargin:F3} should exceed off {off.VarMargin:F3}");
    }

    private static (double MeanTotal, double VarTotal, double VarMargin) SimulateSpread(
        SimulationParameters p, Team home, Team away, ulong seed)
    {
        var rng = new Xoshiro256(seed);
        const int n = 6000;
        double sumTotal = 0, sumTotalSq = 0, sumMargin = 0, sumMarginSq = 0;
        for (int i = 0; i < n; i++)
        {
            var r = MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rng, true);
            int total = r.HomeGoals + r.AwayGoals;
            int margin = r.HomeGoals - r.AwayGoals;
            sumTotal += total;
            sumTotalSq += (double)total * total;
            sumMargin += margin;
            sumMarginSq += (double)margin * margin;
        }

        double meanTotal = sumTotal / n;
        double varTotal = sumTotalSq / n - meanTotal * meanTotal;
        double meanMargin = sumMargin / n;
        double varMargin = sumMarginSq / n - meanMargin * meanMargin;
        return (meanTotal, varTotal, varMargin);
    }
}
