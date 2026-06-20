using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

/// <summary>A saved tournament must reload identically, so "load a saved tournament" re-renders the same thing.</summary>
public class TournamentSnapshotTests
{
    [Fact]
    public void Detailed_Tournament_Round_Trips_Through_Json()
    {
        var data = new SeedTeamDataProvider().GetTournamentData();
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(123);
        var original = new TournamentSimulator(data, p).Simulate(Fidelity.Detailed, ref rng);

        string path = Path.Combine(Path.GetTempPath(), "wc_snapshot_test.json");
        TournamentSnapshot.Save(original, path);
        var loaded = TournamentSnapshot.Load(path);
        File.Delete(path);

        Assert.Equal(original.ChampionCode, loaded.ChampionCode);
        Assert.Equal(original.RunnerUpCode, loaded.RunnerUpCode);
        Assert.Equal(original.GroupResults.Count, loaded.GroupResults.Count);
        Assert.Equal(original.KnockoutResults.Count, loaded.KnockoutResults.Count);
        Assert.Equal(original.GroupStandings.Count, loaded.GroupStandings.Count);
        Assert.Equal(original.QualifiedThirds.Count, loaded.QualifiedThirds.Count);

        // Spot-check a knockout match round-trips its score and winner.
        var ko = original.KnockoutResults[^1];
        var koLoaded = loaded.KnockoutResults[^1];
        Assert.Equal(ko.Result.HomeGoals, koLoaded.Result.HomeGoals);
        Assert.Equal(ko.Result.AwayGoals, koLoaded.Result.AwayGoals);
        Assert.Equal(ko.Result.WinnerCode, koLoaded.Result.WinnerCode);

        // And the furthest-stage map (drives "how far each team got").
        Assert.Equal(original.FurthestStage.Count, loaded.FurthestStage.Count);
    }
}
