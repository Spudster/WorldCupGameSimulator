using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Event-level match simulator. Runs a minute-by-minute model that produces player-attributed
/// goals (with minute, shot distance and type), cards (incl. second-yellow reds that reduce the
/// team's effective strength), in-play penalties, injuries, and a full box score. Used for single
/// matches and smaller-N detailed tournament runs.
/// </summary>
public static class DetailedMatchSimulator
{
    private const int RegulationMinutes = 90;
    private const int ExtraTimeMinutes = 30;
    internal const int MaxSubs = 5;

    public static MatchResult Simulate(
        Team home, Team away, Stage stage, SimulationParameters p, ref Xoshiro256 rng, bool neutralVenue)
    {
        var g = p.Global;
        bool knockout = stage != Stage.Group;
        var state = new MatchState(home, away, p, neutralVenue, ref rng);

        // Regulation.
        SimulatePeriod(ref rng, state, p, periodStart: 1, coreMinutes: RegulationMinutes, scale: 1.0);

        // Referee's added time: the realised second-half board comes from how far play actually ran;
        // the first-half board is a short 1–4 minutes signalled by the fourth official.
        state.FirstHalfStoppage = 1 + rng.NextInt(4);
        state.SecondHalfStoppage = Math.Max(1, state.LastMinute - RegulationMinutes);

        var method = MatchMethod.Regulation;
        ShootoutResult? shootout = null;

        if (knockout && state.HomeGoals == state.AwayGoals)
        {
            method = MatchMethod.ExtraTime;
            SimulatePeriod(ref rng, state, p, periodStart: RegulationMinutes + 1,
                coreMinutes: ExtraTimeMinutes, scale: g.ExtraTimeGoalScale);

            if (state.HomeGoals == state.AwayGoals)
            {
                method = MatchMethod.Penalties;
                shootout = ShootoutSimulator.Simulate(ref rng, state.HomeStrength, state.AwayStrength, g,
                    home.Code, state.PenaltyTakers(isHome: true),
                    away.Code, state.PenaltyTakers(isHome: false));
            }
        }

        // Match-day conditions and atmosphere (drawn after play, so the calibrated event stream is untouched).
        // FIFA MANDATES cooling / hydration breaks (≈30th minute of each half) when the heat is high — and
        // with June–July kick-offs across Dallas, Houston, Monterrey, Guadalajara, Miami, Atlanta and KC,
        // a large share of 2026 fixtures hit it. ~1/3 (cooler venues / evening kick-offs — Vancouver,
        // Seattle, the Bay, NY/NJ) stay mild; the rest are hot day games where the breaks are mandatory.
        int temperatureC = rng.NextDouble() < 0.35
            ? 19 + rng.NextInt(11)   // 19–29 °C — mild, no mandated break
            : 30 + rng.NextInt(10);  // 30–39 °C — hot; cooling breaks are mandatory
        state.GenerateAtmosphere(ref rng, temperatureC);
        return state.Build(stage, method, shootout, ref rng);
    }

    private static void SimulatePeriod(
        ref Xoshiro256 rng, MatchState s, SimulationParameters p, int periodStart, int coreMinutes, double scale)
    {
        var g = p.Global;
        var ev = g.Events;

        // Stoppage time is part of the period, so normalise per-minute hazards by the *actual*
        // length (core + stoppage). This keeps realised totals on the per-match targets instead of
        // inflating them by the extra minutes.
        int stoppage = 1 + rng.NextInt(coreMinutes >= RegulationMinutes ? 6 : 4);
        int end = periodStart + coreMinutes - 1 + stoppage;
        int length = coreMinutes + stoppage;

        double Per(double perTeamMatchTotal) => perTeamMatchTotal * scale / length;

        double yellowPerTeam = ev.YellowCardsPerMatch / 2.0;
        double directRedPerTeam = ev.DirectRedCardsPerMatch / 2.0;
        double penPerTeam = ev.PenaltiesPerMatch / 2.0;
        double injPerTeam = ev.InjuriesPerMatch / 2.0;

        for (int minute = periodStart; minute <= end; minute++)
        {
            // Goals and cards rise through the match (fatigue, urgency, tactical fouls). The multiplier
            // is mean-1 over the period (0.6 early → 1.4 late), so realised totals stay on target.
            double surge = 1.0 + 0.8 * ((minute - periodStart) / (double)Math.Max(1, length - 1) - 0.5);

            // Momentum decays a little each minute, then this minute's hazards ride the current swing.
            s.DecayMomentum();

            StepTeam(ref rng, s, p, minute, isHome: true,
                goalP: s.HomeOpenPlayLambda * scale / length * surge * s.MomentumFactor(isHome: true),
                yellowP: Per(yellowPerTeam) * s.HomeIndiscipline * surge * s.LateFrustrationMultiplier(isHome: true, minute),
                redP: Per(directRedPerTeam) * s.HomeIndiscipline * surge,
                penP: Per(penPerTeam) * s.HomeAttackShare * 2,
                injP: Per(injPerTeam),
                errP: Per(ev.UnpunishedErrorsPerMatch));

            StepTeam(ref rng, s, p, minute, isHome: false,
                goalP: s.AwayOpenPlayLambda * scale / length * surge * s.MomentumFactor(isHome: false),
                yellowP: Per(yellowPerTeam) * s.AwayIndiscipline * surge * s.LateFrustrationMultiplier(isHome: false, minute),
                redP: Per(directRedPerTeam) * s.AwayIndiscipline * surge,
                penP: Per(penPerTeam) * s.AwayAttackShare * 2,
                injP: Per(injPerTeam),
                errP: Per(ev.UnpunishedErrorsPerMatch));

            // Refereeing mistakes are a per-match hazard (not per team): a denied penalty, a missed
            // red, a goal wrongly chalked off. None of them change the score.
            if (Distributions.Chance(ref rng, Per(ev.RefereeMistakesPerMatch)))
            {
                s.CommitRefereeMistake(ref rng, minute);
            }

            // Tactical substitutions: managers refresh the side through the second half (ramps to the
            // final whistle). Capped at 5 subs/side (shared with injuries), so it self-limits.
            if (coreMinutes >= RegulationMinutes && minute >= 55)
            {
                double subP = 0.15 * Math.Min(1.0, (minute - 55) / 35.0);
                if (Distributions.Chance(ref rng, subP)) s.TacticalSub(ref rng, isHome: true, minute);
                if (Distributions.Chance(ref rng, subP)) s.TacticalSub(ref rng, isHome: false, minute);
            }
        }

        s.LastMinute = end;
    }

    private static void StepTeam(
        ref Xoshiro256 rng, MatchState s, SimulationParameters p, int minute, bool isHome,
        double goalP, double yellowP, double redP, double penP, double injP, double errP)
    {
        var g = p.Global;

        // Penalty (resolved immediately; a converted penalty is also a goal).
        if (Distributions.Chance(ref rng, penP))
        {
            s.AwardPenalty(ref rng, isHome, minute);
        }

        // Open-play goal.
        if (Distributions.Chance(ref rng, goalP))
        {
            s.ScoreOpenPlay(ref rng, isHome, minute);
        }

        // A mistake that didn't cost a goal (got away with it).
        if (Distributions.Chance(ref rng, errP))
        {
            s.CommitUnpunishedError(ref rng, isHome, minute);
        }

        // Cards.
        if (Distributions.Chance(ref rng, redP))
        {
            s.ShowCard(ref rng, isHome, minute, forceRed: true);
        }
        else if (Distributions.Chance(ref rng, yellowP))
        {
            s.ShowCard(ref rng, isHome, minute, forceRed: false);
        }

        // Injury.
        if (Distributions.Chance(ref rng, injP))
        {
            s.Injure(ref rng, isHome, minute);
        }
    }
}
