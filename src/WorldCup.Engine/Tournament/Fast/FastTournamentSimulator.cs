using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament.Fast;

/// <summary>Reusable per-thread scratch buffers so a tournament can be simulated without allocation.</summary>
internal sealed class FastScratch
{
    public FastScratch(int teamCount)
    {
        StageReached = new int[teamCount];
    }

    public readonly int[] StageReached;
    public readonly int[] WinnerTeam = new int[FastTournamentModel.GroupCount];
    public readonly int[] RunnerUpTeam = new int[FastTournamentModel.GroupCount];
    public readonly int[] ThirdTeam = new int[FastTournamentModel.GroupCount];
    public readonly int[] ThirdPoints = new int[FastTournamentModel.GroupCount];
    public readonly int[] ThirdGd = new int[FastTournamentModel.GroupCount];
    public readonly int[] ThirdGf = new int[FastTournamentModel.GroupCount];
    public readonly bool[] ThirdQualified = new bool[FastTournamentModel.GroupCount];
    public readonly int[] WinnerOf = new int[FastTournamentModel.KnockoutMatches];
    public readonly int[] LoserOf = new int[FastTournamentModel.KnockoutMatches];
    public readonly int[] ThirdSourceForWinnerGroup = new int[FastTournamentModel.GroupCount];
    public readonly bool[] UsedThird = new bool[FastTournamentModel.GroupCount];
    public readonly int[] AssignBuffer = new int[FastTournamentModel.GroupCount];
}

/// <summary>
/// The allocation-free, index-based single-tournament simulator that powers fast-mode Monte Carlo.
/// One call simulates a full tournament and folds the outcome into a <see cref="TournamentAggregator"/>.
/// </summary>
internal static class FastTournamentSimulator
{
    public static void Run(
        FastTournamentModel m, GlobalParameters g, FastScratch s, TournamentAggregator agg,
        bool includeThirdPlace, ref Xoshiro256 rng)
    {
        Array.Clear(s.StageReached, 0, m.TeamCount);

        // --- Group stage ---
        Span<int> pts = stackalloc int[4];
        Span<int> gf = stackalloc int[4];
        Span<int> ga = stackalloc int[4];
        Span<int> mHomeLocal = stackalloc int[6];
        Span<int> mAwayLocal = stackalloc int[6];
        Span<int> mHg = stackalloc int[6];
        Span<int> mAg = stackalloc int[6];
        Span<int> order = stackalloc int[4];

        for (int gi = 0; gi < FastTournamentModel.GroupCount; gi++)
        {
            int[] gt = m.GroupTeams[gi];
            pts.Clear();
            gf.Clear();
            ga.Clear();

            for (int mm = 0; mm < 6; mm++)
            {
                var (la, lb) = FastTournamentModel.GroupPattern[mm];
                int home = gt[la];
                int away = gt[lb];
                int hg, ag;
                int idx = gi * 6 + mm;
                if (m.LockedFlag[idx])
                {
                    hg = m.LockedHomeGoals[idx];
                    ag = m.LockedAwayGoals[idx];
                }
                else
                {
                    bool neutral = !m.IsHost[home];
                    var r = FastMatchSimulator.SimulateRegulation(ref rng, m.Strength[home], m.Strength[away], g, neutral);
                    hg = r.HomeGoals;
                    ag = r.AwayGoals;
                }

                mHomeLocal[mm] = la;
                mAwayLocal[mm] = lb;
                mHg[mm] = hg;
                mAg[mm] = ag;

                Accumulate(pts, gf, ga, la, hg, ag);
                Accumulate(pts, gf, ga, lb, ag, hg);
            }

            RankGroup(pts, gf, ga, mHomeLocal, mAwayLocal, mHg, mAg, order, ref rng);

            int winner = gt[order[0]];
            int runner = gt[order[1]];
            int third = gt[order[2]];
            s.WinnerTeam[gi] = winner;
            s.RunnerUpTeam[gi] = runner;
            s.ThirdTeam[gi] = third;
            s.ThirdPoints[gi] = pts[order[2]];
            s.ThirdGd[gi] = gf[order[2]] - ga[order[2]];
            s.ThirdGf[gi] = gf[order[2]];
            s.ThirdQualified[gi] = false;

            for (int k = 0; k < 4; k++)
            {
                agg.GroupPointsSum[gt[order[k]]] += pts[order[k]];
            }
        }

        // --- Best eight third-placed teams ---
        SelectBestThirds(m, s, ref rng);

        // Assign thirds to winner slots.
        AssignThirds(m, s, ref rng);

        // Mark advancement (reached R32) for winners, runners-up and qualified thirds.
        for (int gi = 0; gi < FastTournamentModel.GroupCount; gi++)
        {
            s.StageReached[s.WinnerTeam[gi]] = 1;
            s.StageReached[s.RunnerUpTeam[gi]] = 1;
            if (s.ThirdQualified[gi])
            {
                s.StageReached[s.ThirdTeam[gi]] = 1;
            }

            agg.TopGroup[s.WinnerTeam[gi]]++;
        }

        // --- Knockout stage ---
        int finalTop = -1, finalBottom = -1;
        int champion = -1;
        for (int mi = 0; mi < FastTournamentModel.KnockoutMatches; mi++)
        {
            if (mi == m.ThirdPlaceIndex && !includeThirdPlace)
            {
                continue;
            }

            int top = ResolveFeeder(m.TopFeeder[mi], m, s);
            int bottom = ResolveFeeder(m.BottomFeeder[mi], m, s);

            var r = FastMatchSimulator.SimulateKnockout(ref rng, m.Strength[top], m.Strength[bottom], g);
            int winnerTeam = r.WinnerIsHome == true ? top : bottom;
            int loserTeam = winnerTeam == top ? bottom : top;
            s.WinnerOf[mi] = winnerTeam;
            s.LoserOf[mi] = loserTeam;

            int rank = m.MatchStageRank[mi];
            if (rank > s.StageReached[top])
            {
                s.StageReached[top] = rank;
            }

            if (rank > s.StageReached[bottom])
            {
                s.StageReached[bottom] = rank;
            }

            if (mi == m.FinalIndex)
            {
                finalTop = top;
                finalBottom = bottom;
                champion = winnerTeam;
            }
        }

        // --- Fold into the aggregator ---
        for (int t = 0; t < m.TeamCount; t++)
        {
            int rank = s.StageReached[t];
            if (rank >= 1) agg.ReachedR32[t]++;
            if (rank >= 2) agg.ReachedR16[t]++;
            if (rank >= 3) agg.ReachedQuarter[t]++;
            if (rank >= 4) agg.ReachedSemi[t]++;
            if (rank >= 5) agg.ReachedFinal[t]++;
        }

        if (champion >= 0)
        {
            agg.Champion[champion]++;
        }

        if (finalTop >= 0 && finalBottom >= 0)
        {
            var key = finalTop < finalBottom ? (finalTop, finalBottom) : (finalBottom, finalTop);
            agg.FinalMatchups[key] = agg.FinalMatchups.TryGetValue(key, out long c) ? c + 1 : 1;
        }

        agg.Tournaments++;
    }

    private static void Accumulate(Span<int> pts, Span<int> gf, Span<int> ga, int team, int scored, int conceded)
    {
        gf[team] += scored;
        ga[team] += conceded;
        if (scored > conceded)
        {
            pts[team] += 3;
        }
        else if (scored == conceded)
        {
            pts[team] += 1;
        }
    }

    /// <summary>Rank the four teams of a group into <paramref name="order"/> (local indices 0..3).</summary>
    private static void RankGroup(
        Span<int> pts, Span<int> gf, Span<int> ga,
        Span<int> mHome, Span<int> mAway, Span<int> mHg, Span<int> mAg,
        Span<int> order, ref Xoshiro256 rng)
    {
        for (int i = 0; i < 4; i++)
        {
            order[i] = i;
        }

        // Insertion sort by points, GD, GF (descending).
        for (int i = 1; i < 4; i++)
        {
            int key = order[i];
            int j = i - 1;
            while (j >= 0 && PrimaryLess(order[j], key, pts, gf, ga))
            {
                order[j + 1] = order[j];
                j--;
            }

            order[j + 1] = key;
        }

        // Resolve clusters tied on points/GD/GF.
        int start = 0;
        while (start < 4)
        {
            int end = start + 1;
            while (end < 4 && PrimaryEqual(order[start], order[end], pts, gf, ga))
            {
                end++;
            }

            if (end - start > 1)
            {
                ResolveTie(order, start, end, mHome, mAway, mHg, mAg, ref rng);
            }

            start = end;
        }
    }

    private static bool PrimaryLess(int a, int b, Span<int> pts, Span<int> gf, Span<int> ga)
    {
        // True when team a should sort *below* b (so the insertion sort moves b up).
        int pa = pts[a], pb = pts[b];
        if (pa != pb) return pa < pb;
        int gda = gf[a] - ga[a], gdb = gf[b] - ga[b];
        if (gda != gdb) return gda < gdb;
        return gf[a] < gf[b];
    }

    private static bool PrimaryEqual(int a, int b, Span<int> pts, Span<int> gf, Span<int> ga) =>
        pts[a] == pts[b] && (gf[a] - ga[a]) == (gf[b] - ga[b]) && gf[a] == gf[b];

    private static void ResolveTie(
        Span<int> order, int start, int end,
        Span<int> mHome, Span<int> mAway, Span<int> mHg, Span<int> mAg, ref Xoshiro256 rng)
    {
        Span<int> h2hPts = stackalloc int[4];
        Span<int> h2hGd = stackalloc int[4];
        Span<int> h2hGf = stackalloc int[4];
        Span<ulong> lot = stackalloc ulong[4];
        for (int i = 0; i < 4; i++)
        {
            h2hPts[i] = 0;
            h2hGd[i] = 0;
            h2hGf[i] = 0;
            lot[i] = 0;
        }

        // Membership of the tied cluster (local team indices).
        Span<bool> inCluster = stackalloc bool[4];
        inCluster.Clear();
        for (int i = start; i < end; i++)
        {
            inCluster[order[i]] = true;
            lot[order[i]] = rng.NextUInt64();
        }

        for (int mm = 0; mm < 6; mm++)
        {
            int a = mHome[mm], b = mAway[mm];
            if (!inCluster[a] || !inCluster[b])
            {
                continue;
            }

            h2hGf[a] += mHg[mm];
            h2hGf[b] += mAg[mm];
            h2hGd[a] += mHg[mm] - mAg[mm];
            h2hGd[b] += mAg[mm] - mHg[mm];
            if (mHg[mm] > mAg[mm]) h2hPts[a] += 3;
            else if (mHg[mm] < mAg[mm]) h2hPts[b] += 3;
            else { h2hPts[a] += 1; h2hPts[b] += 1; }
        }

        // Insertion sort the sub-range by H2H (pts, GD, GF desc), then lots (asc).
        for (int i = start + 1; i < end; i++)
        {
            int key = order[i];
            int j = i - 1;
            while (j >= start && TieLess(order[j], key, h2hPts, h2hGd, h2hGf, lot))
            {
                order[j + 1] = order[j];
                j--;
            }

            order[j + 1] = key;
        }
    }

    private static bool TieLess(int a, int b, Span<int> p, Span<int> gd, Span<int> gfh, Span<ulong> lot)
    {
        if (p[a] != p[b]) return p[a] < p[b];
        if (gd[a] != gd[b]) return gd[a] < gd[b];
        if (gfh[a] != gfh[b]) return gfh[a] < gfh[b];
        return lot[a] > lot[b]; // smaller lot ranks higher
    }

    private static void SelectBestThirds(FastTournamentModel m, FastScratch s, ref Xoshiro256 rng)
    {
        // Rank the 12 groups' third-placed teams by points, GD, GF, then lots; qualify the top 8.
        Span<int> idx = stackalloc int[FastTournamentModel.GroupCount];
        Span<ulong> lot = stackalloc ulong[FastTournamentModel.GroupCount];
        for (int i = 0; i < FastTournamentModel.GroupCount; i++)
        {
            idx[i] = i;
            lot[i] = rng.NextUInt64();
        }

        for (int i = 1; i < FastTournamentModel.GroupCount; i++)
        {
            int key = idx[i];
            int j = i - 1;
            while (j >= 0 && ThirdLess(idx[j], key, s, lot))
            {
                idx[j + 1] = idx[j];
                j--;
            }

            idx[j + 1] = key;
        }

        for (int i = 0; i < 8; i++)
        {
            s.ThirdQualified[idx[i]] = true;
        }
    }

    private static bool ThirdLess(int a, int b, FastScratch s, Span<ulong> lot)
    {
        if (s.ThirdPoints[a] != s.ThirdPoints[b]) return s.ThirdPoints[a] < s.ThirdPoints[b];
        if (s.ThirdGd[a] != s.ThirdGd[b]) return s.ThirdGd[a] < s.ThirdGd[b];
        if (s.ThirdGf[a] != s.ThirdGf[b]) return s.ThirdGf[a] < s.ThirdGf[b];
        return lot[a] > lot[b];
    }

    private static void AssignThirds(FastTournamentModel m, FastScratch s, ref Xoshiro256 rng)
    {
        // Qualifying third groups, ascending.
        Span<int> qualifying = stackalloc int[8];
        int qc = 0;
        for (int gi = 0; gi < FastTournamentModel.GroupCount; gi++)
        {
            if (s.ThirdQualified[gi])
            {
                qualifying[qc++] = gi;
            }
        }

        Array.Clear(s.UsedThird, 0, s.UsedThird.Length);
        bool ok = MatchThirds(m, s, qualifying, 0, strict: true);
        if (!ok)
        {
            Array.Clear(s.UsedThird, 0, s.UsedThird.Length);
            MatchThirds(m, s, qualifying, 0, strict: false);
        }
    }

    private static bool MatchThirds(FastTournamentModel m, FastScratch s, Span<int> qualifying, int slot, bool strict)
    {
        if (slot == m.WinnerGroupsForThirds.Length)
        {
            return true;
        }

        int winnerGroup = m.WinnerGroupsForThirds[slot];
        int[] eligible = m.ThirdEligibleGroups[slot];
        for (int q = 0; q < qualifying.Length; q++)
        {
            int source = qualifying[q];
            if (s.UsedThird[source])
            {
                continue;
            }

            bool allowed = strict ? Contains(eligible, source) : source != winnerGroup;
            if (!allowed)
            {
                continue;
            }

            s.UsedThird[source] = true;
            s.ThirdSourceForWinnerGroup[winnerGroup] = source;
            if (MatchThirds(m, s, qualifying, slot + 1, strict))
            {
                return true;
            }

            s.UsedThird[source] = false;
        }

        return false;
    }

    private static bool Contains(int[] arr, int value)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == value)
            {
                return true;
            }
        }

        return false;
    }

    private static int ResolveFeeder(FastFeeder f, FastTournamentModel m, FastScratch s) => f.Kind switch
    {
        FastFeeder.GroupWinner => s.WinnerTeam[f.Arg],
        FastFeeder.GroupRunnerUp => s.RunnerUpTeam[f.Arg],
        FastFeeder.GroupThird => s.ThirdTeam[s.ThirdSourceForWinnerGroup[f.Arg]],
        FastFeeder.MatchWinner => s.WinnerOf[f.Arg],
        FastFeeder.MatchLoser => s.LoserOf[f.Arg],
        _ => throw new InvalidOperationException(),
    };
}
