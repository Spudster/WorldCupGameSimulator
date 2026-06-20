using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Stats;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

public class VergazoTests
{
    [Fact]
    public void Vergazo_Respects_Style_Caps_And_OwnGoal_Limit()
    {
        var data = new SeedTeamDataProvider().GetTournamentData();
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(2026);

        bool sawBicycle = false;
        double maxNonBicycle = 0;
        int totalGoals = 0;

        for (int i = 0; i < 2_000; i++)
        {
            var m = DetailedMatchSimulator.Simulate(data.Team("BRA"), data.Team("ARG"), Stage.Group, p, ref rng, true);
            foreach (var g in m.Goals)
            {
                totalGoals++;
                Assert.InRange(g.Vergazo, 1.0, 10.0);

                if (g.IsOwnGoal)
                {
                    Assert.True(g.Vergazo <= 3.0, $"own goal vergazo {g.Vergazo} exceeded 3");
                }

                if (g.IsPenalty)
                {
                    Assert.True(g.Vergazo <= 4.0, $"penalty vergazo {g.Vergazo} exceeded 4");
                }

                // Only a bicycle kick may exceed 9.5/10 (the route to a perfect 10).
                if (g.Vergazo > 9.5)
                {
                    Assert.Equal(GoalType.BicycleKick, g.Type);
                }

                if (g.Type == GoalType.BicycleKick)
                {
                    sawBicycle = true;
                }
                else
                {
                    maxNonBicycle = Math.Max(maxNonBicycle, g.Vergazo);
                }
            }
        }

        Assert.True(totalGoals > 100);
        Assert.True(sawBicycle, "expected at least one bicycle kick across 2000 detailed matches");
        Assert.True(maxNonBicycle <= 9.5 + 1e-9, $"a non-bicycle goal scored {maxNonBicycle} (>9.5)");
    }

    [Fact]
    public void Vergazo_Record_Tracker_Picks_The_Best_Goal()
    {
        var trackers = RecordTrackers.CreateDefault();
        var screamer = new GoalEvent(62, "X", "x1", "Wonder", null, null,
            GoalType.BicycleKick, 31.0, IsPenalty: false, IsOwnGoal: false, DefendersPassed: 3, Vergazo: 9.8);
        var tapIn = new GoalEvent(12, "Y", "y1", "Poacher", "y2", "Maker",
            GoalType.OpenPlay, 4.0, IsPenalty: false, IsOwnGoal: false, DefendersPassed: 0, Vergazo: 2.4);

        var match = new MatchResult
        {
            Fidelity = Fidelity.Detailed,
            Stage = Stage.Final,
            HomeCode = "X",
            AwayCode = "Y",
            HomeName = "X",
            AwayName = "Y",
            HomeGoals = 1,
            AwayGoals = 1,
            Method = MatchMethod.Regulation,
            WinnerCode = "X",
            Goals = new[] { screamer, tapIn },
        };

        trackers.ForEach(t => t.Observe(match, "X", "Y"));
        var record = trackers.First(t => t.Category.Contains("vergazo")).Result!;
        Assert.Equal(9.8, record.Value, 3);
        Assert.Contains("Wonder", record.Description);
    }
}
