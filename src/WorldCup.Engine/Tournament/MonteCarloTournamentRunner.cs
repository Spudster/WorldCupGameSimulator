using System.Diagnostics;
using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Tournament.Fast;

namespace WorldCup.Engine.Tournament;

/// <summary>
/// Runs the fast-mode full-tournament Monte Carlo: parallelised across cores, each worker with its
/// own RNG and allocation-free scratch, aggregating per-team stage/title probabilities. Built to
/// scale to millions of tournaments and to report live throughput.
/// </summary>
public static class MonteCarloTournamentRunner
{
    public static TournamentMonteCarloReport Run(
        TournamentData data,
        SimulationParameters parameters,
        long iterations,
        bool includeThirdPlace = true,
        IReadOnlyList<PlayedResult>? lockedResults = null,
        ProgressCounter? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be positive.");
        }

        var model = FastTournamentModel.Build(data, parameters, lockedResults);
        var g = parameters.Global;

        // A fixed partition count (not the CPU count) keeps results identical across machines for a
        // given seed + N, while MaxDegreeOfParallelism still uses every available core.
        int partitionCount = (int)Math.Min(256, Math.Max(1, iterations));
        int maxParallel = Math.Max(1, Environment.ProcessorCount);
        var partials = new TournamentAggregator[partitionCount];
        var sw = Stopwatch.StartNew();

        Parallel.For(0, partitionCount, new ParallelOptions { MaxDegreeOfParallelism = maxParallel, CancellationToken = cancellationToken }, part =>
        {
            long start = iterations * part / partitionCount;
            long end = iterations * (part + 1) / partitionCount;
            long count = end - start;

            var agg = new TournamentAggregator(model.TeamCount);
            var scratch = new FastScratch(model.TeamCount);
            var rng = new Xoshiro256(g.Seed + (ulong)(part + 1) * 0x9E3779B97F4A7C15UL);

            const long reportEvery = 8192;
            long sinceReport = 0;
            for (long i = 0; i < count; i++)
            {
                FastTournamentSimulator.Run(model, g, scratch, agg, includeThirdPlace, ref rng);
                if (progress is not null && ++sinceReport >= reportEvery)
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

            partials[part] = agg;
        });

        sw.Stop();

        var merged = new TournamentAggregator(model.TeamCount);
        foreach (var part in partials)
        {
            merged.Merge(part);
        }

        return BuildReport(data, model, parameters, merged, includeThirdPlace, lockedResults is { Count: > 0 },
            sw.Elapsed.TotalSeconds);
    }

    private static TournamentMonteCarloReport BuildReport(
        TournamentData data, FastTournamentModel model, SimulationParameters parameters,
        TournamentAggregator agg, bool includeThirdPlace, bool currentState, double elapsedSeconds)
    {
        long n = agg.Tournaments;
        double inv = n > 0 ? 1.0 / n : 0;
        var teamsByCode = data.Teams.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

        var odds = new List<TeamTournamentOdds>(model.TeamCount);
        for (int i = 0; i < model.TeamCount; i++)
        {
            var team = teamsByCode[model.Codes[i]];
            odds.Add(new TeamTournamentOdds(
                team.Code, team.Name, team.Group,
                agg.Champion[i] * inv,
                agg.ReachedFinal[i] * inv,
                agg.ReachedSemi[i] * inv,
                agg.ReachedQuarter[i] * inv,
                agg.ReachedR16[i] * inv,
                agg.ReachedR32[i] * inv,
                agg.TopGroup[i] * inv,
                agg.GroupPointsSum[i] * inv));
        }

        odds = odds.OrderByDescending(o => o.Champion).ThenByDescending(o => o.ReachedFinal).ToList();

        var finals = agg.FinalMatchups
            .OrderByDescending(kv => kv.Value)
            .Take(12)
            .Select(kv =>
            {
                var a = teamsByCode[model.Codes[kv.Key.Item1]];
                var b = teamsByCode[model.Codes[kv.Key.Item2]];
                return new FinalMatchupOdds(a.Code, a.Name, b.Code, b.Name, kv.Value * inv);
            })
            .ToList();

        double sims = elapsedSeconds > 0 ? n / elapsedSeconds : 0;

        return new TournamentMonteCarloReport(
            n, parameters.Label, parameters.Global.Seed, Fidelity.Fast, includeThirdPlace, currentState,
            elapsedSeconds, sims, odds, finals);
    }
}
