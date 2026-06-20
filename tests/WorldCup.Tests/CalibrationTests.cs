using WorldCup.Data.Providers;
using WorldCup.Engine.Calibration;
using WorldCup.Engine.Parameters;
using Xunit;

namespace WorldCup.Tests;

public class CalibrationTests
{
    [Fact]
    public void Default_Parameters_Land_In_Band_Out_Of_The_Box()
    {
        var data = new SeedTeamDataProvider().GetTournamentData();
        var p = SimulationParameters.CreateStarting();

        // Measure is deterministic (seeded by the parameter seed), so this is not flaky.
        var report = CalibrationRunner.Measure(data, p, 8_000);

        foreach (var metric in report.Metrics)
        {
            Assert.True(metric.InBand,
                $"{metric.Name} measured {metric.Measured:0.000} is outside target {metric.Target:0.000} ± {metric.Tolerance:0.000}");
        }
    }

    [Fact]
    public void AutoTune_Improves_Or_Holds_Metrics()
    {
        var data = new SeedTeamDataProvider().GetTournamentData();
        var p = SimulationParameters.CreateStarting();
        p.Global.Events.CornersPerMatch = 18; // deliberately off-target

        var (_, before, after) = CalibrationRunner.AutoTune(data, p, matchesPerIteration: 3_000, maxIterations: 8);

        var beforeCorners = before.Metrics.First(m => m.Name == "Corners / match");
        var afterCorners = after.Metrics.First(m => m.Name == "Corners / match");
        Assert.False(beforeCorners.InBand);
        Assert.True(Math.Abs(afterCorners.Delta) < Math.Abs(beforeCorners.Delta));
    }
}
