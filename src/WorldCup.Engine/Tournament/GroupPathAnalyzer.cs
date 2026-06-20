using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>
/// Builds a "path to victory and path to defeat" analysis for one team in one group: the current
/// table, the odds of each remaining fixture, the probability of every finishing tier, and the exact
/// combinations of results that win the group or get the team eliminated.
/// <para>
/// Two engines feed it. An <em>exact enumeration</em> of all 3^k win/draw/loss combinations of the
/// remaining matches gives the discrete scenarios and what is already mathematically settled (using
/// points, with goal-difference ties surfaced as a best..worst range). A <em>Monte Carlo</em> over the
/// remaining fixtures — real scorelines through the official tiebreakers — gives the headline
/// finishing probabilities and the "what we need from our own game" conditional shares.
/// </para>
/// </summary>
public static class GroupPathAnalyzer
{
    /// <summary>A still-to-play group fixture with its pre-computed win/draw/loss model odds.</summary>
    private readonly record struct RemMatch(
        Team Home, Team Away, double PHome, double PDraw, double PAway, bool Neutral)
    {
        public bool Involves(string code) =>
            string.Equals(Home.Code, code, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Away.Code, code, StringComparison.OrdinalIgnoreCase);
    }

    public static GroupPathAnalysis Analyze(
        TournamentData data,
        char group,
        string teamCode,
        SimulationParameters p,
        IReadOnlyList<PlayedResult> playedResults,
        long iterations,
        ulong seed,
        ProgressCounter? progress = null)
    {
        var teams = data.TeamsInGroup(group).ToList();
        if (teams.All(t => !string.Equals(t.Code, teamCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Team '{teamCode}' is not in group {group}.", nameof(teamCode));
        }

        var codes = teams.Select(t => t.Code).ToList();
        var selected = data.Team(teamCode);
        var hostCodes = data.Teams
            .Where(t => data.Metadata.Hosts.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
            .Select(t => t.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Split this group's six fixtures into already-played and still-to-play.
        var playedByPair = new Dictionary<(string, string), PlayedResult>();
        foreach (var r in playedResults)
        {
            playedByPair[Key(r.HomeCode, r.AwayCode)] = r;
        }

        var playedOutcomes = new List<GroupMatchOutcome>();
        var remaining = new List<RemMatch>();
        foreach (var f in data.GroupSchedule.Where(f => f.Group == group))
        {
            var home = data.Team(f.HomeCode);
            var away = data.Team(f.AwayCode);
            if (playedByPair.TryGetValue(Key(f.HomeCode, f.AwayCode), out var pr))
            {
                bool same = string.Equals(pr.HomeCode, f.HomeCode, StringComparison.OrdinalIgnoreCase);
                int hg = same ? pr.HomeGoals : pr.AwayGoals;
                int ag = same ? pr.AwayGoals : pr.HomeGoals;
                playedOutcomes.Add(new GroupMatchOutcome(home.Code, away.Code, hg, ag, 0, 0));
            }
            else
            {
                bool neutral = !hostCodes.Contains(home.Code);
                var (lh, la) = MatchModel.ExpectedGoals(
                    p.EffectiveStrength(home), p.EffectiveStrength(away), p.Global, neutral);
                var grid = MatchModel.ScoreGridWithForm(lh, la, p.Global.DrawCoupling, p.Global.UpsetVariance);
                var (hw, dr, aw) = MatchModel.OutcomeProbabilities(grid);
                remaining.Add(new RemMatch(home, away, hw, dr, aw, neutral));
            }
        }

        // Current table (partial — only the games already played).
        var lotsRng = new Xoshiro256(seed ^ 0xA5A5A5A5A5A5A5A5UL);
        var partial = GroupStandingsCalculator.Compute(group, codes, playedOutcomes, ref lotsRng);
        var standings = partial
            .Select(s => new GroupStandingRow(
                s.Rank, s.Code, data.Team(s.Code).Name, s.Played, s.Won, s.Drawn, s.Lost,
                s.GoalsFor, s.GoalsAgainst, s.Points,
                string.Equals(s.Code, selected.Code, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var curPoints = partial.ToDictionary(s => s.Code, s => s.Points, StringComparer.OrdinalIgnoreCase);

        var remOdds = remaining
            .Select(m => new RemainingFixtureOdds(
                m.Home.Code, m.Home.Name, m.Away.Code, m.Away.Name,
                m.PHome, m.PDraw, m.PAway, m.Involves(selected.Code)))
            .ToList();

        int ownRemaining = remaining.Count(m => m.Involves(selected.Code));
        bool groupComplete = remaining.Count == 0;
        int finalRank = groupComplete
            ? partial.First(s => string.Equals(s.Code, selected.Code, StringComparison.OrdinalIgnoreCase)).Rank
            : 0;

        var (victory, defeat, victoryMass, defeatMass, globalBest, globalWorst, totalCombos) =
            Enumerate(remaining, curPoints, selected.Code, data);

        var (winGroup, runnerUp, third, fourth, branches) =
            MonteCarlo(remaining, playedOutcomes, codes, group, selected.Code, ownRemaining, p, iterations, seed, progress);

        return new GroupPathAnalysis(
            Group: group,
            TeamCode: selected.Code,
            TeamName: selected.Name,
            ParameterLabel: p.Label,
            Seed: seed,
            Iterations: groupComplete ? 0 : iterations,
            Standings: standings,
            RemainingFixtures: remOdds,
            OwnRemaining: ownRemaining,
            GroupComplete: groupComplete,
            FinalRankIfComplete: finalRank,
            WinGroup: winGroup,
            RunnerUp: runnerUp,
            ThirdPlace: third,
            Eliminated: fourth,
            AdvanceDirect: winGroup + runnerUp,
            ClinchedWinGroup: globalWorst == 1,
            ClinchedAdvance: globalWorst <= 2,
            CannotWinGroup: globalBest > 1,
            CannotAdvance: globalBest > 2,
            CannotFinishLast: globalWorst < 4,
            OwnResultBranches: branches,
            VictoryScenarios: victory,
            DefeatScenarios: defeat,
            VictoryMass: victoryMass,
            DefeatMass: defeatMass,
            TotalCombinations: totalCombos);
    }

    /// <summary>
    /// Enumerate EVERY combination of the remaining group results (3^k for k games still to play) and,
    /// for each, who finishes in the top two. Hypothetical games use representative 1–0 / 0–0 / 0–1
    /// scores so the goal-difference tie-breaks are deterministic; real margins can shuffle close ties.
    /// </summary>
    public static GroupPermutations Permutations(
        TournamentData data,
        char group,
        IReadOnlyList<PlayedResult> playedResults,
        ulong seed,
        string? selectedCode = null)
    {
        var codes = data.TeamsInGroup(group).Select(t => t.Code).ToList();
        var playedByPair = new Dictionary<(string, string), PlayedResult>();
        foreach (var r in playedResults)
        {
            playedByPair[Key(r.HomeCode, r.AwayCode)] = r;
        }

        var playedOutcomes = new List<GroupMatchOutcome>();
        var remaining = new List<(Team Home, Team Away)>();
        foreach (var f in data.GroupSchedule.Where(f => f.Group == group))
        {
            var home = data.Team(f.HomeCode);
            var away = data.Team(f.AwayCode);
            if (playedByPair.TryGetValue(Key(f.HomeCode, f.AwayCode), out var pr))
            {
                bool same = string.Equals(pr.HomeCode, f.HomeCode, StringComparison.OrdinalIgnoreCase);
                int hg = same ? pr.HomeGoals : pr.AwayGoals;
                int ag = same ? pr.AwayGoals : pr.HomeGoals;
                playedOutcomes.Add(new GroupMatchOutcome(home.Code, away.Code, hg, ag, 0, 0));
            }
            else
            {
                remaining.Add((home, away));
            }
        }

        int k = remaining.Count;
        var fixtures = remaining
            .Select(m => new RemainingFixtureLabel(m.Home.Code, m.Home.Name, m.Away.Code, m.Away.Name))
            .ToList();

        int total = (int)Math.Pow(3, k);
        var rows = new List<PermutationRow>(total);
        var baseRng = new Xoshiro256(seed);
        for (int combo = 0; combo < total; combo++)
        {
            var outcomes = new int[k];
            var sims = new List<GroupMatchOutcome>(playedOutcomes);
            int c = combo;
            for (int j = 0; j < k; j++)
            {
                int o = c % 3;
                c /= 3;
                var (home, away) = remaining[j];
                (int hg, int ag, int sign) = o switch
                {
                    0 => (1, 0, 1),
                    1 => (0, 0, 0),
                    _ => (0, 1, -1),
                };
                outcomes[j] = sign;
                sims.Add(new GroupMatchOutcome(home.Code, away.Code, hg, ag, 0, 0));
            }

            var rng = baseRng; // value copy: identical lots seed for every combination → deterministic
            var table = GroupStandingsCalculator.Compute(group, codes, sims, ref rng);
            bool selQual = selectedCode is not null &&
                (string.Equals(table[0].Code, selectedCode, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(table[1].Code, selectedCode, StringComparison.OrdinalIgnoreCase));
            rows.Add(new PermutationRow(
                outcomes,
                table[0].Code, data.Team(table[0].Code).Name,
                table[1].Code, data.Team(table[1].Code).Name,
                table[2].Code, data.Team(table[2].Code).Name,
                selQual));
        }

        return new GroupPermutations(group, fixtures, rows, total, selectedCode);
    }

    /// <summary>
    /// Analyse the WHOLE group at once: every team's current standing and finishing-tier probabilities,
    /// from a single shared Monte Carlo (so the four teams' shares are mutually consistent — exactly one
    /// of them wins the group in each simulation), plus what is already mathematically settled for each.
    /// </summary>
    public static GroupOutlook AnalyzeGroup(
        TournamentData data,
        char group,
        SimulationParameters p,
        IReadOnlyList<PlayedResult> playedResults,
        long iterations,
        ulong seed,
        ProgressCounter? progress = null)
    {
        var teams = data.TeamsInGroup(group).ToList();
        var codes = teams.Select(t => t.Code).ToList();
        var hostCodes = data.Teams
            .Where(t => data.Metadata.Hosts.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
            .Select(t => t.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var playedByPair = new Dictionary<(string, string), PlayedResult>();
        foreach (var r in playedResults)
        {
            playedByPair[Key(r.HomeCode, r.AwayCode)] = r;
        }

        var played = new List<GroupMatchOutcome>();
        var remaining = new List<RemMatch>();
        foreach (var f in data.GroupSchedule.Where(f => f.Group == group))
        {
            var home = data.Team(f.HomeCode);
            var away = data.Team(f.AwayCode);
            if (playedByPair.TryGetValue(Key(f.HomeCode, f.AwayCode), out var pr))
            {
                bool same = string.Equals(pr.HomeCode, f.HomeCode, StringComparison.OrdinalIgnoreCase);
                int hg = same ? pr.HomeGoals : pr.AwayGoals;
                int ag = same ? pr.AwayGoals : pr.HomeGoals;
                played.Add(new GroupMatchOutcome(home.Code, away.Code, hg, ag, 0, 0));
            }
            else
            {
                bool neutral = !hostCodes.Contains(home.Code);
                var (lh, la) = MatchModel.ExpectedGoals(p.EffectiveStrength(home), p.EffectiveStrength(away), p.Global, neutral);
                var grid = MatchModel.ScoreGridWithForm(lh, la, p.Global.DrawCoupling, p.Global.UpsetVariance);
                var (hw, dr, aw) = MatchModel.OutcomeProbabilities(grid);
                remaining.Add(new RemMatch(home, away, hw, dr, aw, neutral));
            }
        }

        bool groupComplete = remaining.Count == 0;
        var lotsRng = new Xoshiro256(seed ^ 0xA5A5A5A5A5A5A5A5UL);
        var partial = GroupStandingsCalculator.Compute(group, codes, played, ref lotsRng);
        var curPoints = partial.ToDictionary(s => s.Code, s => s.Points, StringComparer.OrdinalIgnoreCase);

        var remOdds = remaining
            .Select(m => new RemainingFixtureOdds(m.Home.Code, m.Home.Name, m.Away.Code, m.Away.Name, m.PHome, m.PDraw, m.PAway, false))
            .ToList();

        // One Monte Carlo pass; tally every team's finishing rank each iteration.
        long n = Math.Max(1, iterations);
        var rankCounts = codes.ToDictionary(c => c, _ => new long[5], StringComparer.OrdinalIgnoreCase);
        var rng = new Xoshiro256(seed);
        var sims = new List<GroupMatchOutcome>(played.Count + remaining.Count);
        for (long it = 0; it < n; it++)
        {
            sims.Clear();
            sims.AddRange(played);
            foreach (var m in remaining)
            {
                var res = MatchSimulator.Simulate(m.Home, m.Away, Stage.Group, Fidelity.Fast, p, ref rng, m.Neutral);
                sims.Add(new GroupMatchOutcome(m.Home.Code, m.Away.Code, res.HomeGoals, res.AwayGoals, 0, 0));
            }

            var table = GroupStandingsCalculator.Compute(group, codes, sims, ref rng);
            foreach (var rowS in table)
            {
                rankCounts[rowS.Code][rowS.Rank]++;
            }

            if ((it & 0x3FF) == 0)
            {
                progress?.Add(1024);
            }
        }

        var bounds = RankBoundsAll(remaining, curPoints, codes);
        double inv = 1.0 / n;
        var outlooks = new List<GroupTeamOutlook>();
        foreach (var s in partial) // already ordered by current rank
        {
            var rc = rankCounts[s.Code];
            double p1 = rc[1] * inv, p2 = rc[2] * inv, p3 = rc[3] * inv, p4 = rc[4] * inv;
            var (best, worst) = bounds[s.Code];
            bool clinchWin = worst == 1, clinchAdv = worst <= 2, cannotAdv = best > 2, cannotLast = worst < 4;
            var rowStanding = new GroupStandingRow(
                s.Rank, s.Code, data.Team(s.Code).Name, s.Played, s.Won, s.Drawn, s.Lost,
                s.GoalsFor, s.GoalsAgainst, s.Points, false);
            outlooks.Add(new GroupTeamOutlook(
                rowStanding, p1, p2, p3, p4, p1 + p2, clinchWin, clinchAdv, cannotAdv, cannotLast,
                GroupStatus(groupComplete, s.Rank, clinchWin, clinchAdv, cannotAdv, best, p1 + p2)));
        }

        return new GroupOutlook(group, p.Label, seed, groupComplete ? 0 : iterations, groupComplete, remOdds, outlooks);
    }

    /// <summary>Rank best/worst bounds (from points) for EVERY team in one enumeration pass over the remaining results.</summary>
    private static Dictionary<string, (int Best, int Worst)> RankBoundsAll(
        List<RemMatch> remaining, Dictionary<string, int> curPoints, List<string> codes)
    {
        int total = (int)Math.Pow(3, remaining.Count);
        var best = codes.ToDictionary(c => c, _ => 4, StringComparer.OrdinalIgnoreCase);
        var worst = codes.ToDictionary(c => c, _ => 1, StringComparer.OrdinalIgnoreCase);
        var points = new Dictionary<string, int>(curPoints, StringComparer.OrdinalIgnoreCase);

        for (int combo = 0; combo < total; combo++)
        {
            foreach (var c in curPoints.Keys)
            {
                points[c] = curPoints[c];
            }

            int code = combo;
            foreach (var m in remaining)
            {
                int digit = code % 3;
                code /= 3;
                if (digit == 0) points[m.Home.Code] += 3;
                else if (digit == 1) { points[m.Home.Code] += 1; points[m.Away.Code] += 1; }
                else points[m.Away.Code] += 3;
            }

            foreach (var sel in codes)
            {
                int selPts = points[sel];
                int above = 0, tied = 0;
                foreach (var kv in points)
                {
                    if (string.Equals(kv.Key, sel, StringComparison.OrdinalIgnoreCase)) continue;
                    if (kv.Value > selPts) above++;
                    else if (kv.Value == selPts) tied++;
                }

                int bestRank = above + 1, worstRank = above + tied + 1;
                if (bestRank < best[sel]) best[sel] = bestRank;
                if (worstRank > worst[sel]) worst[sel] = worstRank;
            }
        }

        return codes.ToDictionary(c => c, c => (best[c], worst[c]), StringComparer.OrdinalIgnoreCase);
    }

    private static string GroupStatus(bool complete, int rank, bool clinchWin, bool clinchAdv, bool cannotAdv, int bestRank, double advance)
    {
        if (complete)
        {
            return rank switch { 1 => "Won the group", 2 => "Runner-up", 3 => "3rd — best-third lottery", _ => "Eliminated" };
        }

        if (clinchWin) return "Won the group ✓";
        if (clinchAdv) return "Qualified ✓";
        if (bestRank == 4) return "Eliminated";
        if (cannotAdv) return "3rd-place hopes only";
        return advance >= 0.66 ? "In command" : advance >= 0.33 ? "In the mix" : "Up against it";
    }

    /// <summary>
    /// Walk all 3^k win/draw/loss combinations of the remaining matches. For each, the team's points
    /// are fixed; its place is the points rank, widened to a best..worst range when it ties others
    /// (the tie is then settled by goal difference / head-to-head, which depends on actual scorelines).
    /// </summary>
    private static (List<GroupPathScenario> Victory, List<GroupPathScenario> Defeat,
        double VictoryMass, double DefeatMass, int GlobalBest, int GlobalWorst, int Total)
        Enumerate(List<RemMatch> remaining, Dictionary<string, int> curPoints, string sel, TournamentData data)
    {
        int k = remaining.Count;
        int total = (int)Math.Pow(3, k);
        var victory = new List<GroupPathScenario>();
        var defeat = new List<GroupPathScenario>();
        double victoryMass = 0, defeatMass = 0;
        int globalBest = 4, globalWorst = 1;

        var points = new Dictionary<string, int>(curPoints, StringComparer.OrdinalIgnoreCase);

        for (int combo = 0; combo < total; combo++)
        {
            // Reset to the current points, then apply this combination's results.
            foreach (var c in curPoints.Keys)
            {
                points[c] = curPoints[c];
            }

            double prob = 1.0;
            var outcomes = new List<ScenarioOutcome>(k);
            int code = combo;
            foreach (var m in remaining)
            {
                int digit = code % 3;
                code /= 3;
                int sign;
                if (digit == 0) // home win
                {
                    points[m.Home.Code] += 3;
                    prob *= m.PHome;
                    sign = 1;
                }
                else if (digit == 1) // draw
                {
                    points[m.Home.Code] += 1;
                    points[m.Away.Code] += 1;
                    prob *= m.PDraw;
                    sign = 0;
                }
                else // away win
                {
                    points[m.Away.Code] += 3;
                    prob *= m.PAway;
                    sign = -1;
                }

                outcomes.Add(new ScenarioOutcome(
                    m.Home.Code, m.Home.Name, m.Away.Code, m.Away.Name, sign, Describe(m.Home.Name, m.Away.Name, sign)));
            }

            int selPts = points[sel];
            int above = 0, tied = 0;
            foreach (var kv in points)
            {
                if (string.Equals(kv.Key, sel, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (kv.Value > selPts)
                {
                    above++;
                }
                else if (kv.Value == selPts)
                {
                    tied++;
                }
            }

            int bestRank = above + 1;
            int worstRank = above + tied + 1;
            globalBest = Math.Min(globalBest, bestRank);
            globalWorst = Math.Max(globalWorst, worstRank);

            var scenario = new GroupPathScenario(outcomes, prob, bestRank, worstRank, bestRank != worstRank);

            if (bestRank == 1)
            {
                victory.Add(scenario);
                victoryMass += prob;
            }

            if (worstRank == 4)
            {
                defeat.Add(scenario);
                defeatMass += prob;
            }
        }

        victory.Sort((a, b) => b.Probability.CompareTo(a.Probability));
        defeat.Sort((a, b) => b.Probability.CompareTo(a.Probability));
        return (victory, defeat, victoryMass, defeatMass, globalBest, globalWorst, total);
    }

    /// <summary>
    /// Monte Carlo the remaining fixtures with real scorelines and the official tiebreakers to get the
    /// finishing-tier probabilities, plus the conditional shares bucketed by the team's own points haul.
    /// </summary>
    private static (double WinGroup, double RunnerUp, double Third, double Fourth, List<OwnResultBranch> Branches)
        MonteCarlo(List<RemMatch> remaining, List<GroupMatchOutcome> played, List<string> codes, char group,
            string sel, int ownRemaining, SimulationParameters p, long iterations, ulong seed, ProgressCounter? progress)
    {
        long n = Math.Max(1, iterations);
        long win = 0, second = 0, third = 0, fourth = 0;

        // Conditional buckets keyed by points the team takes from its own remaining matches.
        var bucketTotal = new Dictionary<int, long>();
        var bucketWin = new Dictionary<int, long>();
        var bucketAdvance = new Dictionary<int, long>();
        var bucketElim = new Dictionary<int, long>();

        var rng = new Xoshiro256(seed);
        var sims = new List<GroupMatchOutcome>(played.Count + remaining.Count);

        for (long it = 0; it < n; it++)
        {
            sims.Clear();
            sims.AddRange(played);
            int ownPts = 0;
            foreach (var m in remaining)
            {
                var res = MatchSimulator.Simulate(m.Home, m.Away, Stage.Group, Fidelity.Fast, p, ref rng, m.Neutral);
                sims.Add(new GroupMatchOutcome(m.Home.Code, m.Away.Code, res.HomeGoals, res.AwayGoals, 0, 0));

                if (string.Equals(m.Home.Code, sel, StringComparison.OrdinalIgnoreCase))
                {
                    ownPts += res.HomeGoals > res.AwayGoals ? 3 : res.HomeGoals == res.AwayGoals ? 1 : 0;
                }
                else if (string.Equals(m.Away.Code, sel, StringComparison.OrdinalIgnoreCase))
                {
                    ownPts += res.AwayGoals > res.HomeGoals ? 3 : res.AwayGoals == res.HomeGoals ? 1 : 0;
                }
            }

            var table = GroupStandingsCalculator.Compute(group, codes, sims, ref rng);
            int rank = table.First(s => string.Equals(s.Code, sel, StringComparison.OrdinalIgnoreCase)).Rank;

            switch (rank)
            {
                case 1: win++; break;
                case 2: second++; break;
                case 3: third++; break;
                default: fourth++; break;
            }

            if (ownRemaining > 0)
            {
                bucketTotal[ownPts] = bucketTotal.GetValueOrDefault(ownPts) + 1;
                if (rank == 1) bucketWin[ownPts] = bucketWin.GetValueOrDefault(ownPts) + 1;
                if (rank <= 2) bucketAdvance[ownPts] = bucketAdvance.GetValueOrDefault(ownPts) + 1;
                if (rank == 4) bucketElim[ownPts] = bucketElim.GetValueOrDefault(ownPts) + 1;
            }

            if ((it & 0x3FF) == 0)
            {
                progress?.Add(1024);
            }
        }

        var branches = new List<OwnResultBranch>();
        foreach (var pts in bucketTotal.Keys.OrderByDescending(x => x))
        {
            long tot = bucketTotal[pts];
            double winShare = (double)bucketWin.GetValueOrDefault(pts) / tot;
            double advShare = (double)bucketAdvance.GetValueOrDefault(pts) / tot;
            double elimShare = (double)bucketElim.GetValueOrDefault(pts) / tot;
            branches.Add(new OwnResultBranch(
                Label: BranchLabel(pts, ownRemaining),
                PointsGained: pts,
                Probability: (double)tot / n,
                WinGroup: winShare,
                Advance: advShare,
                Eliminated: elimShare,
                Verdict: Verdict(winShare, advShare, elimShare)));
        }

        return ((double)win / n, (double)second / n, (double)third / n, (double)fourth / n, branches);
    }

    private static string BranchLabel(int points, int ownRemaining)
    {
        if (ownRemaining == 1)
        {
            return points switch { 3 => "Win", 1 => "Draw", _ => "Lose" };
        }

        return points == 1 ? "Take 1 pt" : $"Take {points} pts";
    }

    private static string Verdict(double win, double advance, double elim)
    {
        const double sure = 0.9995;
        if (advance >= sure)
        {
            return win >= sure ? "Wins the group — guaranteed" : "Through to the knockouts — guaranteed";
        }

        if (elim >= sure)
        {
            return "Eliminated — out regardless of other results";
        }

        if (win < 1e-4 && advance < 1e-4)
        {
            return "Cannot finish in the top two";
        }

        return "Depends on the other result(s)";
    }

    private static string Describe(string home, string away, int sign) => sign switch
    {
        1 => $"{home} beat {away}",
        -1 => $"{away} beat {home}",
        _ => $"{home} draw {away}",
    };

    private static (string, string) Key(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
}
