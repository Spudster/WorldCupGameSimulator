using WorldCup.Data.Models;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Stats;

/// <summary>Golden Boot row (top scorers); also carries assists/minutes for tiebreaks.</summary>
public sealed record ScorerRow(string PlayerId, string Name, string TeamCode, int Goals, int Assists, int Minutes);

/// <summary>Most-assists row.</summary>
public sealed record AssistRow(string PlayerId, string Name, string TeamCode, int Assists, int Goals);

/// <summary>MVP / Golden Ball row with the transparent composite score and its components.</summary>
public sealed record MvpRow(
    string PlayerId, string Name, string TeamCode, double Score,
    int Goals, int Assists, int CleanSheets, int Minutes, string FurthestStage);

/// <summary>Golden Glove row (best goalkeeper).</summary>
public sealed record GoalkeeperRow(
    string PlayerId, string Name, string TeamCode, int CleanSheets, int Saves, int AmazingSaves, int GoalsConceded, int Minutes);

/// <summary>Discipline row (cards).</summary>
public sealed record DisciplineRow(string PlayerId, string Name, string TeamCode, int Yellows, int Reds);

/// <summary>Per-team aggregate stats.</summary>
public sealed record TeamStatRow(
    string TeamCode, string Name,
    int GoalsFor, int GoalsAgainst, int CleanSheets,
    int Shots, int ShotsOnTarget, int Corners,
    int Yellows, int Reds, double PossessionAvg);

/// <summary>Penalty taker row (spot-kick record).</summary>
public sealed record PenaltyTakerRow(string PlayerId, string Name, string TeamCode, int Scored, int Missed);

/// <summary>How many goals of a given type were scored across the run.</summary>
public sealed record GoalTypeRow(string Type, int Count, double Percent);

/// <summary>One "crazy stat" / record extreme.</summary>
public sealed record RecordEntry(string Category, string Description, double Value);

/// <summary>An injury entry for the tournament-wide injury list, with the specific diagnosis and lay-off.</summary>
public sealed record InjuryItem(
    string PlayerName, string TeamCode, int Minute, InjurySeverity Severity, bool Replaced,
    string BodyPart = "", string Diagnosis = "", int RecoveryDays = 0);

/// <summary>How often a player won a per-tournament award across a detailed Monte Carlo run.</summary>
public sealed record AwardFrequencyRow(string PlayerId, string Name, string TeamCode, long Wins, double Frequency, double AverageTally);

/// <summary>The full detailed-mode statistics report.</summary>
public sealed record StatsReport(
    int Tournaments,
    IReadOnlyList<ScorerRow> GoldenBoot,
    IReadOnlyList<AssistRow> TopAssists,
    IReadOnlyList<MvpRow> Mvp,
    IReadOnlyList<GoalkeeperRow> GoldenGlove,
    IReadOnlyList<DisciplineRow> MostYellows,
    IReadOnlyList<DisciplineRow> MostReds,
    IReadOnlyList<TeamStatRow> Teams,
    IReadOnlyList<PenaltyTakerRow> PenaltyTakers,
    IReadOnlyList<GoalTypeRow> GoalsByType,
    IReadOnlyList<RecordEntry> CrazyStats,
    IReadOnlyList<InjuryItem> Injuries,
    int TotalInjuries,
    IReadOnlyList<AwardFrequencyRow> GoldenBootFrequency,
    IReadOnlyList<AwardFrequencyRow> MvpFrequency,
    IReadOnlyList<AwardFrequencyRow> GoldenGloveFrequency);
