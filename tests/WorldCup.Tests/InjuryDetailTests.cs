using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using Xunit;

namespace WorldCup.Tests;

public class InjuryDetailTests
{
    [Theory]
    [InlineData(0, "plays on")]
    [InlineData(1, "~1 day")]
    [InlineData(3, "~3 days")]
    [InlineData(7, "~1 week")]
    [InlineData(14, "~2 weeks")]
    [InlineData(30, "~1 month")]
    [InlineData(240, "~8 months")]
    public void RecoveryText_Reads_Naturally(int days, string expected)
    {
        Assert.Equal(expected, InjuryCatalog.RecoveryText(days));
    }

    [Fact]
    public void Diagnose_Gives_A_Specific_Body_Part_Diagnosis_And_Recovery()
    {
        var rng = new Xoshiro256(123);
        foreach (var severity in new[] { InjurySeverity.Knock, InjurySeverity.Minor, InjurySeverity.Major })
        {
            for (int i = 0; i < 50; i++)
            {
                var (bodyPart, diagnosis, days) = InjuryCatalog.Diagnose(severity, ref rng);
                Assert.False(string.IsNullOrWhiteSpace(bodyPart));
                Assert.False(string.IsNullOrWhiteSpace(diagnosis));
                Assert.True(days >= 0);
            }
        }
    }

    [Fact]
    public void Recovery_Scales_With_Severity()
    {
        var rng = new Xoshiro256(7);
        double knock = AverageDays(InjurySeverity.Knock, ref rng);
        double minor = AverageDays(InjurySeverity.Minor, ref rng);
        double major = AverageDays(InjurySeverity.Major, ref rng);

        Assert.True(knock < minor, $"knock {knock:F1} should be shorter than minor {minor:F1}");
        Assert.True(minor < major, $"minor {minor:F1} should be shorter than major {major:F1}");
        Assert.True(major > 60, $"major average {major:F1} should be months, not days");
    }

    private static double AverageDays(InjurySeverity severity, ref Xoshiro256 rng)
    {
        long sum = 0;
        const int n = 400;
        for (int i = 0; i < n; i++)
        {
            sum += InjuryCatalog.Diagnose(severity, ref rng).RecoveryDays;
        }

        return (double)sum / n;
    }

    [Fact]
    public void Simulated_Injuries_Carry_A_Diagnosis_And_Recovery()
    {
        var data = new SeedTeamDataProvider().GetTournamentData();
        var p = SimulationParameters.CreateStarting();
        p.Global.Events.InjuriesPerMatch = 6; // raise the rate so the test reliably sees injuries
        var rng = new Xoshiro256(42);

        int seen = 0;
        for (int i = 0; i < 400; i++)
        {
            var r = MatchSimulator.Simulate(data.Team("BRA"), data.Team("HAI"), Stage.Group, Fidelity.Detailed, p, ref rng, true);
            foreach (var inj in r.Injuries)
            {
                seen++;
                Assert.False(string.IsNullOrWhiteSpace(inj.BodyPart));
                Assert.False(string.IsNullOrWhiteSpace(inj.Diagnosis));
                // A knock can be "plays on" (0 days); anything more serious must carry real time.
                if (inj.Severity != InjurySeverity.Knock)
                {
                    Assert.True(inj.RecoveryDays >= 7, $"{inj.Severity} should be at least a week, was {inj.RecoveryDays}");
                }
            }
        }

        Assert.True(seen > 50, $"expected to see plenty of injuries, saw {seen}");
    }
}
