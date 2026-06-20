using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// The low-allocation, strength-only match simulator that powers the millions-of-runs fast mode.
/// It works purely on numeric strengths and a per-thread <see cref="Xoshiro256"/>, with no object
/// allocation per match.
/// </summary>
public static class FastMatchSimulator
{
    /// <summary>Simulate a regulation-only match (group stage). Draws are possible.</summary>
    public static FastMatchResult SimulateRegulation(
        ref Xoshiro256 rng, double homeStrength, double awayStrength, GlobalParameters g, bool neutralVenue)
    {
        var mir = MatchModel.RollMiracle(homeStrength, awayStrength, g, ref rng); // rare underdog "day of destiny"
        var (lh, la) = MatchModel.ExpectedGoals(mir.HomeStrength, mir.AwayStrength, g, neutralVenue);
        double tempo = MatchModel.FormFactor(ref rng, g.MatchTempoVariance); // shared open/cagey game tempo
        lh *= MatchModel.FormFactor(ref rng, g.UpsetVariance) * tempo;
        la *= MatchModel.FormFactor(ref rng, g.UpsetVariance) * tempo;
        var (h, a) = MatchModel.SampleScoreline(ref rng, lh, la, g.DrawCoupling);
        bool? winner = h > a ? true : a > h ? false : null;
        return new FastMatchResult(h, a, MatchMethod.Regulation, winner, 0, 0);
    }

    /// <summary>
    /// Simulate a knockout match to a definite winner: regulation, then extra time, then a
    /// shootout if still level. Played at a neutral venue.
    /// </summary>
    public static FastMatchResult SimulateKnockout(
        ref Xoshiro256 rng, double homeStrength, double awayStrength, GlobalParameters g)
    {
        var mir = MatchModel.RollMiracle(homeStrength, awayStrength, g, ref rng); // rare underdog "day of destiny"
        var (lh, la) = MatchModel.ExpectedGoals(mir.HomeStrength, mir.AwayStrength, g, neutralVenue: true);
        // One per-match form draw for each team plus a shared tempo, used for regulation and extra time.
        double tempo = MatchModel.FormFactor(ref rng, g.MatchTempoVariance);
        lh *= MatchModel.FormFactor(ref rng, g.UpsetVariance) * tempo;
        la *= MatchModel.FormFactor(ref rng, g.UpsetVariance) * tempo;
        var (h, a) = MatchModel.SampleScoreline(ref rng, lh, la, g.DrawCoupling);
        if (h != a)
        {
            return new FastMatchResult(h, a, MatchMethod.Regulation, h > a, 0, 0);
        }

        // Extra time: 30 minutes at a reduced scoring rate.
        var (eh, ea) = MatchModel.SampleScoreline(
            ref rng, lh * g.ExtraTimeGoalScale, la * g.ExtraTimeGoalScale, g.DrawCoupling);
        h += eh;
        a += ea;
        if (h != a)
        {
            return new FastMatchResult(h, a, MatchMethod.ExtraTime, h > a, 0, 0);
        }

        // Penalty shootout — the inspired underdog carries their lift into it too.
        var so = ShootoutSimulator.Simulate(ref rng, mir.HomeStrength, mir.AwayStrength, g);
        return new FastMatchResult(h, a, MatchMethod.Penalties, so.HomeWon, so.HomeScored, so.AwayScored);
    }
}
