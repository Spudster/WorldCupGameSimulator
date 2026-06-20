using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

public class BracketTests
{
    private static readonly BracketDefinition Bracket = OfficialBracket2026.Build();

    [Fact]
    public void Bracket_Has_32_Matches_Through_Final()
    {
        Assert.Equal(32, Bracket.Matches.Count);
        Assert.Equal(16, Bracket.Matches.Count(m => m.Stage == Stage.RoundOf32));
        Assert.Equal(8, Bracket.Matches.Count(m => m.Stage == Stage.RoundOf16));
        Assert.Equal(4, Bracket.Matches.Count(m => m.Stage == Stage.QuarterFinal));
        Assert.Equal(2, Bracket.Matches.Count(m => m.Stage == Stage.SemiFinal));
        Assert.Equal(1, Bracket.Matches.Count(m => m.Stage == Stage.ThirdPlacePlayoff));
        Assert.Equal(1, Bracket.Matches.Count(m => m.Stage == Stage.Final));
    }

    [Fact]
    public void Later_Matches_Only_Reference_Earlier_Matches()
    {
        foreach (var m in Bracket.Matches)
        {
            foreach (var feeder in new[] { m.Top, m.Bottom })
            {
                if (feeder.Kind is FeederKind.MatchWinner or FeederKind.MatchLoser)
                {
                    Assert.True(feeder.MatchId < m.Id, $"Match {m.Id} references later match {feeder.MatchId}");
                }
            }
        }
    }

    [Fact]
    public void Final_Is_Fed_By_The_Two_Semifinals()
    {
        var final = Bracket.Matches.Single(m => m.Stage == Stage.Final);
        Assert.Equal(FeederKind.MatchWinner, final.Top.Kind);
        Assert.Equal(FeederKind.MatchWinner, final.Bottom.Kind);
        Assert.Equal(101, final.Top.MatchId);
        Assert.Equal(102, final.Bottom.MatchId);
    }

    [Fact]
    public void R32_Uses_Each_Group_Winner_And_RunnerUp_Exactly_Once()
    {
        var winners = new List<char>();
        var runners = new List<char>();
        foreach (var m in Bracket.Matches.Where(m => m.Stage == Stage.RoundOf32))
        {
            foreach (var f in new[] { m.Top, m.Bottom })
            {
                if (f.Kind == FeederKind.GroupSlot)
                {
                    if (f.Slot.Kind == SlotSpecKind.Winner) winners.Add(f.Slot.Group);
                    else if (f.Slot.Kind == SlotSpecKind.RunnerUp) runners.Add(f.Slot.Group);
                }
            }
        }

        Assert.Equal(12, winners.Distinct().Count());
        Assert.Equal(12, runners.Distinct().Count());
    }

    [Fact]
    public void Eight_WinnerSlots_Host_A_ThirdPlacedTeam()
    {
        int thirdSlots = Bracket.Matches
            .Where(m => m.Stage == Stage.RoundOf32)
            .SelectMany(m => new[] { m.Top, m.Bottom })
            .Count(f => f.Kind == FeederKind.GroupSlot && f.Slot.Kind == SlotSpecKind.ThirdForWinner);
        Assert.Equal(8, thirdSlots);
        Assert.Equal(8, Bracket.ThirdPlaceWinnerGroups.Count);
    }

    [Theory]
    [InlineData("ABCDEFGH")]
    [InlineData("EFGHIJKL")]
    [InlineData("CDEFGHIJ")]
    [InlineData("ABEFIJKL")]
    public void ThirdPlaceAssignment_Is_Valid_And_Never_SameGroup(string qualifying)
    {
        var qualifyingGroups = qualifying.ToCharArray();
        var assignment = ThirdPlaceAssigner.Assign(
            Bracket.ThirdPlaceWinnerGroups, qualifyingGroups, Bracket.ThirdPlaceEligibleGroups);

        // Every winner slot gets a distinct source group from the qualifying set, never its own group.
        Assert.Equal(Bracket.ThirdPlaceWinnerGroups.Count, assignment.Count);
        Assert.Equal(assignment.Count, assignment.Values.Distinct().Count());
        foreach (var (winnerGroup, source) in assignment)
        {
            Assert.NotEqual(winnerGroup, source);
            Assert.Contains(source, qualifyingGroups);
        }
    }
}
