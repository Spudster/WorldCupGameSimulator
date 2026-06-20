using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Calibration;

/// <summary>One measured calibration metric against its real-world target.</summary>
public sealed record CalibrationMetric(string Name, double Measured, double Target, double Tolerance)
{
    public double Delta => Measured - Target;
    public bool InBand => Math.Abs(Delta) <= Tolerance;
}

/// <summary>The result of a calibration measurement (and any auto-tuning).</summary>
public sealed record CalibrationReport(
    int Matches, IReadOnlyList<CalibrationMetric> Metrics, bool AutoTuned, int TuningIterations);

/// <summary>
/// Diagnostic that runs a large detailed-mode batch of representative matchups, measures the
/// realised per-match averages, and compares them to recent World Cup targets. Can auto-tune the
/// global rate knobs (goal scaling, card/penalty/corner rates) until each metric lands in its band.
/// </summary>
public static class CalibrationRunner
{
    // Real-world targets (see README for sources).
    public const double TargetGoals = 2.7;
    public const double TargetYellows = 3.3;
    public const double TargetReds = 0.1;
    public const double TargetPenalties = 0.4;
    public const double TargetCorners = 10.0;
    public const double TargetDraws = 0.22; // over random pairings (mismatches lower the rate vs ~0.25 in close games)

    public static CalibrationReport Measure(TournamentData data, SimulationParameters p, int matches)
    {
        var teams = data.Teams;
        var rng = new Xoshiro256(p.Global.Seed ^ 0xA24BAED4963EE407UL);

        long goals = 0, yellows = 0, reds = 0, pens = 0, corners = 0, draws = 0;
        for (int i = 0; i < matches; i++)
        {
            int ai = rng.NextInt(teams.Count);
            int bi = rng.NextInt(teams.Count);
            if (bi == ai)
            {
                bi = (bi + 1) % teams.Count;
            }

            var r = DetailedMatchSimulator.Simulate(teams[ai], teams[bi], Stage.Group, p, ref rng, neutralVenue: true);
            goals += r.HomeGoals + r.AwayGoals;
            if (r.HomeGoals == r.AwayGoals)
            {
                draws++;
            }

            foreach (var c in r.Cards)
            {
                if (c.IsRed) reds++; else yellows++;
            }

            pens += r.Penalties.Count;
            corners += (r.HomeBox?.Corners ?? 0) + (r.AwayBox?.Corners ?? 0);
        }

        double inv = 1.0 / Math.Max(1, matches);
        var metrics = new List<CalibrationMetric>
        {
            new("Goals / match", goals * inv, TargetGoals, 0.15),
            new("Yellow cards / match", yellows * inv, TargetYellows, 0.30),
            new("Red cards / match", reds * inv, TargetReds, 0.06),
            new("Penalties / match", pens * inv, TargetPenalties, 0.10),
            new("Corners / match", corners * inv, TargetCorners, 1.0),
            // Informational only (not auto-tuned): random pairings include big mismatches, so this runs
            // below the ~25% seen in competitive group games.
            new("Draws / match", draws * inv, TargetDraws, 0.07),
        };

        return new CalibrationReport(matches, metrics, AutoTuned: false, TuningIterations: 0);
    }

    /// <summary>
    /// Auto-tune the global rate knobs so the measured averages land in their target bands. Returns
    /// a tuned copy of the parameters plus before/after reports. Adjusts one knob per metric, each
    /// iteration proportionally toward its target.
    /// </summary>
    public static (SimulationParameters Tuned, CalibrationReport Before, CalibrationReport After) AutoTune(
        TournamentData data, SimulationParameters parameters, int matchesPerIteration = 4000, int maxIterations = 12)
    {
        var before = Measure(data, parameters, matchesPerIteration);
        var tuned = parameters.Clone();
        tuned.Label = parameters.Label + " (calibrated)";

        int iterations = 0;
        CalibrationReport current = before;
        for (; iterations < maxIterations; iterations++)
        {
            // The draw rate is informational (no dedicated knob), so it doesn't block convergence.
            if (current.Metrics.Where(m => m.Name != "Draws / match").All(m => m.InBand))
            {
                break;
            }

            var ev = tuned.Global.Events;
            foreach (var metric in current.Metrics)
            {
                if (metric.InBand || metric.Measured <= 0)
                {
                    continue;
                }

                double factor = Math.Clamp(metric.Target / metric.Measured, 0.5, 2.0);
                switch (metric.Name)
                {
                    case "Goals / match":
                        tuned.Global.GoalBaseline = Clamp(tuned.Global.GoalBaseline * factor, 0.5, 3.0);
                        break;
                    case "Yellow cards / match":
                        ev.YellowCardsPerMatch = Clamp(ev.YellowCardsPerMatch * factor, 0.5, 8.0);
                        break;
                    case "Red cards / match":
                        ev.DirectRedCardsPerMatch = Clamp(ev.DirectRedCardsPerMatch * factor, 0.005, 0.5);
                        break;
                    case "Penalties / match":
                        ev.PenaltiesPerMatch = Clamp(ev.PenaltiesPerMatch * factor, 0.05, 1.5);
                        break;
                    case "Corners / match":
                        ev.CornersPerMatch = Clamp(ev.CornersPerMatch * factor, 4.0, 16.0);
                        break;
                }
            }

            current = Measure(data, tuned, matchesPerIteration);
        }

        var after = current with { AutoTuned = true, TuningIterations = iterations };
        return (tuned, before, after);
    }

    private static double Clamp(double v, double lo, double hi) => Math.Min(hi, Math.Max(lo, v));
}
