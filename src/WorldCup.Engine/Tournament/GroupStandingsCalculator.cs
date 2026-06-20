using WorldCup.Engine.Random;

namespace WorldCup.Engine.Tournament;

/// <summary>
/// Computes group standings and ranks teams using the official 2026 tiebreaker order:
/// (1) points, (2) goal difference, (3) goals scored, (4) head-to-head among the tied teams
/// (points, then GD, then goals scored in matches between them), (5) fair-play (fewer cards),
/// (6) drawing of lots. Also ranks the best third-placed teams across groups (criteria 1–3, 5–6;
/// head-to-head does not apply across groups).
/// </summary>
public static class GroupStandingsCalculator
{
    /// <summary>
    /// Compute the ordered standings (Rank 1..N) for one group.
    /// </summary>
    public static List<TeamStanding> Compute(
        char group, IReadOnlyList<string> teamCodes, IReadOnlyList<GroupMatchOutcome> matches, ref Xoshiro256 lotsRng)
    {
        var table = new Dictionary<string, TeamStanding>(StringComparer.Ordinal);
        foreach (var code in teamCodes)
        {
            table[code] = new TeamStanding(code) { Group = group };
        }

        foreach (var m in matches)
        {
            if (table.TryGetValue(m.Home, out var home) && table.TryGetValue(m.Away, out var away))
            {
                home.ApplyResult(m.HomeGoals, m.AwayGoals, m.HomeFairPlay);
                away.ApplyResult(m.AwayGoals, m.HomeGoals, m.AwayFairPlay);
            }
        }

        // Random lot value per team (drawn up front so we don't pass the RNG into the sort).
        var lots = new Dictionary<string, ulong>(StringComparer.Ordinal);
        foreach (var code in teamCodes)
        {
            lots[code] = lotsRng.NextUInt64();
        }

        var ordered = table.Values
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GoalDifference)
            .ThenByDescending(s => s.GoalsFor)
            .ToList();

        var result = new List<TeamStanding>(ordered.Count);
        int i = 0;
        while (i < ordered.Count)
        {
            int j = i + 1;
            while (j < ordered.Count && SameTopKeys(ordered[i], ordered[j]))
            {
                j++;
            }

            if (j - i == 1)
            {
                result.Add(ordered[i]);
            }
            else
            {
                result.AddRange(ResolveCluster(ordered.GetRange(i, j - i), matches, lots));
            }

            i = j;
        }

        for (int r = 0; r < result.Count; r++)
        {
            result[r].Rank = r + 1;
        }

        return result;
    }

    private static bool SameTopKeys(TeamStanding a, TeamStanding b) =>
        a.Points == b.Points && a.GoalDifference == b.GoalDifference && a.GoalsFor == b.GoalsFor;

    /// <summary>
    /// Resolve a cluster of teams tied on points/GD/GF via head-to-head, then fair-play, then lots.
    /// </summary>
    private static List<TeamStanding> ResolveCluster(
        List<TeamStanding> cluster, IReadOnlyList<GroupMatchOutcome> matches, Dictionary<string, ulong> lots)
    {
        var codes = cluster.Select(s => s.Code).ToHashSet(StringComparer.Ordinal);

        // Mini-table among the tied teams only.
        var h2hPoints = new Dictionary<string, int>(StringComparer.Ordinal);
        var h2hGd = new Dictionary<string, int>(StringComparer.Ordinal);
        var h2hGf = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in cluster)
        {
            h2hPoints[s.Code] = 0;
            h2hGd[s.Code] = 0;
            h2hGf[s.Code] = 0;
        }

        foreach (var m in matches)
        {
            if (!codes.Contains(m.Home) || !codes.Contains(m.Away))
            {
                continue;
            }

            h2hGf[m.Home] += m.HomeGoals;
            h2hGf[m.Away] += m.AwayGoals;
            h2hGd[m.Home] += m.HomeGoals - m.AwayGoals;
            h2hGd[m.Away] += m.AwayGoals - m.HomeGoals;
            if (m.HomeGoals > m.AwayGoals)
            {
                h2hPoints[m.Home] += 3;
            }
            else if (m.HomeGoals < m.AwayGoals)
            {
                h2hPoints[m.Away] += 3;
            }
            else
            {
                h2hPoints[m.Home] += 1;
                h2hPoints[m.Away] += 1;
            }
        }

        return cluster
            .OrderByDescending(s => h2hPoints[s.Code])
            .ThenByDescending(s => h2hGd[s.Code])
            .ThenByDescending(s => h2hGf[s.Code])
            .ThenBy(s => s.FairPlayPoints)
            .ThenBy(s => lots[s.Code])
            .ToList();
    }

    /// <summary>
    /// Rank third-placed teams across groups to pick the best eight. Uses points, GD, goals scored,
    /// fair-play, then lots (head-to-head is not applicable across groups).
    /// </summary>
    public static List<TeamStanding> RankThirdPlaced(IReadOnlyList<TeamStanding> thirds, ref Xoshiro256 lotsRng)
    {
        var lots = new Dictionary<string, ulong>(StringComparer.Ordinal);
        foreach (var t in thirds)
        {
            lots[t.Code] = lotsRng.NextUInt64();
        }

        return thirds
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GoalDifference)
            .ThenByDescending(s => s.GoalsFor)
            .ThenBy(s => s.FairPlayPoints)
            .ThenBy(s => lots[s.Code])
            .ToList();
    }
}
