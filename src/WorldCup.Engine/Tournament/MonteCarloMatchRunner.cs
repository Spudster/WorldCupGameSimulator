using System.Diagnostics;
using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>Monte Carlo for a single matchup: fast millions (regulation W/D/L) and detailed best-of-N.</summary>
public static class MonteCarloMatchRunner
{
    private const int MaxScore = 15; // goals beyond this are clamped into the frequency grid.

    /// <summary>Fast regulation Monte Carlo: W/D/L, expected goals, and the top scoreline frequencies.</summary>
    public static MatchMonteCarloReport RunFast(
        Team home, Team away, SimulationParameters p, long iterations, bool neutralVenue,
        ProgressCounter? progress = null, CancellationToken cancellationToken = default)
    {
        double hs = p.EffectiveStrength(home);
        double as_ = p.EffectiveStrength(away);
        var g = p.Global;

        int partitionCount = (int)Math.Min(256, Math.Max(1, iterations));
        int maxParallel = Math.Max(1, Environment.ProcessorCount);
        var grids = new long[partitionCount][];
        var homeWins = new long[partitionCount];
        var draws = new long[partitionCount];
        var awayWins = new long[partitionCount];
        var homeGoals = new long[partitionCount];
        var awayGoals = new long[partitionCount];
        var sw = Stopwatch.StartNew();

        Parallel.For(0, partitionCount, new ParallelOptions { MaxDegreeOfParallelism = maxParallel, CancellationToken = cancellationToken }, w =>
        {
            long start = iterations * w / partitionCount;
            long end = iterations * (w + 1) / partitionCount;
            long count = end - start;
            var grid = new long[(MaxScore + 1) * (MaxScore + 1)];
            var rng = new Xoshiro256(g.Seed + (ulong)(w + 1) * 0x9E3779B97F4A7C15UL);
            long hw = 0, dr = 0, aw = 0, hg = 0, ag = 0, sinceReport = 0;

            for (long i = 0; i < count; i++)
            {
                var r = FastMatchSimulator.SimulateRegulation(ref rng, hs, as_, g, neutralVenue);
                hg += r.HomeGoals;
                ag += r.AwayGoals;
                if (r.HomeGoals > r.AwayGoals) hw++;
                else if (r.HomeGoals < r.AwayGoals) aw++;
                else dr++;

                int hh = Math.Min(r.HomeGoals, MaxScore);
                int aa = Math.Min(r.AwayGoals, MaxScore);
                grid[hh * (MaxScore + 1) + aa]++;

                if (progress is not null && ++sinceReport >= 8192)
                {
                    progress.Add(sinceReport);
                    sinceReport = 0;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            if (progress is not null && sinceReport > 0)
            {
                progress.Add(sinceReport);
            }

            grids[w] = grid;
            homeWins[w] = hw;
            draws[w] = dr;
            awayWins[w] = aw;
            homeGoals[w] = hg;
            awayGoals[w] = ag;
        });

        sw.Stop();

        var totalGrid = new long[(MaxScore + 1) * (MaxScore + 1)];
        long totHw = 0, totDr = 0, totAw = 0, totHg = 0, totAg = 0;
        for (int w = 0; w < partitionCount; w++)
        {
            for (int k = 0; k < totalGrid.Length; k++)
            {
                totalGrid[k] += grids[w][k];
            }

            totHw += homeWins[w];
            totDr += draws[w];
            totAw += awayWins[w];
            totHg += homeGoals[w];
            totAg += awayGoals[w];
        }

        long n = totHw + totDr + totAw;
        double inv = n > 0 ? 1.0 / n : 0;

        var scorelines = new List<ScorelineFrequency>();
        for (int h = 0; h <= MaxScore; h++)
        {
            for (int a = 0; a <= MaxScore; a++)
            {
                long c = totalGrid[h * (MaxScore + 1) + a];
                if (c > 0)
                {
                    scorelines.Add(new ScorelineFrequency(h, a, c, c * inv));
                }
            }
        }

        scorelines = scorelines.OrderByDescending(s => s.Count).Take(12).ToList();
        double sims = sw.Elapsed.TotalSeconds > 0 ? n / sw.Elapsed.TotalSeconds : 0;

        return new MatchMonteCarloReport(
            n, home.Code, home.Name, away.Code, away.Name, p.Label, g.Seed,
            totHw * inv, totDr * inv, totAw * inv,
            totHg * inv, totAg * inv, sw.Elapsed.TotalSeconds, sims, scorelines);
    }

    /// <summary>
    /// The full single-matchup Monte Carlo: runs the detailed simulator N times (parallelised) and
    /// aggregates the most-probable score, W/D/L, scoreline frequencies AND every box-score average
    /// (cards, penalties, corners, shots, possession, …) for each side. Scales to millions of runs.
    /// </summary>
    public static MatchAggregateReport RunAggregate(
        Team home, Team away, SimulationParameters p, long iterations, Stage stage, bool neutralVenue,
        ProgressCounter? progress = null, CancellationToken cancellationToken = default)
    {
        var g = p.Global;
        int partitionCount = (int)Math.Min(256, Math.Max(1, iterations));
        int maxParallel = Math.Max(1, Environment.ProcessorCount);
        var partials = new MatchAcc[partitionCount];
        var sw = Stopwatch.StartNew();

        Parallel.For(0, partitionCount, new ParallelOptions { MaxDegreeOfParallelism = maxParallel, CancellationToken = cancellationToken }, part =>
        {
            long start = iterations * part / partitionCount;
            long end = iterations * (part + 1) / partitionCount;
            long count = end - start;
            var acc = new MatchAcc();
            var rng = new Xoshiro256(g.Seed + (ulong)(part + 1) * 0x9E3779B97F4A7C15UL);
            long sinceReport = 0;

            for (long i = 0; i < count; i++)
            {
                var r = DetailedMatchSimulator.Simulate(home, away, stage, p, ref rng, neutralVenue);
                acc.Add(r, home.Code, away.Code);
                if (progress is not null && ++sinceReport >= 4096)
                {
                    progress.Add(sinceReport);
                    sinceReport = 0;
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            if (progress is not null && sinceReport > 0)
            {
                progress.Add(sinceReport);
            }

            partials[part] = acc;
        });

        sw.Stop();

        var total = new MatchAcc();
        foreach (var a in partials)
        {
            total.Merge(a);
        }

        long n = Math.Max(1, total.N);
        double inv = 1.0 / n;

        var scorelines = new List<ScorelineFrequency>();
        for (int h = 0; h <= MaxScore; h++)
        {
            for (int a = 0; a <= MaxScore; a++)
            {
                long c = total.Grid[h * (MaxScore + 1) + a];
                if (c > 0)
                {
                    scorelines.Add(new ScorelineFrequency(h, a, c, c * inv));
                }
            }
        }

        scorelines = scorelines.OrderByDescending(s => s.Count).Take(12).ToList();
        double sims = sw.Elapsed.TotalSeconds > 0 ? n / sw.Elapsed.TotalSeconds : 0;

        // Most likely scorers (avg goals per match) across the run.
        var playerInfo = home.Squad.Concat(away.Squad)
            .ToDictionary(pl => pl.Id, pl => (pl.Name, pl.TeamCode), StringComparer.Ordinal);
        var topScorers = total.ScorerGoals
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .Select(kv =>
            {
                var info = playerInfo.TryGetValue(kv.Key, out var pi) ? pi : (kv.Key, string.Empty);
                return new ScorerLine(kv.Key, info.Item1, info.Item2, kv.Value * inv, kv.Value);
            })
            .ToList();

        BestGoalInfo? bestGoal = total.BestVergazo >= 0
            ? new BestGoalInfo(total.BestScorer, total.BestTeam, total.BestVergazo, total.BestType, total.BestDistance, total.BestMinute)
            : null;

        double goalInv = total.GoalCount > 0 ? 1.0 / total.GoalCount : 0;
        double avgVergazo = total.VergazoSum * goalInv;
        double worldiePercent = total.WorldieCount * goalInv * 100.0;

        return new MatchAggregateReport(
            n, home.Code, home.Name, away.Code, away.Name, p.Label, g.Seed,
            total.Hw * inv, total.Dr * inv, total.Aw * inv, total.Hg * inv, total.Ag * inv,
            sw.Elapsed.TotalSeconds, sims, scorelines,
            total.HomeAverages(home, inv), total.AwayAverages(away, inv),
            topScorers, bestGoal, avgVergazo, worldiePercent,
            total.Btts * inv * 100.0, total.Over25 * inv * 100.0,
            total.HomeCs * inv * 100.0, total.AwayCs * inv * 100.0,
            total.ControversyAverages(inv));
    }

    /// <summary>Detailed best-of-N: averages of the box-score events plus W/D/L and scorelines.</summary>
    public static MatchDetailedAggregate RunDetailed(
        Team home, Team away, SimulationParameters p, int iterations, Stage stage,
        bool neutralVenue, ProgressCounter? progress = null)
    {
        var rng = new Xoshiro256(p.Global.Seed ^ 0xD1B54A32D192ED03UL);
        long hw = 0, dr = 0, aw = 0;
        long hg = 0, ag = 0;
        long yellows = 0, reds = 0, pens = 0, corners = 0, injuries = 0, shots = 0;
        var grid = new Dictionary<(int, int), long>();

        for (int i = 0; i < iterations; i++)
        {
            var r = DetailedMatchSimulator.Simulate(home, away, stage, p, ref rng, neutralVenue);
            hg += r.HomeGoals;
            ag += r.AwayGoals;
            if (r.HomeGoals > r.AwayGoals) hw++;
            else if (r.HomeGoals < r.AwayGoals) aw++;
            else dr++;

            foreach (var c in r.Cards)
            {
                if (c.IsRed) reds++; else yellows++;
            }

            pens += r.Penalties.Count;
            injuries += r.Injuries.Count;
            corners += (r.HomeBox?.Corners ?? 0) + (r.AwayBox?.Corners ?? 0);
            shots += (r.HomeBox?.Shots ?? 0) + (r.AwayBox?.Shots ?? 0);

            var key = (r.HomeGoals, r.AwayGoals);
            grid[key] = grid.TryGetValue(key, out long cc) ? cc + 1 : 1;
            progress?.Add(1);
        }

        long n = Math.Max(1, (long)iterations);
        double inv = 1.0 / n;
        var scorelines = grid
            .OrderByDescending(kv => kv.Value)
            .Take(12)
            .Select(kv => new ScorelineFrequency(kv.Key.Item1, kv.Key.Item2, kv.Value, kv.Value * inv))
            .ToList();

        return new MatchDetailedAggregate(
            iterations, home.Code, home.Name, away.Code, away.Name, p.Label,
            hw * inv, dr * inv, aw * inv, hg * inv, ag * inv,
            yellows * inv, reds * inv, pens * inv, corners * inv, injuries * inv, shots * inv,
            scorelines);
    }

    /// <summary>Thread-local accumulator of detailed-match box-score totals for the aggregate runner.</summary>
    private sealed class MatchAcc
    {
        public long N, Hw, Dr, Aw, Hg, Ag;
        public long Btts, Over25, HomeCs, AwayCs; // both-teams-scored, over 2.5 goals, clean sheets
        public readonly long[] Grid = new long[(MaxScore + 1) * (MaxScore + 1)];

        // Home / away box-score running totals.
        private long _hShots, _hSot, _hCorners, _hFouls, _hOff, _hYel, _hRed, _hPen, _hInj, _hSav, _hThrow, _hGk;
        private long _aShots, _aSot, _aCorners, _aFouls, _aOff, _aYel, _aRed, _aPen, _aInj, _aSav, _aThrow, _aGk;
        private double _hPoss, _aPoss;

        // Mistakes & refereeing controversy running totals (both sides combined).
        private long _gkErrGoals, _defErrGoals, _unpErrors, _ctrlPens, _ctrlCards, _refMistakes;

        // Per-player goal tallies (excludes own goals) and the best (highest-vergazo) goal seen.
        public readonly Dictionary<string, long> ScorerGoals = new(StringComparer.Ordinal);
        public double VergazoSum;
        public long GoalCount;
        public long WorldieCount; // goals rated >= 9.0/10
        public double BestVergazo = -1;
        public string BestScorer = string.Empty;
        public string BestTeam = string.Empty;
        public GoalType BestType;
        public double BestDistance;
        public int BestMinute;

        public void Add(MatchResult r, string homeCode, string awayCode)
        {
            N++;
            Hg += r.HomeGoals;
            Ag += r.AwayGoals;
            // Count by the resolved winner so knockout ties decided by ET/penalties are credited as
            // a win (an "advance"), not a draw. For group games a draw has an empty WinnerCode.
            if (string.Equals(r.WinnerCode, homeCode, StringComparison.OrdinalIgnoreCase)) Hw++;
            else if (string.Equals(r.WinnerCode, awayCode, StringComparison.OrdinalIgnoreCase)) Aw++;
            else Dr++;

            if (r.HomeGoals > 0 && r.AwayGoals > 0) Btts++;
            if (r.HomeGoals + r.AwayGoals >= 3) Over25++;
            if (r.AwayGoals == 0) HomeCs++;
            if (r.HomeGoals == 0) AwayCs++;

            Grid[Math.Min(r.HomeGoals, MaxScore) * (MaxScore + 1) + Math.Min(r.AwayGoals, MaxScore)]++;

            var hb = r.HomeBox;
            var ab = r.AwayBox;
            if (hb is not null)
            {
                _hShots += hb.Shots; _hSot += hb.ShotsOnTarget; _hCorners += hb.Corners; _hFouls += hb.Fouls;
                _hOff += hb.Offsides; _hYel += hb.Yellows; _hRed += hb.Reds; _hSav += hb.Saves; _hPoss += hb.PossessionPercent;
                _hThrow += hb.ThrowIns; _hGk += hb.GoalKicks;
            }

            if (ab is not null)
            {
                _aShots += ab.Shots; _aSot += ab.ShotsOnTarget; _aCorners += ab.Corners; _aFouls += ab.Fouls;
                _aOff += ab.Offsides; _aYel += ab.Yellows; _aRed += ab.Reds; _aSav += ab.Saves; _aPoss += ab.PossessionPercent;
                _aThrow += ab.ThrowIns; _aGk += ab.GoalKicks;
            }

            foreach (var pen in r.Penalties)
            {
                if (string.Equals(pen.TeamCode, homeCode, StringComparison.OrdinalIgnoreCase)) _hPen++;
                else if (string.Equals(pen.TeamCode, awayCode, StringComparison.OrdinalIgnoreCase)) _aPen++;
            }

            foreach (var inj in r.Injuries)
            {
                if (string.Equals(inj.TeamCode, homeCode, StringComparison.OrdinalIgnoreCase)) _hInj++;
                else if (string.Equals(inj.TeamCode, awayCode, StringComparison.OrdinalIgnoreCase)) _aInj++;
            }

            foreach (var e in r.Errors)
            {
                if (!e.LedToGoal) _unpErrors++;
                else if (e.Kind == ErrorKind.GoalkeeperError) _gkErrGoals++;
                else _defErrGoals++;
            }

            foreach (var c in r.Cards)
            {
                if (c.Controversial) _ctrlCards++;
            }

            foreach (var pen in r.Penalties)
            {
                if (pen.Controversial) _ctrlPens++;
            }

            // Standalone bad calls only (the tagged wrong-pen/wrong-card ones are counted above).
            foreach (var bc in r.BadCalls)
            {
                if (bc.Type is BadCallType.PenaltyDenied or BadCallType.MissedCard
                    or BadCallType.GoalWronglyDisallowed or BadCallType.GoalWronglyAllowed)
                {
                    _refMistakes++;
                }
            }

            foreach (var goal in r.Goals)
            {
                VergazoSum += goal.Vergazo;
                GoalCount++;
                if (goal.Vergazo >= 9.0)
                {
                    WorldieCount++;
                }

                if (!goal.IsOwnGoal)
                {
                    ScorerGoals[goal.ScorerId] = ScorerGoals.GetValueOrDefault(goal.ScorerId) + 1;
                }

                if (goal.Vergazo > BestVergazo)
                {
                    BestVergazo = goal.Vergazo;
                    BestScorer = goal.ScorerName;
                    BestTeam = goal.TeamCode;
                    BestType = goal.Type;
                    BestDistance = goal.DistanceMeters;
                    BestMinute = goal.Minute;
                }
            }
        }

        public void Merge(MatchAcc o)
        {
            N += o.N; Hw += o.Hw; Dr += o.Dr; Aw += o.Aw; Hg += o.Hg; Ag += o.Ag;
            Btts += o.Btts; Over25 += o.Over25; HomeCs += o.HomeCs; AwayCs += o.AwayCs;
            for (int i = 0; i < Grid.Length; i++)
            {
                Grid[i] += o.Grid[i];
            }

            _hShots += o._hShots; _hSot += o._hSot; _hCorners += o._hCorners; _hFouls += o._hFouls; _hOff += o._hOff;
            _hYel += o._hYel; _hRed += o._hRed; _hPen += o._hPen; _hInj += o._hInj; _hSav += o._hSav; _hPoss += o._hPoss;
            _hThrow += o._hThrow; _hGk += o._hGk;
            _aShots += o._aShots; _aSot += o._aSot; _aCorners += o._aCorners; _aFouls += o._aFouls; _aOff += o._aOff;
            _aYel += o._aYel; _aRed += o._aRed; _aPen += o._aPen; _aInj += o._aInj; _aSav += o._aSav; _aPoss += o._aPoss;
            _aThrow += o._aThrow; _aGk += o._aGk;

            _gkErrGoals += o._gkErrGoals; _defErrGoals += o._defErrGoals; _unpErrors += o._unpErrors;
            _ctrlPens += o._ctrlPens; _ctrlCards += o._ctrlCards; _refMistakes += o._refMistakes;

            VergazoSum += o.VergazoSum;
            GoalCount += o.GoalCount;
            WorldieCount += o.WorldieCount;

            foreach (var kv in o.ScorerGoals)
            {
                ScorerGoals[kv.Key] = ScorerGoals.GetValueOrDefault(kv.Key) + kv.Value;
            }

            if (o.BestVergazo > BestVergazo)
            {
                BestVergazo = o.BestVergazo;
                BestScorer = o.BestScorer;
                BestTeam = o.BestTeam;
                BestType = o.BestType;
                BestDistance = o.BestDistance;
                BestMinute = o.BestMinute;
            }
        }

        public TeamMatchAverages HomeAverages(Team team, double inv) => new(
            team.Code, team.Name, Hg * inv, _hShots * inv, _hSot * inv, _hPoss * inv, _hCorners * inv,
            _hFouls * inv, _hOff * inv, _hYel * inv, _hRed * inv, _hPen * inv, _hInj * inv, _hSav * inv,
            _hThrow * inv, _hGk * inv);

        public TeamMatchAverages AwayAverages(Team team, double inv) => new(
            team.Code, team.Name, Ag * inv, _aShots * inv, _aSot * inv, _aPoss * inv, _aCorners * inv,
            _aFouls * inv, _aOff * inv, _aYel * inv, _aRed * inv, _aPen * inv, _aInj * inv, _aSav * inv,
            _aThrow * inv, _aGk * inv);

        public ControversyAverages ControversyAverages(double inv) => new(
            _gkErrGoals * inv, _defErrGoals * inv, _unpErrors * inv,
            _ctrlPens * inv, _ctrlCards * inv, _refMistakes * inv);
    }
}
