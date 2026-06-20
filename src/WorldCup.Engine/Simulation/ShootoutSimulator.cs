using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>Result of a penalty shootout, including the kick-by-kick log.</summary>
public readonly record struct ShootoutResult(int HomeScored, int AwayScored, bool HomeWon, int Rounds, IReadOnlyList<ShootoutKick> Kicks);

/// <summary>
/// Simulates a penalty shootout: best-of-five, then sudden death. Per-kick conversion is the global
/// base nudged by the strength gap (lightly, per <see cref="GlobalParameters.ShootoutStrengthWeight"/>).
/// The order of kicking is decided by a fair coin toss (as in a real shootout) so the bracket's
/// nominal "home" side gets no systematic first-mover advantage.
/// </summary>
public static class ShootoutSimulator
{
    public static ShootoutResult Simulate(
        ref Xoshiro256 rng, double homeStrength, double awayStrength, GlobalParameters g,
        string homeCode = "", IReadOnlyList<string>? homeTakers = null,
        string awayCode = "", IReadOnlyList<string>? awayTakers = null)
    {
        var home2 = homeTakers ?? Array.Empty<string>();
        var away2 = awayTakers ?? Array.Empty<string>();

        double diff = (homeStrength - awayStrength) / 100.0;
        double homeConv = Math.Clamp(g.ShootoutKickConversion + g.ShootoutStrengthWeight * diff, 0.45, 0.97);
        double awayConv = Math.Clamp(g.ShootoutKickConversion - g.ShootoutStrengthWeight * diff, 0.45, 0.97);

        // Coin toss for who kicks first; run the alternating sequence in first/second terms.
        bool homeFirst = rng.NextDouble() < 0.5;
        double firstConv = homeFirst ? homeConv : awayConv;
        double secondConv = homeFirst ? awayConv : homeConv;
        bool firstIsHome = homeFirst;

        int first = 0, second = 0, rounds = 0;
        bool? firstWon = null;

        // Record each kick in order, cycling through each side's designated takers for names.
        var kicks = new List<ShootoutKick>();
        int homeIdx = 0, awayIdx = 0;
        string Taker(bool isHome)
        {
            var takers = isHome ? home2 : away2;
            if (takers.Count == 0)
            {
                return "—";
            }

            int i = isHome ? homeIdx++ : awayIdx++;
            return takers[i % takers.Count];
        }

        void Record(bool isHome, bool scored) =>
            kicks.Add(new ShootoutKick(kicks.Count + 1, isHome, isHome ? homeCode : awayCode, Taker(isHome), scored));

        // Best of five: stop early once the result is mathematically decided.
        for (int kick = 1; kick <= 5 && firstWon is null; kick++)
        {
            rounds = kick;
            bool s1 = Distributions.Chance(ref rng, firstConv);
            Record(firstIsHome, s1);
            if (s1)
            {
                first++;
            }

            // First kicker has taken `kick`; second has taken `kick - 1`, so second still has (6 - kick) left.
            if (first > second + (6 - kick))
            {
                firstWon = true;
                break;
            }

            bool s2 = Distributions.Chance(ref rng, secondConv);
            Record(!firstIsHome, s2);
            if (s2)
            {
                second++;
            }

            // Both have now taken `kick`, so first has (5 - kick) left.
            if (second > first + (5 - kick))
            {
                firstWon = false;
                break;
            }
        }

        // Sudden death (both take a kick each round) if still level after five.
        while (firstWon is null && first == second)
        {
            rounds++;
            bool firstScored = Distributions.Chance(ref rng, firstConv);
            Record(firstIsHome, firstScored);
            bool secondScored = Distributions.Chance(ref rng, secondConv);
            Record(!firstIsHome, secondScored);
            if (firstScored)
            {
                first++;
            }

            if (secondScored)
            {
                second++;
            }
        }

        firstWon ??= first > second;

        int home = homeFirst ? first : second;
        int away = homeFirst ? second : first;
        bool homeWon = homeFirst ? firstWon.Value : !firstWon.Value;
        return new ShootoutResult(home, away, homeWon, rounds, kicks);
    }
}
