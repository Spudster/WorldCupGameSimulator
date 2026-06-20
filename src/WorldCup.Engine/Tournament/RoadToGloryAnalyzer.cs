using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>The chance a particular opponent is faced at a given knockout round (conditional on reaching it).</summary>
/// <param name="Code">The opponent's 3-letter code.</param>
/// <param name="Name">The opponent's display name.</param>
/// <param name="Probability">The probability of facing this opponent, conditional on reaching the round.</param>
public sealed record OpponentOdds(string Code, string Name, double Probability);

/// <summary>One knockout round on a team's road to glory: how often it is reached and won, plus the likeliest opponents.</summary>
/// <param name="Stage">The knockout stage this entry describes.</param>
/// <param name="StageName">The human-readable stage name.</param>
/// <param name="ReachProbability">The probability the team reaches (plays a match in) this round.</param>
/// <param name="WinProbability">The probability the team wins its match in this round.</param>
/// <param name="LikelyOpponents">The most likely opponents at this round (top 5), conditional on reaching it.</param>
public sealed record RoadStage(
    Stage Stage, string StageName, double ReachProbability, double WinProbability,
    IReadOnlyList<OpponentOdds> LikelyOpponents);

/// <summary>A team's "road to glory": round-by-round reach/win odds, likely opponents, and title odds.</summary>
/// <param name="TeamCode">The analysed team's 3-letter code.</param>
/// <param name="TeamName">The analysed team's display name.</param>
/// <param name="Iterations">The number of full-tournament simulations run.</param>
/// <param name="ChampionProbability">The probability the team is crowned champion.</param>
/// <param name="FinalProbability">The probability the team reaches the final.</param>
/// <param name="Stages">The per-round breakdown, shallow → deep (Round of 32 → Final).</param>
public sealed record RoadToGloryReport(
    string TeamCode, string TeamName, long Iterations,
    double ChampionProbability, double FinalProbability,
    IReadOnlyList<RoadStage> Stages);

/// <summary>
/// Runs many full-tournament simulations and, for one chosen team, reports how often it reaches each
/// knockout round, its most likely opponent at each round, and its odds of lifting the trophy.
/// </summary>
public static class RoadToGloryAnalyzer
{
    /// <summary>The knockout rounds reported, in order from shallow to deep.</summary>
    private static readonly Stage[] ReportableStages =
    {
        Stage.RoundOf32, Stage.RoundOf16, Stage.QuarterFinal, Stage.SemiFinal, Stage.Final,
    };

    /// <summary>
    /// Simulates <paramref name="iterations"/> tournaments and builds a road-to-glory report for
    /// <paramref name="teamCode"/>.
    /// </summary>
    /// <param name="data">The tournament data (teams, bracket, schedule).</param>
    /// <param name="teamCode">The 3-letter code of the team to analyse.</param>
    /// <param name="p">The simulation parameters.</param>
    /// <param name="locked">Already-played results to lock, or <see langword="null"/> for a clean run.</param>
    /// <param name="iterations">The number of full-tournament simulations to run.</param>
    /// <param name="seed">The seed for the run's random number generator.</param>
    /// <param name="includeThirdPlacePlayoff">Whether to simulate the third-place playoff.</param>
    /// <param name="progress">An optional progress counter, advanced by one per simulated tournament.</param>
    /// <returns>The completed road-to-glory report for the chosen team.</returns>
    public static RoadToGloryReport Analyze(
        TournamentData data, string teamCode, SimulationParameters p,
        IReadOnlyList<PlayedResult>? locked, long iterations, ulong seed, bool includeThirdPlacePlayoff = true,
        ProgressCounter? progress = null)
    {
        long denominator = Math.Max(1, iterations);

        var reachCount = new Dictionary<Stage, long>();
        var winCount = new Dictionary<Stage, long>();
        var opponentCounts = new Dictionary<Stage, Dictionary<string, long>>();
        foreach (var stage in ReportableStages)
        {
            reachCount[stage] = 0;
            winCount[stage] = 0;
            opponentCounts[stage] = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }

        long championCount = 0;

        var sim = new TournamentSimulator(data, p, includeThirdPlacePlayoff, locked);
        var rng = new Xoshiro256(seed);

        long runs = Math.Max(0, iterations);
        for (long i = 0; i < runs; i++)
        {
            var result = sim.Simulate(Fidelity.Fast, ref rng);

            foreach (var stage in ReportableStages)
            {
                foreach (var ko in result.KnockoutResults)
                {
                    if (ko.Stage != stage)
                    {
                        continue;
                    }

                    bool isHome = string.Equals(ko.Result.HomeCode, teamCode, StringComparison.OrdinalIgnoreCase);
                    bool isAway = string.Equals(ko.Result.AwayCode, teamCode, StringComparison.OrdinalIgnoreCase);
                    if (!isHome && !isAway)
                    {
                        continue;
                    }

                    reachCount[stage]++;
                    string opponent = isHome ? ko.Result.AwayCode : ko.Result.HomeCode;
                    var counts = opponentCounts[stage];
                    counts[opponent] = counts.GetValueOrDefault(opponent) + 1;

                    if (string.Equals(ko.Result.WinnerCode, teamCode, StringComparison.OrdinalIgnoreCase))
                    {
                        winCount[stage]++;
                    }

                    break;
                }
            }

            if (string.Equals(result.ChampionCode, teamCode, StringComparison.OrdinalIgnoreCase))
            {
                championCount++;
            }

            progress?.Add(1);
        }

        var stages = new List<RoadStage>(ReportableStages.Length);
        double finalReachProbability = 0;
        foreach (var stage in ReportableStages)
        {
            long reached = reachCount[stage];
            double reachProbability = reached / (double)denominator;
            double winProbability = winCount[stage] / (double)denominator;

            IReadOnlyList<OpponentOdds> opponents;
            if (reached == 0)
            {
                opponents = Array.Empty<OpponentOdds>();
            }
            else
            {
                opponents = opponentCounts[stage]
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .Select(kvp => new OpponentOdds(kvp.Key, data.Team(kvp.Key).Name, kvp.Value / (double)reached))
                    .ToList();
            }

            stages.Add(new RoadStage(stage, Stages.DisplayName(stage), reachProbability, winProbability, opponents));

            if (stage == Stage.Final)
            {
                finalReachProbability = reachProbability;
            }
        }

        double championProbability = championCount / (double)denominator;

        return new RoadToGloryReport(
            teamCode,
            data.Team(teamCode).Name,
            iterations,
            championProbability,
            finalReachProbability,
            stages);
    }
}
