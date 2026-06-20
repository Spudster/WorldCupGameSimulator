using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Stats;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

public class StatsTests
{
    private static TournamentData LoadData() => new SeedTeamDataProvider().GetTournamentData();

    [Fact]
    public void Detailed_Match_Goal_Tallies_Sum_To_Score()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(123);
        var home = data.Team("BRA");
        var away = data.Team("HAI");

        for (int i = 0; i < 300; i++)
        {
            var m = DetailedMatchSimulator.Simulate(home, away, Stage.Group, p, ref rng, neutralVenue: true);

            Assert.Equal(m.HomeGoals + m.AwayGoals, m.Goals.Count);
            Assert.Equal(m.HomeGoals, m.Goals.Count(g => g.TeamCode == home.Code));
            Assert.Equal(m.AwayGoals, m.Goals.Count(g => g.TeamCode == away.Code));

            // Box-score card counts equal the card-event counts.
            Assert.Equal(m.Cards.Count(c => c.TeamCode == home.Code && !c.IsRed), m.HomeBox!.Yellows);
            Assert.Equal(m.Cards.Count(c => c.TeamCode == home.Code && c.IsRed), m.HomeBox!.Reds);
        }
    }

    [Fact]
    public void SecondYellow_Produces_A_Red()
    {
        // Force a very high yellow rate so a second yellow is likely; confirm bookkeeping holds.
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        p.Global.Events.YellowCardsPerMatch = 30;
        var rng = new Xoshiro256(7);

        bool sawSecondYellowRed = false;
        for (int i = 0; i < 200 && !sawSecondYellowRed; i++)
        {
            var m = DetailedMatchSimulator.Simulate(data.Team("BRA"), data.Team("ARG"), Stage.Group, p, ref rng, true);
            foreach (var red in m.Cards.Where(c => c.IsRed && c.IsSecondYellow))
            {
                sawSecondYellowRed = true;
                // The player must have two yellow cards recorded.
                int yellows = m.Cards.Count(c => c.PlayerId == red.PlayerId && !c.IsRed);
                Assert.True(yellows >= 2);
            }
        }

        Assert.True(sawSecondYellowRed);
    }

    [Fact]
    public void StatsAggregator_Builds_Sorted_Leaderboards()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        var agg = new TournamentStatsAggregator(data, p.Global.Mvp);

        var sim = new TournamentSimulator(data, p);
        var rng = new Xoshiro256(2026);
        for (int i = 0; i < 3; i++)
        {
            agg.Add(sim.Simulate(Fidelity.Detailed, ref rng));
        }

        var report = agg.Build();
        Assert.Equal(3, report.Tournaments);

        // Golden Boot sorted by goals descending.
        for (int i = 1; i < report.GoldenBoot.Count; i++)
        {
            Assert.True(report.GoldenBoot[i - 1].Goals >= report.GoldenBoot[i].Goals);
        }

        // MVP scores positive and sorted.
        Assert.NotEmpty(report.Mvp);
        for (int i = 1; i < report.Mvp.Count; i++)
        {
            Assert.True(report.Mvp[i - 1].Score >= report.Mvp[i].Score);
        }

        Assert.NotEmpty(report.CrazyStats);
        Assert.Contains(report.CrazyStats, r => r.Category.Contains("Longest goal"));
    }

    [Fact]
    public void RecordTrackers_Pick_The_Right_Extremes()
    {
        var trackers = RecordTrackers.CreateDefault();

        Observe(trackers, MakeMatch(goals: new[]
        {
            Goal(10, "X", "x1", "Alpha", GoalType.OpenPlay, 8),
            Goal(89, "Y", "y1", "Beta", GoalType.LongRange, 31.5),
        }));
        Observe(trackers, MakeMatch(goals: new[]
        {
            Goal(3, "X", "x2", "Gamma", GoalType.OpenPlay, 12),
        }));

        var longest = trackers.First(t => t.Category.Contains("Longest goal")).Result!;
        Assert.Equal(31.5, longest.Value, 3);
        Assert.Contains("Beta", longest.Description);

        var fastest = trackers.First(t => t.Category == "Fastest goal").Result!;
        Assert.Equal(3, fastest.Value);

        var latest = trackers.First(t => t.Category.Contains("Latest goal")).Result!;
        Assert.Equal(89, latest.Value);
    }

    [Fact]
    public void MostGoalsByPlayer_Detects_Hat_Trick()
    {
        var trackers = RecordTrackers.CreateDefault();
        Observe(trackers, MakeMatch(goals: new[]
        {
            Goal(10, "X", "x1", "Striker", GoalType.OpenPlay, 9),
            Goal(40, "X", "x1", "Striker", GoalType.Header, 6),
            Goal(70, "X", "x1", "Striker", GoalType.Penalty, 11),
        }));

        var record = trackers.First(t => t.Category.Contains("Most goals by a player")).Result!;
        Assert.Equal(3, record.Value);
        Assert.Contains("Striker", record.Description);
    }

    private static void Observe(List<IRecordTracker> trackers, MatchResult m) =>
        trackers.ForEach(t => t.Observe(m, m.HomeName, m.AwayName));

    private static GoalEvent Goal(int minute, string team, string id, string name, GoalType type, double dist) =>
        new(minute, team, id, name, null, null, type, dist, type == GoalType.Penalty, false);

    private static MatchResult MakeMatch(GoalEvent[] goals) => new()
    {
        Fidelity = Fidelity.Detailed,
        Stage = Stage.Group,
        HomeCode = "X",
        AwayCode = "Y",
        HomeName = "X",
        AwayName = "Y",
        HomeGoals = goals.Count(g => g.TeamCode == "X"),
        AwayGoals = goals.Count(g => g.TeamCode == "Y"),
        Method = MatchMethod.Regulation,
        WinnerCode = "X",
        Goals = goals,
    };
}
