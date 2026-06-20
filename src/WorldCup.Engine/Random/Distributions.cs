namespace WorldCup.Engine.Random;

/// <summary>
/// Sampling helpers for the simulation: Poisson (goal counts), and a right-skewed
/// distribution for shot distances. All take a <see cref="Xoshiro256"/> by ref so the
/// caller's per-thread RNG state advances.
/// </summary>
public static class Distributions
{
    /// <summary>
    /// Sample a Poisson(lambda) variate. Uses Knuth's multiplicative algorithm for small lambda
    /// (the World Cup goal regime, lambda &lt; ~30) and a normal approximation for large lambda.
    /// </summary>
    public static int SamplePoisson(ref Xoshiro256 rng, double lambda)
    {
        if (lambda <= 0)
        {
            return 0;
        }

        if (lambda < 30.0)
        {
            // Knuth: multiply uniforms until the product drops below e^-lambda.
            double l = Math.Exp(-lambda);
            int k = 0;
            double p = 1.0;
            do
            {
                k++;
                p *= rng.NextDouble();
            }
            while (p > l);
            return k - 1;
        }

        // Normal approximation with continuity correction for large lambda.
        double approx = lambda + Math.Sqrt(lambda) * rng.NextGaussian();
        int rounded = (int)Math.Round(approx);
        return rounded < 0 ? 0 : rounded;
    }

    /// <summary>
    /// Sample a goal's shot distance (metres) from a right-skewed log-normal distribution so the
    /// large majority of goals come from inside the box (~6–18 m) with a thin long tail for rare
    /// 25 m+ strikes. Parameters chosen to give a median around 11 m.
    /// </summary>
    /// <param name="mu">Log-space mean (default ~ln(11)).</param>
    /// <param name="sigma">Log-space standard deviation controlling tail thickness.</param>
    public static double SampleShotDistance(ref Xoshiro256 rng, double mu = 2.40, double sigma = 0.42)
    {
        double z = rng.NextGaussian();
        double metres = Math.Exp(mu + sigma * z);
        // Keep within sane footballing bounds (own-half screamers exist but are absurd beyond ~45 m).
        return Math.Clamp(metres, 1.5, 50.0);
    }

    /// <summary>Bernoulli trial: returns true with the given probability.</summary>
    public static bool Chance(ref Xoshiro256 rng, double probability) => rng.NextDouble() < probability;

    /// <summary>
    /// Pick an index in [0, weights.Count) proportional to the (non-negative) weights.
    /// Returns 0 if all weights are zero.
    /// </summary>
    public static int SampleWeighted(ref Xoshiro256 rng, IReadOnlyList<double> weights)
    {
        double total = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            total += weights[i];
        }

        if (total <= 0)
        {
            return 0;
        }

        double r = rng.NextDouble() * total;
        double cumulative = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i];
            if (r < cumulative)
            {
                return i;
            }
        }

        return weights.Count - 1;
    }
}
