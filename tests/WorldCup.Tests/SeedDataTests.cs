using WorldCup.Data;
using WorldCup.Data.Providers;
using Xunit;

namespace WorldCup.Tests;

public class SeedDataTests
{
    private static readonly Lazy<WorldCup.Data.Models.TournamentData> Data =
        new(() => new SeedTeamDataProvider().GetTournamentData());

    [Fact]
    public void Seed_Has_48_Teams_In_12_Groups_Of_Four()
    {
        var data = Data.Value;
        Assert.Equal(48, data.Teams.Count);
        Assert.Equal(12, data.Groups.Count);
        foreach (var group in data.Groups)
        {
            Assert.Equal(4, data.TeamsInGroup(group).Count);
        }
    }

    [Fact]
    public void Official_Draw_Spot_Checks()
    {
        var data = Data.Value;
        Assert.Equal('A', data.Team("MEX").Group);
        Assert.Equal('C', data.Team("BRA").Group);
        Assert.Equal('J', data.Team("ARG").Group);
        Assert.Equal('L', data.Team("ENG").Group);
        // Resolved playoff winners are present (Italy/Denmark/Poland are not).
        Assert.True(data.TryGetTeam("BIH", out _));
        Assert.True(data.TryGetTeam("SWE", out _));
        Assert.True(data.TryGetTeam("COD", out _));
        Assert.False(data.TryGetTeam("ITA", out _));
    }

    [Fact]
    public void Schedule_Is_A_Single_RoundRobin_72_Matches()
    {
        var data = Data.Value;
        Assert.Equal(72, data.GroupSchedule.Count);
        foreach (var group in data.Groups)
        {
            var fixtures = data.GroupSchedule.Where(f => f.Group == group).ToList();
            Assert.Equal(6, fixtures.Count); // C(4,2)
            // Each pair appears exactly once.
            var pairs = fixtures.Select(f => f.HomeCode + "-" + f.AwayCode).ToHashSet();
            Assert.Equal(6, pairs.Count);
        }
    }

    [Fact]
    public void Every_Team_Has_A_Squad_Sufficient_For_A_4_3_3()
    {
        var data = Data.Value;
        foreach (var team in data.Teams)
        {
            Assert.True(team.Squad.Count >= 16, $"{team.Code} squad too small: {team.Squad.Count}");
            Assert.True(team.PlayersAt(WorldCup.Data.Models.Position.GK).Any(), $"{team.Code} has no GK");
            Assert.True(team.PlayersAt(WorldCup.Data.Models.Position.DEF).Count() >= 4, $"{team.Code} <4 DEF");
            Assert.True(team.PlayersAt(WorldCup.Data.Models.Position.MID).Count() >= 3, $"{team.Code} <3 MID");
            Assert.True(team.PlayersAt(WorldCup.Data.Models.Position.FWD).Count() >= 3, $"{team.Code} <3 FWD");
        }
    }

    [Fact]
    public void Real_Squads_Are_Loaded_With_Known_Stars()
    {
        var data = Data.Value;
        // squads_2026.json is bundled, so squads are real (not synthetic).
        Assert.False(data.Team("ARG").IsSyntheticSquad);
        Assert.Contains(data.Team("ARG").Squad, p => p.Name.Contains("Messi"));
        Assert.Contains(data.Team("FRA").Squad, p => p.Name.Contains("Mbappé"));
        Assert.Contains(data.Team("POR").Squad, p => p.Name.Contains("Ronaldo"));
    }

    [Fact]
    public void Squad_Loading_Is_Deterministic()
    {
        var first = new SeedTeamDataProvider().GetTournamentData().Team("BRA").Squad.Select(p => p.Name).ToList();
        var second = new SeedTeamDataProvider().GetTournamentData().Team("BRA").Squad.Select(p => p.Name).ToList();
        Assert.Equal(first, second);
    }
}
