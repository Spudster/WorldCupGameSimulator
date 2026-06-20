using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using Xunit;

namespace WorldCup.Tests;

public class LineupTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    [Theory]
    [InlineData("4-3-3", 4, 3, 3)]
    [InlineData("4-4-2", 4, 4, 2)]
    [InlineData("3-5-2", 3, 5, 2)]
    [InlineData("4-2-3-1", 4, 5, 1)]  // middle numbers collapse into midfield
    [InlineData("5-3-2", 5, 3, 2)]
    [InlineData("nonsense", 4, 3, 3)] // invalid falls back to 4-3-3
    public void ParseFormation_Works(string formation, int def, int mid, int fwd)
    {
        Assert.Equal((def, mid, fwd), LineupProjector.ParseFormation(formation));
    }

    [Fact]
    public void Project_Respects_Formation_Counts()
    {
        var proj = LineupProjector.Project(Data.Team("ENG"), "4-4-2");
        Assert.Equal(11, proj.Xi.Count);
        Assert.Equal(1, proj.Xi.Count(p => p.Position == Position.GK));
        Assert.Equal(4, proj.Xi.Count(p => p.Position == Position.DEF));
        Assert.Equal(4, proj.Xi.Count(p => p.Position == Position.MID));
        Assert.Equal(2, proj.Xi.Count(p => p.Position == Position.FWD));
    }

    [Fact]
    public void Project_Excludes_Unavailable_Players_And_Still_Fields_Eleven()
    {
        var team = Data.Team("ENG");
        var star = LineupProjector.Project(team, "4-3-3").Xi.First(p => p.Position == Position.FWD);

        var xi = LineupProjector.Project(team, "4-3-3", p => p.Id != star.Id).Xi;
        Assert.DoesNotContain(xi, p => p.Id == star.Id);
        Assert.Equal(11, xi.Count); // backfilled from the rest of the squad
    }

    [Fact]
    public void Project_Honours_A_Preferred_Starting_Goalkeeper()
    {
        var team = Data.Team("ENG");
        var keepers = team.Squad.Where(p => p.Position == Position.GK).ToList();
        Assert.True(keepers.Count >= 2, "need a backup keeper to test the override");

        var defaultGk = LineupProjector.Project(team, "4-3-3").Xi.First(p => p.Position == Position.GK);
        var backup = keepers.First(k => k.Id != defaultGk.Id);

        // Pin the backup as preferred — it must now be the one selected to start.
        var xi = LineupProjector.Project(team, "4-3-3", null, new[] { backup.Id }).Xi;
        var startingGk = xi.First(p => p.Position == Position.GK);
        Assert.Equal(backup.Id, startingGk.Id);
        Assert.Equal(11, xi.Count);
        Assert.Equal(1, xi.Count(p => p.Position == Position.GK));
    }
}
