namespace WorldCup.Engine.Tournament;

/// <summary>One row of a (possibly partial) group table, as it stands right now.</summary>
public sealed record GroupStandingRow(
    int Rank, string Code, string Name, int Played, int Won, int Drawn, int Lost,
    int GoalsFor, int GoalsAgainst, int Points, bool IsSelected)
{
    public int GoalDifference => GoalsFor - GoalsAgainst;
}

/// <summary>Pre-match win / draw / loss odds for one group fixture that is still to be played.</summary>
public sealed record RemainingFixtureOdds(
    string HomeCode, string HomeName, string AwayCode, string AwayName,
    double HomeWin, double Draw, double AwayWin, bool InvolvesSelected);

/// <summary>One match's chosen result inside an enumerated scenario (a single "what if").</summary>
/// <param name="Sign">+1 = home win, 0 = draw, -1 = away win.</param>
public sealed record ScenarioOutcome(
    string HomeCode, string HomeName, string AwayCode, string AwayName, int Sign, string Description);

/// <summary>
/// A fully-specified combination of every remaining group result, and where it leaves the selected
/// team. When the team is tied on points the finishing place is a range (<see cref="BestRank"/>..
/// <see cref="WorstRank"/>) settled by the goal-difference / head-to-head tiebreakers.
/// </summary>
public sealed record GroupPathScenario(
    IReadOnlyList<ScenarioOutcome> Outcomes,
    double Probability,
    int BestRank,
    int WorstRank,
    bool TiebreakerDependent);

/// <summary>
/// How the team fares conditional on a given points haul from its own remaining game(s) — the
/// "what do we need from our last match" view. Shares are over the remaining matches of the others.
/// </summary>
public sealed record OwnResultBranch(
    string Label,
    int PointsGained,
    double Probability,
    double WinGroup,
    double Advance,
    double Eliminated,
    string Verdict);

/// <summary>
/// The complete "path to victory / path to defeat" picture for one team in one group: where it sits
/// now, the odds of every remaining group fixture, the probability of each finishing tier, and the
/// concrete result-combinations that win the group or get the team knocked out.
/// </summary>
public sealed record GroupPathAnalysis(
    char Group,
    string TeamCode,
    string TeamName,
    string ParameterLabel,
    ulong Seed,
    long Iterations,
    IReadOnlyList<GroupStandingRow> Standings,
    IReadOnlyList<RemainingFixtureOdds> RemainingFixtures,
    int OwnRemaining,
    bool GroupComplete,
    int FinalRankIfComplete,
    // Headline finishing-tier probabilities (Monte Carlo, full tiebreakers).
    double WinGroup,
    double RunnerUp,
    double ThirdPlace,
    double Eliminated,
    double AdvanceDirect,
    // What is already mathematically settled.
    bool ClinchedWinGroup,
    bool ClinchedAdvance,
    bool CannotWinGroup,
    bool CannotAdvance,
    bool CannotFinishLast,
    IReadOnlyList<OwnResultBranch> OwnResultBranches,
    IReadOnlyList<GroupPathScenario> VictoryScenarios,
    IReadOnlyList<GroupPathScenario> DefeatScenarios,
    double VictoryMass,
    double DefeatMass,
    int TotalCombinations)
{
    /// <summary>P(finish 3rd or 4th) — failing to qualify directly. Third may still sneak through as a best-third.</summary>
    public double FailDirect => ThirdPlace + Eliminated;
}

/// <summary>One team's at-a-glance outlook within a whole-group analysis.</summary>
public sealed record GroupTeamOutlook(
    GroupStandingRow Standing,
    double WinGroup,
    double RunnerUp,
    double ThirdPlace,
    double Eliminated,
    double AdvanceDirect,
    bool ClinchedWinGroup,
    bool ClinchedAdvance,
    bool CannotAdvance,
    bool CannotFinishLast,
    string Status);

/// <summary>
/// The whole-group "where does everyone stand" picture, computed from a single shared Monte Carlo so
/// every team's finishing probabilities are mutually consistent (the win-group shares sum to 1, etc.).
/// </summary>
public sealed record GroupOutlook(
    char Group,
    string ParameterLabel,
    ulong Seed,
    long Iterations,
    bool GroupComplete,
    IReadOnlyList<RemainingFixtureOdds> RemainingFixtures,
    IReadOnlyList<GroupTeamOutlook> Teams);

/// <summary>A still-to-play fixture, labelled for the qualification-scenarios grid.</summary>
public sealed record RemainingFixtureLabel(string HomeCode, string HomeName, string AwayCode, string AwayName);

/// <summary>One combination of remaining results and who qualifies under it. <see cref="Outcomes"/> aligns
/// with the fixtures: 1 = home win, 0 = draw, -1 = away win.</summary>
public sealed record PermutationRow(
    IReadOnlyList<int> Outcomes,
    string FirstCode, string FirstName,
    string SecondCode, string SecondName,
    string ThirdCode, string ThirdName,
    bool SelectedQualifies);

/// <summary>Every combination of the remaining group results, with the resulting top two (who qualify).</summary>
public sealed record GroupPermutations(
    char Group,
    IReadOnlyList<RemainingFixtureLabel> Fixtures,
    IReadOnlyList<PermutationRow> Rows,
    int TotalCombinations,
    string? SelectedCode);
