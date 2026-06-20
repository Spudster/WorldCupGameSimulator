namespace WorldCup.Engine.Random;

/// <summary>
/// A fast, high-quality pseudo-random generator (xoshiro256**) suitable for the Monte Carlo
/// hot path. It is a mutable struct holding 256 bits of state — give <em>each worker thread its
/// own instance</em> (never share one across threads). Seeding is done through SplitMix64 so a
/// single 64-bit seed expands to a well-distributed full state.
/// </summary>
public struct Xoshiro256
{
    private ulong _s0, _s1, _s2, _s3;

    public Xoshiro256(ulong seed)
    {
        // Expand the seed with SplitMix64 (recommended by the xoshiro authors).
        ulong sm = seed;
        _s0 = SplitMix64(ref sm);
        _s1 = SplitMix64(ref sm);
        _s2 = SplitMix64(ref sm);
        _s3 = SplitMix64(ref sm);

        // Guard against the all-zero state.
        if ((_s0 | _s1 | _s2 | _s3) == 0)
        {
            _s0 = 0x9E3779B97F4A7C15;
        }
    }

    private static ulong SplitMix64(ref ulong x)
    {
        x += 0x9E3779B97F4A7C15;
        ulong z = x;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }

    private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

    /// <summary>Next raw 64-bit value.</summary>
    public ulong NextUInt64()
    {
        ulong result = Rotl(_s1 * 5, 7) * 9;
        ulong t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = Rotl(_s3, 45);

        return result;
    }

    /// <summary>Uniform double in [0, 1).</summary>
    public double NextDouble()
    {
        // Use the top 53 bits for a uniform double in [0,1).
        return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
    }

    /// <summary>Uniform double in [0, max).</summary>
    public double NextDouble(double max) => NextDouble() * max;

    /// <summary>Uniform int in [0, exclusiveMax). Requires exclusiveMax &gt; 0.</summary>
    public int NextInt(int exclusiveMax)
    {
        // Lemire-style bounded reduction (slightly biased but negligible for our ranges).
        return (int)((NextUInt64() >> 33) % (uint)exclusiveMax);
    }

    /// <summary>Uniform int in [min, exclusiveMax).</summary>
    public int NextInt(int min, int exclusiveMax) => min + NextInt(exclusiveMax - min);

    /// <summary>Standard-normal sample via the Box–Muller transform.</summary>
    public double NextGaussian()
    {
        double u1 = 1.0 - NextDouble();
        double u2 = NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
