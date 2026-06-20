using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Public entry point for simulating a single match between two <see cref="Team"/>s at either
/// fidelity, returning a fully-populated <see cref="MatchResult"/> (with team names).
/// </summary>
public static class MatchSimulator
{
    public static MatchResult Simulate(
        Team home, Team away, Stage stage, Fidelity fidelity, SimulationParameters p,
        ref Xoshiro256 rng, bool neutralVenue)
    {
        if (fidelity == Fidelity.Detailed)
        {
            return DetailedMatchSimulator.Simulate(home, away, stage, p, ref rng, neutralVenue);
        }

        double hs = p.EffectiveStrength(home);
        double as_ = p.EffectiveStrength(away);
        FastMatchResult fr = stage == Stage.Group
            ? FastMatchSimulator.SimulateRegulation(ref rng, hs, as_, p.Global, neutralVenue)
            : FastMatchSimulator.SimulateKnockout(ref rng, hs, as_, p.Global);

        string winner = fr.WinnerIsHome is null
            ? string.Empty
            : fr.WinnerIsHome.Value ? home.Code : away.Code;

        return new MatchResult
        {
            Fidelity = Fidelity.Fast,
            Stage = stage,
            HomeCode = home.Code,
            AwayCode = away.Code,
            HomeName = home.Name,
            AwayName = away.Name,
            HomeGoals = fr.HomeGoals,
            AwayGoals = fr.AwayGoals,
            Method = fr.Method,
            WinnerCode = winner,
            HomePens = fr.HomePens,
            AwayPens = fr.AwayPens,
        };
    }
}
