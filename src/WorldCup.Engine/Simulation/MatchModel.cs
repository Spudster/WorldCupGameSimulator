using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// The shared Poisson goals model used by both fidelity levels. Expected goals for each side are
/// derived from the strength gap (log-linear), home advantage, and the global goal baseline.
/// Scorelines are drawn from a bivariate Poisson so the realised draw rate can be tuned.
/// </summary>
public static class MatchModel
{
    /// <summary>
    /// Compute expected goals (lambda) for the home and away sides.
    /// </summary>
    /// <param name="homeStrength">Home team effective strength (0–100).</param>
    /// <param name="awayStrength">Away team effective strength (0–100).</param>
    /// <param name="g">Global parameters.</param>
    /// <param name="neutralVenue">True to suppress home advantage (knockout/neutral matches).</param>
    public static (double Home, double Away) ExpectedGoals(
        double homeStrength, double awayStrength, GlobalParameters g, bool neutralVenue)
    {
        double diff = (homeStrength - awayStrength) / 100.0;
        double homeAdv = neutralVenue ? 0.0 : g.HomeAdvantage;
        double lambdaHome = g.GoalBaseline * Math.Exp(g.StrengthSensitivity * diff + homeAdv);
        double lambdaAway = g.GoalBaseline * Math.Exp(g.StrengthSensitivity * -diff);
        return (lambdaHome, lambdaAway);
    }

    /// <summary>
    /// A per-match "form / any-given-day" multiplier for a team's expected goals. Log-normal and
    /// mean-centred (so E[factor] = 1, preserving long-run averages); larger <paramref name="sigma"/>
    /// widens the spread of results, which is what produces upsets and shock scorelines.
    /// </summary>
    public static double FormFactor(ref Xoshiro256 rng, double sigma)
    {
        if (sigma <= 0)
        {
            return 1.0;
        }

        return Math.Exp(rng.NextGaussian() * sigma - sigma * sigma * 0.5);
    }

    /// <summary>
    /// Draw a scoreline from the bivariate Poisson defined by the two lambdas and the global
    /// <see cref="GlobalParameters.DrawCoupling"/> (a shared component that raises the draw rate).
    /// </summary>
    public static (int Home, int Away) SampleScoreline(
        ref Xoshiro256 rng, double lambdaHome, double lambdaAway, double drawCoupling)
    {
        double shared = drawCoupling * Math.Min(lambdaHome, lambdaAway);
        int common = Distributions.SamplePoisson(ref rng, shared);
        int home = Distributions.SamplePoisson(ref rng, Math.Max(0, lambdaHome - shared)) + common;
        int away = Distributions.SamplePoisson(ref rng, Math.Max(0, lambdaAway - shared)) + common;
        return (home, away);
    }

    /// <summary>
    /// The analytic exact-score probability grid for the bivariate Poisson defined by the two lambdas
    /// and draw coupling (the same model <see cref="SampleScoreline"/> draws from). Used for pre-match
    /// odds and the "miracle" rating without running a Monte Carlo. grid[h, a] = P(home h, away a).
    /// </summary>
    public static double[,] ScoreGrid(double lambdaHome, double lambdaAway, double drawCoupling, int maxGoals = 10)
    {
        double shared = drawCoupling * Math.Min(lambdaHome, lambdaAway);
        double[] ph = PoissonPmf(Math.Max(0, lambdaHome - shared), maxGoals);
        double[] pa = PoissonPmf(Math.Max(0, lambdaAway - shared), maxGoals);
        double[] pc = PoissonPmf(shared, maxGoals);

        var grid = new double[maxGoals + 1, maxGoals + 1];
        for (int h = 0; h <= maxGoals; h++)
        {
            for (int a = 0; a <= maxGoals; a++)
            {
                double p = 0;
                int cmax = Math.Min(h, a);
                for (int c = 0; c <= cmax; c++)
                {
                    p += pc[c] * ph[h - c] * pa[a - c];
                }

                grid[h, a] = p;
            }
        }

        return grid;
    }

    // 5-node Gauss–Hermite quadrature for E[f(Z)], Z~N(0,1): nodes z_i and weights w_i with Σw_i = 1.
    private static readonly double[] GhZ = { -2.8569700138728056, -1.3556261799742659, 0.0, 1.3556261799742659, 2.8569700138728056 };
    private static readonly double[] GhW = { 0.011257411327720693, 0.2220759220056126, 0.5333333333333333, 0.2220759220056126, 0.011257411327720693 };

    /// <summary>
    /// The exact-score grid for the bivariate Poisson <em>marginalised over the per-match form factor</em>
    /// (the log-normal "any given day" multiplier with the given <paramref name="sigma"/>). This is the
    /// overdispersed distribution the Monte-Carlo sampler actually draws from, so pre-match odds and the
    /// miracle rating computed from it match the simulation. With sigma ≤ 0 it equals <see cref="ScoreGrid"/>.
    /// </summary>
    public static double[,] ScoreGridWithForm(double lambdaHome, double lambdaAway, double drawCoupling, double sigma, int maxGoals = 10)
    {
        if (sigma <= 0)
        {
            return ScoreGrid(lambdaHome, lambdaAway, drawCoupling, maxGoals);
        }

        double half = sigma * sigma * 0.5;
        var grid = new double[maxGoals + 1, maxGoals + 1];
        for (int i = 0; i < GhZ.Length; i++)
        {
            double fh = Math.Exp(sigma * GhZ[i] - half);
            for (int j = 0; j < GhZ.Length; j++)
            {
                double fa = Math.Exp(sigma * GhZ[j] - half);
                double w = GhW[i] * GhW[j];
                var part = ScoreGrid(lambdaHome * fh, lambdaAway * fa, drawCoupling, maxGoals);
                for (int h = 0; h <= maxGoals; h++)
                {
                    for (int a = 0; a <= maxGoals; a++)
                    {
                        grid[h, a] += w * part[h, a];
                    }
                }
            }
        }

        return grid;
    }

    /// <summary>Win/draw/loss probabilities (normalised) summed from a <see cref="ScoreGrid"/>.</summary>
    public static (double HomeWin, double Draw, double AwayWin) OutcomeProbabilities(double[,] grid)
    {
        double hw = 0, dr = 0, aw = 0, total = 0;
        int n = grid.GetLength(0);
        for (int h = 0; h < n; h++)
        {
            for (int a = 0; a < n; a++)
            {
                double p = grid[h, a];
                total += p;
                if (h > a) hw += p;
                else if (h < a) aw += p;
                else dr += p;
            }
        }

        return total > 0 ? (hw / total, dr / total, aw / total) : (0, 0, 0);
    }

    /// <summary>Probability of an exact scoreline from a <see cref="ScoreGrid"/> (clamped to the grid).</summary>
    public static double ScoreProbability(double[,] grid, int home, int away)
    {
        int n = grid.GetLength(0);
        return grid[Math.Clamp(home, 0, n - 1), Math.Clamp(away, 0, n - 1)];
    }

    /// <summary>
    /// A 1–10 "miracle / upset" rating for an actual result. Driven mainly by how unlikely the
    /// <em>outcome</em> (win/draw/loss) was pre-match — ~1 for the expected result, climbing for upsets
    /// — with a finer gradation for how rare the exact <em>scoreline</em> was (so an emphatic
    /// thrashing or a wild scoreline reads a little higher than the bare minimum for that outcome).
    /// </summary>
    public static double MiracleRating(double outcomeProbability, double scoreProbability)
    {
        double outcomeBits = -Math.Log2(Math.Clamp(outcomeProbability, 1e-6, 1.0));
        double scoreBits = -Math.Log2(Math.Clamp(scoreProbability, 1e-6, 1.0));
        // Outcome surprise dominates; a rare exact scoreline (scoreBits > ~4 ⇒ < 6%) adds up to ~+2.
        return Math.Clamp(0.5 + (outcomeBits - 1.0) * 2.0 + Math.Max(0, scoreBits - 3.5) * 0.4, 1.0, 10.0);
    }

    /// <summary>The outcome of a "miracle" roll — the rare event where an underdog catches fire.</summary>
    public readonly record struct MiracleOutcome(double HomeStrength, double AwayStrength, bool Fired, bool ForHome);

    /// <summary>
    /// Rolls for a "miracle": a rare, gap-scaled chance that the underdog plays out of their skin for the
    /// day, clawing back most of the strength gap (so a genuine upset becomes likely but not certain).
    /// Returns the (possibly) adjusted strengths to feed the goals model — used by BOTH the fast and
    /// detailed simulators so upsets propagate through every projection. Advances the RNG by one draw
    /// only when there is a clear underdog.
    /// </summary>
    public static MiracleOutcome RollMiracle(double homeStrength, double awayStrength, GlobalParameters g, ref Xoshiro256 rng)
    {
        double gap = Math.Abs(homeStrength - awayStrength);
        if (gap < g.MiracleMinGap)
        {
            return new MiracleOutcome(homeStrength, awayStrength, false, false);
        }

        double p = Math.Min(g.MiracleMaxChance, g.MiracleBaseChance + g.MiracleGapChance * (gap - g.MiracleMinGap));
        if (rng.NextDouble() >= p)
        {
            return new MiracleOutcome(homeStrength, awayStrength, false, false);
        }

        bool forHome = homeStrength < awayStrength;
        double swing = g.MiracleStrengthSwing * gap;
        double h = homeStrength, a = awayStrength;
        if (forHome)
        {
            h += swing * 0.6;
            a -= swing * 0.4;
        }
        else
        {
            a += swing * 0.6;
            h -= swing * 0.4;
        }

        return new MiracleOutcome(h, a, true, forHome);
    }

    /// <summary>
    /// In-play win/draw/loss probabilities: the current score plus the goals expected in the time
    /// remaining (expected goals scale down linearly with the minutes left). As the clock runs out the
    /// forecast converges on the current scoreline. Score and time only — no in-play red cards/momentum.
    /// </summary>
    public static (double HomeWin, double Draw, double AwayWin) InPlayOutcome(
        double lambdaHome, double lambdaAway, double drawCoupling,
        int homeSoFar, int awaySoFar, int minuteElapsed, int totalMinutes = 90)
    {
        double remaining = Math.Clamp((totalMinutes - minuteElapsed) / (double)totalMinutes, 0.0, 1.0);
        var grid = ScoreGrid(lambdaHome * remaining, lambdaAway * remaining, drawCoupling);

        double hw = 0, dr = 0, aw = 0, total = 0;
        int n = grid.GetLength(0);
        for (int rh = 0; rh < n; rh++)
        {
            for (int ra = 0; ra < n; ra++)
            {
                double pr = grid[rh, ra];
                total += pr;
                int fh = homeSoFar + rh, fa = awaySoFar + ra;
                if (fh > fa) hw += pr;
                else if (fh < fa) aw += pr;
                else dr += pr;
            }
        }

        return total > 0 ? (hw / total, dr / total, aw / total) : (0, 0, 0);
    }

    private static double[] PoissonPmf(double lambda, int maxK)
    {
        var pmf = new double[maxK + 1];
        double term = Math.Exp(-lambda);
        pmf[0] = term;
        for (int k = 1; k <= maxK; k++)
        {
            term *= lambda / k;
            pmf[k] = term;
        }

        return pmf;
    }
}
