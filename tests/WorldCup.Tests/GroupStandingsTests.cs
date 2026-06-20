using WorldCup.Engine.Random;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

public class GroupStandingsTests
{
    private static List<TeamStanding> Rank(IReadOnlyList<string> teams, IReadOnlyList<GroupMatchOutcome> matches, ulong seed = 1)
    {
        var rng = new Xoshiro256(seed);
        return GroupStandingsCalculator.Compute('A', teams, matches, ref rng);
    }

    [Fact]
    public void Points_Then_GoalDifference_Then_GoalsScored()
    {
        var teams = new[] { "A", "B", "C", "D" };
        var matches = new[]
        {
            // A wins all (9 pts). B and C tie on points; B has better GD; D last.
            new GroupMatchOutcome("A", "B", 1, 0, 0, 0),
            new GroupMatchOutcome("A", "C", 1, 0, 0, 0),
            new GroupMatchOutcome("A", "D", 1, 0, 0, 0),
            new GroupMatchOutcome("B", "C", 0, 0, 0, 0),
            new GroupMatchOutcome("B", "D", 5, 0, 0, 0),
            new GroupMatchOutcome("C", "D", 1, 0, 0, 0),
        };

        var table = Rank(teams, matches);
        Assert.Equal("A", table[0].Code);
        Assert.Equal("B", table[1].Code); // 4 pts, GD +5
        Assert.Equal("C", table[2].Code); // 4 pts, GD +1
        Assert.Equal("D", table[3].Code);
        Assert.Equal(1, table[0].Rank);
        Assert.Equal(9, table[0].Points);
    }

    [Fact]
    public void HeadToHead_Breaks_Ties_When_Points_Gd_Goals_Equal()
    {
        // Construction: D dominates (7 pts). A and B tie on pts=4, GF=2, GA=2, GD=0; A beat B head-to-head.
        var teams = new[] { "A", "B", "C", "D" };
        var matches = new[]
        {
            new GroupMatchOutcome("A", "B", 1, 0, 0, 0),
            new GroupMatchOutcome("A", "C", 1, 1, 0, 0),
            new GroupMatchOutcome("A", "D", 0, 1, 0, 0),
            new GroupMatchOutcome("B", "C", 1, 0, 0, 0),
            new GroupMatchOutcome("B", "D", 1, 1, 0, 0),
            new GroupMatchOutcome("C", "D", 0, 2, 0, 0),
        };

        var table = Rank(teams, matches);
        Assert.Equal("D", table[0].Code);
        Assert.Equal("A", table[1].Code); // tied with B but won head-to-head
        Assert.Equal("B", table[2].Code);
        Assert.Equal("C", table[3].Code);

        // Confirm A and B really were tied on the primary keys.
        var a = table.First(t => t.Code == "A");
        var b = table.First(t => t.Code == "B");
        Assert.Equal(a.Points, b.Points);
        Assert.Equal(a.GoalDifference, b.GoalDifference);
        Assert.Equal(a.GoalsFor, b.GoalsFor);
    }

    [Fact]
    public void FairPlay_Breaks_Ties_When_HeadToHead_Equal()
    {
        // A and B are identical (drew each other) and beat C, D the same way; A has fewer cards.
        var teams = new[] { "A", "B", "C", "D" };
        var matches = new[]
        {
            new GroupMatchOutcome("A", "B", 0, 0, 0, 5), // A fair-play 0, B fair-play 5
            new GroupMatchOutcome("A", "C", 2, 0, 0, 0),
            new GroupMatchOutcome("A", "D", 2, 0, 0, 0),
            new GroupMatchOutcome("B", "C", 2, 0, 0, 0),
            new GroupMatchOutcome("B", "D", 2, 0, 0, 0),
            new GroupMatchOutcome("C", "D", 0, 0, 0, 0),
        };

        var table = Rank(teams, matches);
        Assert.Equal("A", table[0].Code); // fewer cards
        Assert.Equal("B", table[1].Code);
    }

    [Fact]
    public void BestThirdPlaced_Ranked_By_Points_Then_Gd_Then_Goals()
    {
        var thirds = new List<TeamStanding>
        {
            new("T1") { Group = 'A', Won = 1, Drawn = 0, Lost = 2, GoalsFor = 3, GoalsAgainst = 5 }, // 3 pts, GD -2
            new("T2") { Group = 'B', Won = 1, Drawn = 1, Lost = 1, GoalsFor = 4, GoalsAgainst = 4 }, // 4 pts, GD 0
            new("T3") { Group = 'C', Won = 1, Drawn = 1, Lost = 1, GoalsFor = 6, GoalsAgainst = 6 }, // 4 pts, GD 0, more GF
            new("T4") { Group = 'D', Won = 0, Drawn = 1, Lost = 2, GoalsFor = 1, GoalsAgainst = 7 }, // 1 pt
        };

        var rng = new Xoshiro256(7);
        var ranked = GroupStandingsCalculator.RankThirdPlaced(thirds, ref rng);
        Assert.Equal("T3", ranked[0].Code); // 4 pts, GD 0, GF 6
        Assert.Equal("T2", ranked[1].Code); // 4 pts, GD 0, GF 4
        Assert.Equal("T1", ranked[2].Code); // 3 pts
        Assert.Equal("T4", ranked[3].Code); // 1 pt
    }
}
