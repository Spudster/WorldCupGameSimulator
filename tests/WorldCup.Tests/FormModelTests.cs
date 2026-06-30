using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

/// <summary>
/// Recent form carries an already-played result into a team's upcoming forecasts: an underdog that
/// over-performs its rating (Cape Verde holding Spain to a 0–0) is nudged up for its next game, the
/// favourite that under-performs is nudged down, and the whole mechanism stays calibration-safe because
/// it is empty unless results are supplied.
/// </summary>
public class FormModelTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    private static IReadOnlyList<PlayedResult> SpainHeldByCapeVerde =>
        new[] { new PlayedResult("ESP", "CPV", 0, 0) };

    [Fact]
    public void Overperforming_Underdog_Gets_A_Positive_Delta_And_The_Favourite_A_Negative_One()
    {
        var p = SimulationParameters.CreateStarting();
        var adjustments = FormModel.Compute(Data, SpainHeldByCapeVerde, p);

        var cpv = adjustments.Single(a => a.TeamCode == "CPV");
        var esp = adjustments.Single(a => a.TeamCode == "ESP");

        Assert.True(cpv.Delta > 0, $"Cape Verde should be boosted, was {cpv.Delta:0.00}");
        Assert.True(esp.Delta < 0, $"Spain should be dinged, was {esp.Delta:0.00}");

        // Symmetric in sign and bounded by the cap.
        double cap = p.Global.FormMaxDelta;
        Assert.True(Math.Abs(cpv.Delta) <= cap + 1e-9);
        Assert.True(Math.Abs(esp.Delta) <= cap + 1e-9);

        // The explanation names the opponent and reads the right direction.
        Assert.Contains("Cape Verde", cpv.TeamName);
        Assert.Contains("Spain", cpv.Summary);
        Assert.Contains("above", cpv.Summary);
        Assert.Equal(1, cpv.GamesUsed);
    }

    [Fact]
    public void No_Results_Or_Disabled_Form_Yields_No_Adjustments()
    {
        var p = SimulationParameters.CreateStarting();
        Assert.Empty(FormModel.Compute(Data, Array.Empty<PlayedResult>(), p));

        p.Global.FormWeight = 0.0;
        Assert.Empty(FormModel.Compute(Data, SpainHeldByCapeVerde, p));
    }

    [Fact]
    public void Form_Is_Calibration_Safe_Until_Deltas_Are_Applied()
    {
        // A pristine parameter set has no form deltas, so effective strength is exactly the base rating —
        // pre-tournament odds and the calibration are untouched.
        var p = SimulationParameters.CreateStarting();
        var cpv = Data.Team("CPV");
        Assert.Equal(cpv.Strength, p.EffectiveStrength(cpv), 6);
    }

    [Fact]
    public void Applying_The_Delta_Raises_Strength_And_Improves_The_Next_Forecast()
    {
        var data = Data;
        var p = SimulationParameters.CreateStarting();
        var adjustments = FormModel.Compute(data, SpainHeldByCapeVerde, p);

        var cpv = data.Team("CPV");
        var uru = data.Team("URU");

        // Baseline forecast (no form).
        var before = MonteCarloMatchRunner.RunFast(cpv, uru, p, 200_000, neutralVenue: true);

        // Apply the computed form deltas to a clone and re-forecast.
        var withForm = p.Clone();
        withForm.TeamFormDeltas = adjustments.ToDictionary(a => a.TeamCode, a => a.Delta, StringComparer.OrdinalIgnoreCase);

        Assert.True(withForm.EffectiveStrength(cpv) > p.EffectiveStrength(cpv));
        var after = MonteCarloMatchRunner.RunFast(cpv, uru, withForm, 200_000, neutralVenue: true);

        // Cape Verde — boosted by their result against Spain — are now a likelier non-loss.
        Assert.True(after.HomeWin + after.Draw > before.HomeWin + before.Draw,
            $"non-loss should improve: before {before.HomeWin + before.Draw:0.000}, after {after.HomeWin + after.Draw:0.000}");
    }
}
