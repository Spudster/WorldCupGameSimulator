using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Reporting;
using Xunit;

namespace WorldCup.Tests;

public class ErrorsAndControversyTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    private static (int Matches, int Goals, int ErrorGoals, int Errors, int BadCalls, int Var)
        Simulate(SimulationParameters p, int matches, ulong seed = 99)
    {
        var data = Data;
        var home = data.Team("BRA");
        var away = data.Team("HAI"); // a lopsided pairing produces plenty of goals to attribute
        var rng = new Xoshiro256(seed);
        int goals = 0, errorGoals = 0, errors = 0, badCalls = 0, var = 0;
        for (int i = 0; i < matches; i++)
        {
            var r = MatchSimulator.Simulate(home, away, Stage.Group, Fidelity.Detailed, p, ref rng, neutralVenue: true);
            goals += r.HomeGoals + r.AwayGoals;
            errorGoals += r.Goals.Count(g => g.CausedByError != ErrorKind.None);
            errors += r.Errors.Count;
            badCalls += r.BadCalls.Count;
            var += r.BadCalls.Count(bc => bc.VarChecked);
        }

        return (matches, goals, errorGoals, errors, badCalls, var);
    }

    [Fact]
    public void Errors_And_BadCalls_Occur_At_Realistic_Rates()
    {
        const int matches = 4_000;
        var s = Simulate(SimulationParameters.CreateStarting(), matches);

        // Some open-play goals are gifted by an error, but only a realistic minority of them.
        double errorGoalShare = (double)s.ErrorGoals / s.Goals;
        Assert.InRange(errorGoalShare, 0.03, 0.30);

        // Every match has, on average, around one mistake and a sub-one-to-two-per-game controversy load.
        double errorsPerMatch = (double)s.Errors / matches;
        double badCallsPerMatch = (double)s.BadCalls / matches;
        Assert.InRange(errorsPerMatch, 0.5, 4.0);
        Assert.InRange(badCallsPerMatch, 0.30, 2.0);

        // VAR is involved in some — but not all — of the bad calls.
        Assert.InRange((double)s.Var / Math.Max(1, s.BadCalls), 0.2, 0.8);
    }

    [Fact]
    public void Error_Share_Is_Tunable()
    {
        const int matches = 2_000;
        var baseline = Simulate(SimulationParameters.CreateStarting(), matches);

        var cranked = SimulationParameters.CreateStarting();
        cranked.Global.Events.DefensiveErrorGoalShare = 0.5;
        cranked.Global.Events.GoalkeeperErrorGoalShare = 0.2;
        var high = Simulate(cranked, matches);

        // Far more goals are attributed to errors once the shares are raised.
        double baseShare = (double)baseline.ErrorGoals / baseline.Goals;
        double highShare = (double)high.ErrorGoals / high.Goals;
        Assert.True(highShare > baseShare + 0.15, $"high {highShare:F3} should clearly exceed base {baseShare:F3}");
    }

    [Fact]
    public void Disabling_The_Rates_Removes_Player_Errors()
    {
        var p = SimulationParameters.CreateStarting();
        p.Global.Events.DefensiveErrorGoalShare = 0;
        p.Global.Events.GoalkeeperErrorGoalShare = 0;
        p.Global.Events.UnpunishedErrorsPerMatch = 0;

        var s = Simulate(p, 1_500);
        Assert.Equal(0, s.Errors);     // no player/keeper errors at all
        Assert.Equal(0, s.ErrorGoals); // and no goals tagged as gifted
        Assert.True(s.Goals > 0);      // the match still produced goals
    }

    [Fact]
    public void Error_Goals_Are_Tagged_On_Real_Goals_And_Never_Spectacular()
    {
        var p = SimulationParameters.CreateStarting();
        p.Global.Events.GoalkeeperErrorGoalShare = 0.4; // force plenty for the assertion
        var data = Data;
        var rng = new Xoshiro256(7);

        bool sawKeeperErrorGoal = false;
        for (int i = 0; i < 800 && !sawKeeperErrorGoal; i++)
        {
            var r = MatchSimulator.Simulate(data.Team("BRA"), data.Team("HAI"), Stage.Group, Fidelity.Detailed, p, ref rng, true);
            foreach (var g in r.Goals.Where(x => x.CausedByError == ErrorKind.GoalkeeperError))
            {
                sawKeeperErrorGoal = true;
                Assert.False(g.IsOwnGoal);         // it's a real goal for the scoring side
                Assert.True(g.Vergazo <= 4.0);     // a gifted goal is never a screamer
                // A matching error event was logged against the conceding team.
                Assert.Contains(r.Errors, e => e.LedToGoal && e.Kind == ErrorKind.GoalkeeperError && e.Minute == g.Minute);
            }
        }

        Assert.True(sawKeeperErrorGoal, "expected at least one goalkeeper-error goal with the share cranked up");
    }

    [Fact]
    public void Errors_And_BadCalls_Carry_Specific_Descriptions()
    {
        var p = SimulationParameters.CreateStarting();
        p.Global.Events.GoalkeeperErrorGoalShare = 0.3;
        p.Global.Events.RefereeMistakesPerMatch = 2.0;
        var data = Data;
        var rng = new Xoshiro256(11);

        int errorsChecked = 0, badCallsChecked = 0;
        for (int i = 0; i < 600; i++)
        {
            var r = MatchSimulator.Simulate(data.Team("BRA"), data.Team("HAI"), Stage.Group, Fidelity.Detailed, p, ref rng, true);
            foreach (var e in r.Errors)
            {
                errorsChecked++;
                // The description is a specific action phrase, not blank and not merely the player's name.
                Assert.False(string.IsNullOrWhiteSpace(e.Description));
                Assert.NotEqual(e.PlayerName, e.Description);
                Assert.True(e.Description.Length > 6);
            }

            foreach (var bc in r.BadCalls)
            {
                badCallsChecked++;
                Assert.False(string.IsNullOrWhiteSpace(bc.Description));
                Assert.True(bc.Description.Length > 6);
            }
        }

        Assert.True(errorsChecked > 50, $"expected many errors, saw {errorsChecked}");
        Assert.True(badCallsChecked > 50, $"expected many bad calls, saw {badCallsChecked}");
    }

    [Fact]
    public void Match_Html_Renders_The_Errors_And_Controversy_Sections()
    {
        var p = SimulationParameters.CreateStarting();
        p.Global.Events.GoalkeeperErrorGoalShare = 0.5;
        p.Global.Events.RefereeMistakesPerMatch = 3.0;
        var data = Data;
        var rng = new Xoshiro256(3);

        // Find a simulated match that actually has both kinds of event to render.
        MatchResult? match = null;
        for (int i = 0; i < 300 && match is null; i++)
        {
            var r = MatchSimulator.Simulate(data.Team("BRA"), data.Team("HAI"), Stage.Group, Fidelity.Detailed, p, ref rng, true);
            if (r.Errors.Count > 0 && r.BadCalls.Count > 0)
            {
                match = r;
            }
        }

        Assert.NotNull(match);

        string path = Path.Combine(Path.GetTempPath(), "wc_test_match_controversy.html");
        HtmlExporter.MatchResultToHtml(match!, path);
        string html = File.ReadAllText(path);
        File.Delete(path);

        Assert.Contains("Errors", html);
        Assert.Contains("Refereeing controversy", html);
        Assert.Contains("VAR", html);
    }
}
