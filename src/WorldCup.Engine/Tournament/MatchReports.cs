using WorldCup.Data.Models;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>A scoreline and how often it occurred.</summary>
public sealed record ScorelineFrequency(int HomeGoals, int AwayGoals, long Count, double Probability);

/// <summary>Aggregated result of a fast-mode single-matchup Monte Carlo (regulation W/D/L).</summary>
public sealed record MatchMonteCarloReport(
    long Iterations,
    string HomeCode, string HomeName,
    string AwayCode, string AwayName,
    string ParameterLabel,
    ulong Seed,
    double HomeWin,
    double Draw,
    double AwayWin,
    double AvgHomeGoals,
    double AvgAwayGoals,
    double ElapsedSeconds,
    double SimsPerSecond,
    IReadOnlyList<ScorelineFrequency> TopScorelines);

/// <summary>An aggregated scorer line: how often a player scores across the Monte Carlo run.</summary>
public sealed record ScorerLine(string PlayerId, string Name, string TeamCode, double GoalsPerMatch, long TotalGoals);

/// <summary>The Monte Carlo forecast for one remaining scheduled fixture, with its schedule metadata.</summary>
public sealed record ScheduledGameForecast(
    char Group, int Matchday, DateTime KickoffUtc, bool Neutral, MatchMonteCarloReport Report)
{
    /// <summary>1 = home favoured, 0 = draw most likely, -1 = away favoured.</summary>
    public int Favourite =>
        Report.HomeWin >= Report.Draw && Report.HomeWin >= Report.AwayWin ? 1
        : Report.AwayWin >= Report.Draw && Report.AwayWin >= Report.HomeWin ? -1 : 0;

    /// <summary>The single most-likely exact scoreline, or null if none was recorded.</summary>
    public ScorelineFrequency? ModalScore => Report.TopScorelines.Count > 0 ? Report.TopScorelines[0] : null;

    /// <summary>
    /// The most likely exact scoreline that is <em>consistent with the favoured outcome</em> — i.e. the
    /// predicted result's most probable score. A favourite's win probability is spread across many
    /// winning scorelines, so the single most-common score (<see cref="ModalScore"/>) is often a draw or
    /// 1–0 even for a clear favourite; this picks the most probable scoreline whose winner matches the
    /// forecast, which is what people expect from a "predicted score". Falls back to the modal score.
    /// </summary>
    public ScorelineFrequency? PredictedScore
    {
        get
        {
            foreach (var s in Report.TopScorelines)
            {
                int sign = s.HomeGoals > s.AwayGoals ? 1 : s.HomeGoals < s.AwayGoals ? -1 : 0;
                if (sign == Favourite)
                {
                    return s;
                }
            }

            return ModalScore;
        }
    }
}

/// <summary>A batch forecast of every remaining scheduled fixture, each simulated the same number of times.</summary>
public sealed record ScheduledForecastReport(
    long IterationsPerGame,
    string ParameterLabel,
    ulong Seed,
    double ElapsedSeconds,
    IReadOnlyList<ScheduledGameForecast> Games)
{
    /// <summary>Recent-form strength adjustments folded into this batch (empty when form was off or no
    /// games had been played yet) — surfaced so the reader can see <em>why</em> a forecast moved.</summary>
    public IReadOnlyList<FormAdjustment> FormAdjustments { get; init; } = Array.Empty<FormAdjustment>();
}

/// <summary>The Monte Carlo forecast for one scheduled knockout fixture (decisive: ET/penalties fold into the winner).</summary>
public sealed record KnockoutGameForecast(
    int MatchId, Stage Stage, string RoundLabel, string Label, DateTime KickoffUtc, bool Projected, MatchAggregateReport Report)
{
    /// <summary>1 = top side favoured to advance, -1 = bottom side. (Knockouts always produce a winner.)</summary>
    public int Favourite => Report.HomeWin >= Report.AwayWin ? 1 : -1;

    /// <summary>Probability the favoured side advances.</summary>
    public double AdvanceProbability => Math.Max(Report.HomeWin, Report.AwayWin);

    /// <summary>The most likely scoreline consistent with the favoured side advancing (falls back to the modal scoreline).</summary>
    public ScorelineFrequency? PredictedScore
    {
        get
        {
            foreach (var s in Report.TopScorelines)
            {
                int sign = s.HomeGoals > s.AwayGoals ? 1 : s.HomeGoals < s.AwayGoals ? -1 : 0;
                if (sign == Favourite)
                {
                    return s;
                }
            }

            return Report.TopScorelines.Count > 0 ? Report.TopScorelines[0] : null;
        }
    }
}

/// <summary>A batch forecast of every scheduled knockout fixture in scope, each simulated the same number of times.</summary>
public sealed record KnockoutForecastReport(
    long IterationsPerGame,
    string ParameterLabel,
    ulong Seed,
    double ElapsedSeconds,
    string ScopeLabel,
    bool AnyProjected,
    IReadOnlyList<KnockoutGameForecast> Games);

/// <summary>The single best (highest-vergazo) goal seen across all simulated runs of a matchup.</summary>
public sealed record BestGoalInfo(
    string ScorerName, string TeamCode, double Vergazo, GoalType Type, double DistanceMeters, int Minute);

/// <summary>Average per-match mistake / refereeing-controversy figures across a Monte Carlo run (both sides combined).</summary>
public sealed record ControversyAverages(
    double KeeperErrorGoals,
    double DefensiveErrorGoals,
    double UnpunishedErrors,
    double ControversialPenalties,
    double ControversialCards,
    double RefereeMistakes)
{
    /// <summary>Goals per game gifted by an error (keeper + defensive).</summary>
    public double ErrorGoals => KeeperErrorGoals + DefensiveErrorGoals;
}

/// <summary>Average per-match box-score figures for one team across a Monte Carlo run.</summary>
public sealed record TeamMatchAverages(
    string Code,
    string Name,
    double Goals,
    double Shots,
    double ShotsOnTarget,
    double Possession,
    double Corners,
    double Fouls,
    double Offsides,
    double Yellows,
    double Reds,
    double Penalties,
    double Injuries,
    double Saves,
    double ThrowIns = 0,
    double GoalKicks = 0);

/// <summary>
/// The full single-matchup Monte Carlo report: most-probable score + W/D/L + scoreline frequencies,
/// PLUS averaged box-score stats (cards, penalties, corners, …) for each side. Produced by running
/// the detailed simulator N times (parallelised) and aggregating.
/// </summary>
public sealed record MatchAggregateReport(
    long Iterations,
    string HomeCode, string HomeName,
    string AwayCode, string AwayName,
    string ParameterLabel,
    ulong Seed,
    double HomeWin,
    double Draw,
    double AwayWin,
    double AvgHomeGoals,
    double AvgAwayGoals,
    double ElapsedSeconds,
    double SimsPerSecond,
    IReadOnlyList<ScorelineFrequency> TopScorelines,
    TeamMatchAverages Home,
    TeamMatchAverages Away,
    IReadOnlyList<ScorerLine> TopScorers,
    BestGoalInfo? BestGoal,
    double AverageVergazo,
    double WorldiePercent,
    double BttsPercent,
    double Over25Percent,
    double HomeCleanSheetPercent,
    double AwayCleanSheetPercent,
    ControversyAverages Controversy);

/// <summary>Aggregated event averages from a detailed-mode best-of-N matchup.</summary>
public sealed record MatchDetailedAggregate(
    long Iterations,
    string HomeCode, string HomeName,
    string AwayCode, string AwayName,
    string ParameterLabel,
    double HomeWin,
    double Draw,
    double AwayWin,
    double AvgHomeGoals,
    double AvgAwayGoals,
    double YellowsPerGame,
    double RedsPerGame,
    double PenaltiesPerGame,
    double CornersPerGame,
    double InjuriesPerGame,
    double ShotsPerGame,
    IReadOnlyList<ScorelineFrequency> TopScorelines);
