using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

public class GroupPathTests
{
    private static TournamentData LoadData() => new SeedTeamDataProvider().GetTournamentData();

    // Real matchday-1 results for Group C (Brazil, Morocco, Scotland, Haiti).
    private static readonly PlayedResult[] GroupCMatchday1 =
    {
        new("BRA", "MAR", 1, 1),
        new("SCO", "HAI", 1, 0),
    };

    private static GroupPathAnalysis Analyze(
        IReadOnlyList<PlayedResult> played, char group = 'C', string team = "BRA", long n = 40_000) =>
        GroupPathAnalyzer.Analyze(LoadData(), group, team, SimulationParameters.CreateStarting(), played, n, 12345);

    private static GroupOutlook AnalyzeGroup(
        IReadOnlyList<PlayedResult> played, char group = 'C', long n = 40_000) =>
        GroupPathAnalyzer.AnalyzeGroup(LoadData(), group, SimulationParameters.CreateStarting(), played, n, 12345);

    [Fact]
    public void WholeGroup_Covers_All_Four_Teams_With_Consistent_Probabilities()
    {
        var g = AnalyzeGroup(GroupCMatchday1);

        Assert.Equal(4, g.Teams.Count);
        Assert.False(g.GroupComplete);

        // Exactly one team wins the group and exactly two advance directly in each simulation,
        // so across the four teams the shares sum to 1 and 2 respectively.
        Assert.Equal(1.0, g.Teams.Sum(t => t.WinGroup), 6);
        Assert.Equal(2.0, g.Teams.Sum(t => t.AdvanceDirect), 6);
        Assert.Equal(1.0, g.Teams.Sum(t => t.ThirdPlace), 6);
        Assert.Equal(1.0, g.Teams.Sum(t => t.Eliminated), 6);

        // Each team's own four finishing tiers sum to 1.
        foreach (var t in g.Teams)
        {
            Assert.Equal(1.0, t.WinGroup + t.RunnerUp + t.ThirdPlace + t.Eliminated, 6);
            Assert.Equal(t.WinGroup + t.RunnerUp, t.AdvanceDirect, 9);
        }

        // Rows are in current-standings order.
        var ranks = g.Teams.Select(t => t.Standing.Rank).ToList();
        Assert.Equal(new[] { 1, 2, 3, 4 }, ranks);
    }

    [Fact]
    public void WholeGroup_Matches_The_Single_Team_Analysis()
    {
        // The combined pass should agree closely with the per-team analysis (same seed, same model).
        var g = AnalyzeGroup(GroupCMatchday1);
        var bra = g.Teams.Single(t => t.Standing.Code == "BRA");
        var solo = Analyze(GroupCMatchday1, team: "BRA");

        Assert.Equal(solo.WinGroup, bra.WinGroup, 1);
        Assert.Equal(solo.AdvanceDirect, bra.AdvanceDirect, 1);
    }

    [Fact]
    public void TierProbabilities_Sum_To_One()
    {
        var a = Analyze(GroupCMatchday1);
        double sum = a.WinGroup + a.RunnerUp + a.ThirdPlace + a.Eliminated;
        Assert.Equal(1.0, sum, 6);
        Assert.Equal(a.WinGroup + a.RunnerUp, a.AdvanceDirect, 9);
        Assert.InRange(a.WinGroup, 0.0, 1.0);
    }

    [Fact]
    public void Standings_Reflect_The_Played_Results()
    {
        var a = Analyze(GroupCMatchday1);

        var bra = a.Standings.Single(s => s.Code == "BRA");
        Assert.True(bra.IsSelected);
        Assert.Equal(1, bra.Played);
        Assert.Equal(1, bra.Points); // 1-1 draw with Morocco

        var sco = a.Standings.Single(s => s.Code == "SCO");
        Assert.Equal(3, sco.Points); // beat Haiti
        Assert.Equal(1, sco.Rank);   // leads the group on points
    }

    [Fact]
    public void Four_Remaining_Matches_Give_81_Combinations()
    {
        var a = Analyze(GroupCMatchday1);
        Assert.Equal(81, a.TotalCombinations);      // 3^4
        Assert.Equal(4, a.RemainingFixtures.Count);
        Assert.Equal(2, a.OwnRemaining);            // Brazil v Scotland, Brazil v Haiti
        Assert.False(a.GroupComplete);
    }

    [Fact]
    public void VictoryScenarios_Can_Win_And_DefeatScenarios_Finish_Last()
    {
        var a = Analyze(GroupCMatchday1);

        Assert.NotEmpty(a.VictoryScenarios);
        Assert.All(a.VictoryScenarios, s => Assert.Equal(1, s.BestRank));    // winning the group is reachable
        Assert.All(a.DefeatScenarios, s => Assert.Equal(4, s.WorstRank));    // finishing last is reachable
    }

    [Fact]
    public void VictoryMass_Bounds_The_Win_Probability()
    {
        // Every combination where winning is *possible* contributes its full mass, so the enumerated
        // victory mass is an upper bound on the simulated win probability (modulo MC noise).
        var a = Analyze(GroupCMatchday1);
        Assert.True(a.VictoryMass + 0.02 >= a.WinGroup,
            $"victory mass {a.VictoryMass:F3} should bound win prob {a.WinGroup:F3}");
        Assert.True(a.DefeatMass + 0.02 >= a.Eliminated,
            $"defeat mass {a.DefeatMass:F3} should bound elimination prob {a.Eliminated:F3}");
    }

    [Fact]
    public void OwnResultBranches_Are_Ordered_By_Points_Descending()
    {
        var a = Analyze(GroupCMatchday1);
        Assert.NotEmpty(a.OwnResultBranches);
        var pts = a.OwnResultBranches.Select(b => b.PointsGained).ToList();
        Assert.Equal(pts.OrderByDescending(x => x).ToList(), pts);
    }

    [Fact]
    public void Completed_Group_Settles_On_The_Final_Rank()
    {
        // A full Group C in which Brazil win every game: 9 pts and clear first.
        var played = new[]
        {
            new PlayedResult("BRA", "MAR", 2, 0),
            new PlayedResult("BRA", "SCO", 2, 0),
            new PlayedResult("BRA", "HAI", 3, 0),
            new PlayedResult("MAR", "SCO", 1, 0),
            new PlayedResult("MAR", "HAI", 2, 0),
            new PlayedResult("SCO", "HAI", 1, 0),
        };

        var a = Analyze(played);

        Assert.True(a.GroupComplete);
        Assert.Equal(1, a.FinalRankIfComplete);
        Assert.Equal(1.0, a.WinGroup, 6);
        Assert.Equal(0.0, a.Eliminated, 6);
        Assert.True(a.ClinchedWinGroup);
        Assert.True(a.ClinchedAdvance);
        Assert.False(a.CannotWinGroup);
        Assert.True(a.CannotFinishLast);
        Assert.Empty(a.RemainingFixtures);
    }

    [Fact]
    public void Eliminated_Team_In_Completed_Group_Has_The_Right_Flags()
    {
        // Same complete group, but analyse Haiti — they lost all three and finish last.
        var played = new[]
        {
            new PlayedResult("BRA", "MAR", 2, 0),
            new PlayedResult("BRA", "SCO", 2, 0),
            new PlayedResult("BRA", "HAI", 3, 0),
            new PlayedResult("MAR", "SCO", 1, 0),
            new PlayedResult("MAR", "HAI", 2, 0),
            new PlayedResult("SCO", "HAI", 1, 0),
        };

        var a = Analyze(played, team: "HAI");

        Assert.True(a.GroupComplete);
        Assert.Equal(4, a.FinalRankIfComplete);
        Assert.Equal(1.0, a.Eliminated, 6);
        Assert.True(a.CannotWinGroup);
        Assert.True(a.CannotAdvance);
        Assert.False(a.CannotFinishLast);
    }

    [Fact]
    public void Unknown_Team_For_Group_Throws()
    {
        Assert.Throws<ArgumentException>(() => Analyze(GroupCMatchday1, team: "ARG"));
    }
}
