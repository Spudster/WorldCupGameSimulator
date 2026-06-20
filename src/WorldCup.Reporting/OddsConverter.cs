using System;
using System.Globalization;

namespace WorldCup.Reporting;

/// <summary>
/// Pure converter that turns a win probability (0..1) into common betting-odds formats.
/// All methods are side-effect free and defensive against divide-by-zero and NaN inputs.
/// </summary>
public static class OddsConverter
{
    /// <summary>Placeholder rendered when a probability cannot produce meaningful odds.</summary>
    private const string Empty = "—";

    /// <summary>
    /// Decimal odds for the given probability (e.g. <c>0.40 -&gt; "2.50"</c>).
    /// </summary>
    /// <param name="prob">Win probability in the range 0..1.</param>
    /// <returns>
    /// The decimal odds formatted as <c>"0.00"</c>, clamped to a minimum of <c>"1.01"</c> for
    /// very high probabilities, or <c>"—"</c> when <paramref name="prob"/> is zero, negative or NaN.
    /// </returns>
    public static string Decimal(double prob)
    {
        if (double.IsNaN(prob) || prob <= 0.0)
        {
            return Empty;
        }

        double dec = 1.0 / prob;
        if (dec < 1.01)
        {
            dec = 1.01;
        }

        return dec.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Fractional odds for the given probability (e.g. <c>0.40 -&gt; "6/4"</c>): the simplest small
    /// fraction approximating <c>(1 - prob) / prob</c> with a denominator from 1 to 20, reduced by gcd.
    /// </summary>
    /// <param name="prob">Win probability in the range 0..1.</param>
    /// <returns>
    /// The fractional odds formatted as <c>"num/den"</c>, or <c>"—"</c> when
    /// <paramref name="prob"/> is zero, negative or NaN.
    /// </returns>
    public static string Fractional(double prob)
    {
        if (double.IsNaN(prob) || prob <= 0.0)
        {
            return Empty;
        }

        double ratio = (1.0 - prob) / prob;
        if (ratio < 0.0)
        {
            ratio = 0.0;
        }

        int bestNum = 0;
        int bestDen = 1;
        double bestError = double.PositiveInfinity;

        for (int den = 1; den <= 20; den++)
        {
            int num = (int)Math.Round(ratio * den, MidpointRounding.AwayFromZero);
            double error = Math.Abs(((double)num / den) - ratio);
            if (error < bestError)
            {
                bestError = error;
                bestNum = num;
                bestDen = den;
            }
        }

        int divisor = Gcd(bestNum, bestDen);
        if (divisor > 1)
        {
            bestNum /= divisor;
            bestDen /= divisor;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1}",
            bestNum,
            bestDen);
    }

    /// <summary>
    /// American moneyline odds for the given probability: favourites are negative (e.g. <c>"-150"</c>),
    /// underdogs positive (e.g. <c>"+150"</c>).
    /// </summary>
    /// <param name="prob">Win probability in the range 0..1.</param>
    /// <returns>
    /// The signed moneyline as a string, or <c>"—"</c> when <paramref name="prob"/> is
    /// outside the open interval (0, 1) or NaN.
    /// </returns>
    public static string American(double prob)
    {
        if (double.IsNaN(prob) || prob <= 0.0 || prob >= 1.0)
        {
            return Empty;
        }

        if (prob >= 0.5)
        {
            long line = (long)Math.Round((prob / (1.0 - prob)) * 100.0, MidpointRounding.AwayFromZero);
            return string.Format(CultureInfo.InvariantCulture, "-{0}", line);
        }

        long plus = (long)Math.Round(((1.0 - prob) / prob) * 100.0, MidpointRounding.AwayFromZero);
        return string.Format(CultureInfo.InvariantCulture, "+{0}", plus);
    }

    /// <summary>
    /// Combined display of all three odds formats (e.g. <c>"2.50 · 6/4 · +150"</c>).
    /// </summary>
    /// <param name="prob">Win probability in the range 0..1.</param>
    /// <returns>The decimal, fractional and American odds joined by <c>" · "</c>.</returns>
    public static string All(double prob)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} · {1} · {2}",
            Decimal(prob),
            Fractional(prob),
            American(prob));
    }

    /// <summary>
    /// Computes the greatest common divisor of the absolute values of two integers.
    /// </summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns>The greatest common divisor, or 1 when both operands are zero.</returns>
    private static int Gcd(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a == 0 ? 1 : a;
    }
}
