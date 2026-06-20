using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

public class TournamentTests
{
    private static TournamentData LoadData() => new SeedTeamDataProvider().GetTournamentData();

    [Fact]
    public void CurrentState_Locks_Real_Results_Unchanged()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        var locked = new[]
        {
            new PlayedResult("MEX", "RSA", 2, 0),
            new PlayedResult("ARG", "ALG", 3, 0),
        };

        var sim = new TournamentSimulator(data, p, includeThirdPlacePlayoff: true, lockedResults: locked);
        var rng = new Xoshiro256(42);
        var result = sim.Simulate(Fidelity.Detailed, ref rng);

        AssertLocked(result, "MEX", "RSA", 2, 0);
        AssertLocked(result, "ARG", "ALG", 3, 0);

        // Run again with a different seed — locked scores must be identical.
        var rng2 = new Xoshiro256(99999);
        var result2 = sim.Simulate(Fidelity.Detailed, ref rng2);
        AssertLocked(result2, "MEX", "RSA", 2, 0);
    }

    private static void AssertLocked(TournamentResult result, string a, string b, int aGoals, int bGoals)
    {
        var match = result.GroupResults.Single(m =>
            (m.HomeCode == a && m.AwayCode == b) || (m.HomeCode == b && m.AwayCode == a));
        Assert.True(match.IsLocked);
        int forA = match.HomeCode == a ? match.HomeGoals : match.AwayGoals;
        int forB = match.HomeCode == b ? match.HomeGoals : match.AwayGoals;
        Assert.Equal(aGoals, forA);
        Assert.Equal(bGoals, forB);
    }

    [Fact]
    public void SinglePlaythrough_Produces_A_Champion_And_32_Qualifiers()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        var sim = new TournamentSimulator(data, p);
        var rng = new Xoshiro256(7);
        var result = sim.Simulate(Fidelity.Fast, ref rng);

        Assert.False(string.IsNullOrEmpty(result.ChampionCode));
        Assert.NotEqual(result.ChampionCode, result.RunnerUpCode);

        int qualifiers = result.FurthestStage.Count(kv => Stages.Rank(kv.Value) >= Stages.Rank(Stage.RoundOf32));
        Assert.Equal(32, qualifiers);
        Assert.Equal(8, result.QualifiedThirds.Count);

        // Champion appeared in the final.
        Assert.Equal(Stage.Final, result.FurthestStage[result.ChampionCode]);
    }

    [Fact]
    public void PreferredGoalkeeper_Actually_Starts_In_A_Simulated_Match()
    {
        var data = LoadData();
        var home = data.Team("ENG");
        var away = data.Team("FRA");

        // The keeper who starts by default, and a different keeper to pin instead.
        var defaultGk = LineupProjector.Project(home, "4-3-3").Xi.First(pl => pl.Position == Position.GK);
        var backup = home.Squad.First(pl => pl.Position == Position.GK && pl.Id != defaultGk.Id);

        var p = SimulationParameters.CreateStarting();
        p.PreferredStarters.Add(backup.Id);

        var rng = new Xoshiro256(7);
        var result = MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rng, neutralVenue: true);

        // The pinned backup made the starting XI, so it appears in the line-up (every starter does).
        // Which keeper starts is proved deterministically at the projector level in LineupTests; here we
        // only confirm the pin flows through the match engine's own selection path.
        Assert.Contains(backup.Id, result.HomeLineup);
    }

    [Fact]
    public void FastMonteCarlo_Probabilities_Are_Coherent()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        var report = MonteCarloTournamentRunner.Run(data, p, 3_000);

        Assert.Equal(3_000, report.Iterations);

        double championSum = report.Teams.Sum(t => t.Champion);
        Assert.InRange(championSum, 0.98, 1.02);

        foreach (var t in report.Teams)
        {
            Assert.InRange(t.Champion, 0.0, 1.0);
            Assert.True(t.ReachedR32 + 1e-9 >= t.ReachedR16, $"{t.Code}: R32 < R16");
            Assert.True(t.ReachedR16 + 1e-9 >= t.ReachedQuarter);
            Assert.True(t.ReachedFinal + 1e-9 >= t.Champion);
        }

        // A clear favourite should out-perform a clear minnow.
        double arg = report.Teams.First(t => t.Code == "ARG").Champion;
        double nzl = report.Teams.First(t => t.Code == "NZL").Champion;
        Assert.True(arg > nzl, $"ARG {arg} should exceed NZL {nzl}");
    }

    [Fact]
    public void Reproducible_With_Same_Seed()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        var r1 = MonteCarloTournamentRunner.Run(data, p, 2_000);
        var r2 = MonteCarloTournamentRunner.Run(data, p, 2_000);

        // Same seed + same parameters → identical champion tallies.
        var c1 = r1.Teams.OrderBy(t => t.Code).Select(t => t.Champion).ToList();
        var c2 = r2.Teams.OrderBy(t => t.Code).Select(t => t.Champion).ToList();
        Assert.Equal(c1, c2);
    }
}
