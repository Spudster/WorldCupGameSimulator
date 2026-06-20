using WorldCup.Data.Models;

namespace WorldCup.Engine.Simulation;

/// <summary>How a match was decided.</summary>
public enum MatchMethod
{
    Regulation,
    ExtraTime,
    Penalties,
}

/// <summary>The fidelity at which a result was produced.</summary>
public enum Fidelity
{
    Fast,
    Detailed,
}

/// <summary>Kind of goal, used for box scores, the vergazo rating and "crazy stats".</summary>
public enum GoalType
{
    OpenPlay,
    Header,
    FreeKick,
    Penalty,
    LongRange,

    /// <summary>An overhead / bicycle kick — the most spectacular finish (and the only route to 10/10).</summary>
    BicycleKick,
    OwnGoal,
}

/// <summary>Injury severity.</summary>
public enum InjurySeverity
{
    Knock,
    Minor,
    Major,
}

/// <summary>Outcome of a single penalty kick (in open play, not a shootout).</summary>
public enum PenaltyOutcome
{
    Scored,
    Missed,
    Saved,
}

/// <summary>What kind of mistake caused something (a goal, or a survived scare). <see cref="None"/> = no error.</summary>
public enum ErrorKind
{
    None,

    /// <summary>An outfield mistake — a misplaced pass, slip, miscontrol or poor clearance.</summary>
    DefensiveError,

    /// <summary>A goalkeeper mistake — a fumble, flap at a cross, or a shot let through.</summary>
    GoalkeeperError,
}

/// <summary>A refereeing mistake ("bad call").</summary>
public enum BadCallType
{
    /// <summary>A penalty awarded that should not have been (a soft / incorrect call).</summary>
    WrongPenaltyAwarded,

    /// <summary>A clear penalty waved away.</summary>
    PenaltyDenied,

    /// <summary>A harsh or incorrect card shown.</summary>
    WrongCard,

    /// <summary>A card (often a red) that should have been shown but was not.</summary>
    MissedCard,

    /// <summary>A legitimate goal wrongly chalked off.</summary>
    GoalWronglyDisallowed,

    /// <summary>A goal that should have been ruled out (offside / a foul in the build-up) but stood.</summary>
    GoalWronglyAllowed,
}

/// <summary>
/// The minimal, allocation-light result of a fast-mode match. For group matches the method is
/// always <see cref="MatchMethod.Regulation"/> and <see cref="WinnerIsHome"/> may be null (draw).
/// For knockout matches the goals include extra time and the winner is always resolved.
/// </summary>
public readonly record struct FastMatchResult(
    int HomeGoals,
    int AwayGoals,
    MatchMethod Method,
    bool? WinnerIsHome,
    int HomePens,
    int AwayPens)
{
    public bool IsDraw => WinnerIsHome is null;
}

/// <summary>
/// A goal in detailed mode, attributed to players.
/// <para>
/// <see cref="Vergazo"/> is the 1–10 "vergazo" spectacularity rating (how much of a screamer the
/// goal was), driven by style, shot distance, defenders beaten in the build-up and execution
/// quality. Only a top-class long-range bicycle kick reaches 10/10; own goals are always ≤ 3.
/// </para>
/// </summary>
public sealed record GoalEvent(
    int Minute,
    string TeamCode,
    string ScorerId,
    string ScorerName,
    string? AssistId,
    string? AssistName,
    GoalType Type,
    double DistanceMeters,
    bool IsPenalty,
    bool IsOwnGoal,
    int DefendersPassed = 0,
    double Vergazo = 0,

    /// <summary>Set when the goal was gifted by a defensive or goalkeeper error by the conceding side.</summary>
    ErrorKind CausedByError = ErrorKind.None,

    /// <summary>How the scorer / team celebrated (or, for an own goal, the despair).</summary>
    string Celebration = "");

/// <summary>A card shown in detailed mode, with the specific offence the referee booked it for.</summary>
public sealed record CardEvent(
    int Minute,
    string TeamCode,
    string PlayerId,
    string PlayerName,
    bool IsRed,
    bool IsSecondYellow,

    /// <summary>True when the referee got this card wrong (harsh / incorrect).</summary>
    bool Controversial = false,

    /// <summary>What the player did to earn it (e.g. "a late challenge", "violent conduct").</summary>
    string Reason = "");

/// <summary>An in-play penalty (not a shootout kick).</summary>
public sealed record PenaltyEvent(
    int Minute,
    string TeamCode,
    string TakerId,
    string TakerName,
    string KeeperId,
    string KeeperName,
    PenaltyOutcome Outcome,

    /// <summary>True when the penalty was a wrong call (soft / incorrect decision).</summary>
    bool Controversial = false);

/// <summary>
/// A notable goalkeeper save in detailed mode, with a 1–10 spectacularity rating (the keeper's
/// answer to the vergazo). A flying top-corner tip or a point-blank reflex stop rates highest;
/// <see cref="IsAmazing"/> flags the worldie saves.
/// </summary>
public sealed record SaveEvent(
    int Minute,
    string TeamCode,
    string KeeperId,
    string KeeperName,
    double Rating,
    bool IsAmazing,
    double ShotDistanceMeters);

/// <summary>A substitution in detailed mode (tactical or forced by injury).</summary>
public sealed record SubstitutionEvent(
    int Minute,
    string TeamCode,
    string OffId,
    string OffName,
    string OnId,
    string OnName,
    bool Injury);

/// <summary>
/// A player or goalkeeper mistake. When <see cref="LedToGoal"/> is true the error gifted the
/// opponent a goal; otherwise it was a scare the team got away with (a save, a miss, a clearance).
/// </summary>
public sealed record PlayerErrorEvent(
    int Minute,
    string TeamCode,
    string PlayerId,
    string PlayerName,
    ErrorKind Kind,
    bool LedToGoal,
    string Description);

/// <summary>
/// A refereeing mistake. <see cref="ForCode"/> is the side that benefited from the call and
/// <see cref="AgainstCode"/> the side wronged by it (either may be empty). <see cref="VarChecked"/>
/// flags whether VAR looked at it and still got it wrong / let it stand.
/// </summary>
public sealed record BadCallEvent(
    int Minute,
    BadCallType Type,
    string ForCode,
    string AgainstCode,
    bool VarChecked,
    string PlayerName,
    string Description);

/// <summary>
/// An injury in detailed mode, diagnosed specifically: which body part, the exact diagnosis, and the
/// expected lay-off in days (0 = shook it off and played on).
/// </summary>
public sealed record InjuryEvent(
    int Minute,
    string TeamCode,
    string PlayerId,
    string PlayerName,
    InjurySeverity Severity,
    bool CouldBeReplaced,
    string BodyPart = "",
    string Diagnosis = "",
    int RecoveryDays = 0);

/// <summary>
/// Pre-match prediction vs. actual result — how surprising the outcome was (detailed mode only).
/// <see cref="MiracleRating"/> is the 1–10 "shock" rating: ~1 for the expected result, 10 for a
/// genuine giant-killing. The pre-match probabilities are the model's odds before kick-off.
/// </summary>
public sealed record UpsetInfo(
    double PreMatchHomeWin,
    double PreMatchDraw,
    double PreMatchAwayWin,
    double ResultProbability,
    double MiracleRating);

/// <summary>
/// A "miracle" — the rare in-game event where an underdog catches fire and produces a genuine upset.
/// The strength boost is applied to that side for the match; this records that it happened and who it
/// lifted (the <see cref="UpsetInfo.MiracleRating"/> then measures how shocking the final result was).
/// </summary>
public sealed record MiracleEvent(int Minute, string TeamCode, string TeamName, string Description);

/// <summary>One kick of a penalty shootout, in taking order.</summary>
public sealed record ShootoutKick(int Number, bool IsHome, string TeamCode, string Player, bool Scored);

/// <summary>A hot-weather hydration / cooling break (FIFA mandates them above a heat threshold).</summary>
public sealed record CoolingBreak(int Minute, int TemperatureC);

/// <summary>How serious an on-field flashpoint got.</summary>
public enum ConfrontationLevel
{
    /// <summary>Handbags — a shove and a sneer that fizzles out.</summary>
    Handbags,

    /// <summary>A nose-to-nose face-off, words exchanged.</summary>
    FaceOff,

    /// <summary>A real shoving match with several players piling in.</summary>
    Scuffle,

    /// <summary>An ugly, full-scale brawl.</summary>
    Brawl,
}

/// <summary>An on-field confrontation between players (and possibly the benches). <see cref="Cause"/> is
/// the specific thing that sparked it (a red card, a contentious call, a provocative goal…).</summary>
public sealed record Confrontation(
    int Minute,
    ConfrontationLevel Level,
    bool BenchInvolved,
    string Cause,
    string Description);

/// <summary>The match-day weather. Descriptive flavour only — it colours the commentary and the
/// conditions readout without altering the calibrated event stream.</summary>
public enum WeatherKind
{
    Clear,
    Sunny,
    Overcast,
    Breezy,
    Windy,
    LightRain,
    HeavyRain,
    Humid,
    Cold,
}

/// <summary>Match-day conditions: a weather kind plus a one-line descriptive note.</summary>
public sealed record Weather(WeatherKind Kind, string Note);

/// <summary>How a chance that did NOT go in came to nothing — woodwork, a goal-line clearance, a great
/// block, or an agonising miss.</summary>
public enum NearMissKind
{
    HitThePost,
    HitTheBar,
    OffTheLine,
    GreatBlock,
    BlazedOver,
    JustWide,
    HeaderOff,
    RattledTheWoodwork,
}

/// <summary>A near-miss / woodwork moment (detailed mode). Generated after the final whistle, so it is
/// pure flavour and never changes the score.</summary>
public sealed record NearMiss(int Minute, string TeamCode, string PlayerName, NearMissKind Kind, string Description);

/// <summary>What a VAR check looked at.</summary>
public enum VarKind
{
    OffsideOnGoal,
    HandballOnGoal,
    PenaltyAppeal,
    PossibleRed,
    GoalLine,
}

/// <summary>A VAR check on an existing decision. Attribution-only: it narrates the drama of the review
/// but never adds or removes a goal/card/penalty, so the calibrated totals are untouched.
/// <see cref="DecisionStands"/> is true when the original on-field decision is upheld.</summary>
public sealed record VarCheck(int Minute, string TeamCode, VarKind Kind, bool DecisionStands, string Description);

/// <summary>Per-team box-score totals for a single match (detailed mode).</summary>
public sealed record TeamBoxScore(
    string TeamCode,
    int Goals,
    int Shots,
    int ShotsOnTarget,
    double PossessionPercent,
    int Corners,
    int Fouls,
    int Offsides,
    int Saves,
    int Yellows,
    int Reds,
    int ThrowIns = 0,
    int GoalKicks = 0);

/// <summary>
/// The complete result of a single match. In fast mode only the score-level fields are populated;
/// in detailed mode the event lists and box scores are filled too.
/// </summary>
public sealed record MatchResult
{
    public required Fidelity Fidelity { get; init; }
    public required Stage Stage { get; init; }
    public required string HomeCode { get; init; }
    public required string AwayCode { get; init; }
    public required string HomeName { get; init; }
    public required string AwayName { get; init; }
    public required int HomeGoals { get; init; }
    public required int AwayGoals { get; init; }
    public required MatchMethod Method { get; init; }

    /// <summary>Winner team code; empty for a (group-stage) draw.</summary>
    public string WinnerCode { get; init; } = string.Empty;

    public int HomePens { get; init; }
    public int AwayPens { get; init; }

    /// <summary>Number of shootout rounds (0 when the match did not go to penalties).</summary>
    public int ShootoutRounds { get; init; }

    /// <summary>The kick-by-kick penalty shootout, in order (empty unless the match went to penalties).</summary>
    public IReadOnlyList<ShootoutKick> ShootoutKicks { get; init; } = Array.Empty<ShootoutKick>();

    /// <summary>Referee's added time signalled at the end of the first half, in minutes (0 in fast mode).</summary>
    public int FirstHalfStoppage { get; init; }

    /// <summary>Referee's added time signalled at the end of the second half, in minutes (0 in fast mode).</summary>
    public int SecondHalfStoppage { get; init; }

    /// <summary>Match-day temperature in °C (0 if not modelled, e.g. fast mode).</summary>
    public int TemperatureC { get; init; }

    /// <summary>Hydration / cooling breaks taken in hot conditions (detailed mode).</summary>
    public IReadOnlyList<CoolingBreak> CoolingBreaks { get; init; } = Array.Empty<CoolingBreak>();

    /// <summary>On-field flashpoints / confrontations during the match (detailed mode).</summary>
    public IReadOnlyList<Confrontation> Confrontations { get; init; } = Array.Empty<Confrontation>();

    /// <summary>Match-day weather (detailed mode) — descriptive flavour, no effect on the event stream.</summary>
    public Weather? Weather { get; init; }

    /// <summary>Near-misses and woodwork — chances that rattled the frame, were cleared off the line or
    /// flashed wide (detailed mode). Pure flavour; never affects the score.</summary>
    public IReadOnlyList<NearMiss> NearMisses { get; init; } = Array.Empty<NearMiss>();

    /// <summary>VAR checks / review drama on existing decisions (detailed mode) — attribution-only.</summary>
    public IReadOnlyList<VarCheck> VarChecks { get; init; } = Array.Empty<VarCheck>();

    /// <summary>True when this is a real, already-played result locked in "current state" mode.</summary>
    public bool IsLocked { get; init; }

    // Detailed-mode payload (empty in fast mode).
    public IReadOnlyList<GoalEvent> Goals { get; init; } = Array.Empty<GoalEvent>();
    public IReadOnlyList<CardEvent> Cards { get; init; } = Array.Empty<CardEvent>();
    public IReadOnlyList<PenaltyEvent> Penalties { get; init; } = Array.Empty<PenaltyEvent>();
    public IReadOnlyList<InjuryEvent> Injuries { get; init; } = Array.Empty<InjuryEvent>();

    /// <summary>Notable goalkeeper saves with spectacularity ratings (detailed mode).</summary>
    public IReadOnlyList<SaveEvent> SaveEvents { get; init; } = Array.Empty<SaveEvent>();

    /// <summary>Substitutions (tactical and injury-forced) made during the match (detailed mode).</summary>
    public IReadOnlyList<SubstitutionEvent> Substitutions { get; init; } = Array.Empty<SubstitutionEvent>();

    /// <summary>Player / goalkeeper errors during the match — those that led to a goal and those that didn't.</summary>
    public IReadOnlyList<PlayerErrorEvent> Errors { get; init; } = Array.Empty<PlayerErrorEvent>();

    /// <summary>Refereeing mistakes ("bad calls") during the match (detailed mode).</summary>
    public IReadOnlyList<BadCallEvent> BadCalls { get; init; } = Array.Empty<BadCallEvent>();
    public TeamBoxScore? HomeBox { get; init; }
    public TeamBoxScore? AwayBox { get; init; }

    /// <summary>Pre-match odds vs. the actual result and the 1–10 miracle rating (detailed mode).</summary>
    public UpsetInfo? Upset { get; init; }

    /// <summary>The "miracle" event that lifted an underdog and drove a giant-killing, if one fired (detailed mode).</summary>
    public MiracleEvent? Miracle { get; init; }

    /// <summary>Player ids who appeared for the home team (starters + subs).</summary>
    public IReadOnlyList<string> HomeLineup { get; init; } = Array.Empty<string>();

    /// <summary>Player ids who appeared for the away team (starters + subs).</summary>
    public IReadOnlyList<string> AwayLineup { get; init; } = Array.Empty<string>();

    /// <summary>Minutes played per player id this match (drives leaderboards/MVP).</summary>
    public IReadOnlyDictionary<string, int> Minutes { get; init; } =
        new Dictionary<string, int>();

    public bool IsDraw => WinnerCode.Length == 0;
    public bool WentToPens => Method == MatchMethod.Penalties;
}
