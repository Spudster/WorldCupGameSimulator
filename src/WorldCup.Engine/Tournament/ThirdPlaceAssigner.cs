namespace WorldCup.Engine.Tournament;

/// <summary>
/// Assigns the eight qualifying third-placed teams to the eight group-winner R32 slots. FIFA's full
/// 495-row Annex C table is approximated by a deterministic bipartite matching that honours the
/// per-slot eligibility sets (and, failing that, the fundamental "no same-group rematch" rule). The
/// result is reproducible (not random) and never pairs a third-placed team against a side from its
/// own group.
/// </summary>
public static class ThirdPlaceAssigner
{
    /// <summary>
    /// Returns a mapping winner-group → the group whose third-placed team fills that slot.
    /// </summary>
    /// <param name="winnerGroups">The eight winner-group letters that host a third-placed team.</param>
    /// <param name="qualifyingThirdGroups">The eight groups that produced a qualifying third-placed team.</param>
    /// <param name="eligible">Per winner-group, the groups eligible to be assigned there.</param>
    public static Dictionary<char, char> Assign(
        IReadOnlyList<char> winnerGroups,
        IReadOnlyList<char> qualifyingThirdGroups,
        IReadOnlyDictionary<char, IReadOnlyList<char>> eligible)
    {
        var sortedThirds = qualifyingThirdGroups.OrderBy(c => c).ToArray();

        var strict = TryMatch(winnerGroups, sortedThirds, eligible, strict: true);
        if (strict is not null)
        {
            return strict;
        }

        var relaxed = TryMatch(winnerGroups, sortedThirds, eligible, strict: false);
        if (relaxed is not null)
        {
            return relaxed;
        }

        // Last resort (should never happen): positional pairing.
        var fallback = new Dictionary<char, char>();
        for (int i = 0; i < winnerGroups.Count && i < sortedThirds.Length; i++)
        {
            fallback[winnerGroups[i]] = sortedThirds[i];
        }

        return fallback;
    }

    private static Dictionary<char, char>? TryMatch(
        IReadOnlyList<char> winnerGroups,
        char[] thirds,
        IReadOnlyDictionary<char, IReadOnlyList<char>> eligible,
        bool strict)
    {
        var assignment = new Dictionary<char, char>(winnerGroups.Count);
        var used = new bool[thirds.Length];
        return Recurse(0) ? assignment : null;

        bool Recurse(int index)
        {
            if (index == winnerGroups.Count)
            {
                return true;
            }

            char winner = winnerGroups[index];
            for (int t = 0; t < thirds.Length; t++)
            {
                if (used[t])
                {
                    continue;
                }

                char source = thirds[t];
                bool allowed = strict
                    ? eligible.TryGetValue(winner, out var set) && set.Contains(source)
                    : source != winner;
                if (!allowed)
                {
                    continue;
                }

                used[t] = true;
                assignment[winner] = source;
                if (Recurse(index + 1))
                {
                    return true;
                }

                used[t] = false;
                assignment.Remove(winner);
            }

            return false;
        }
    }
}
