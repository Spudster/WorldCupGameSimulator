using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>Per-team probabilities from a full-tournament Monte Carlo run.</summary>
public sealed record TeamTournamentOdds(
    string Code,
    string Name,
    char Group,
    double Champion,
    double ReachedFinal,
    double ReachedSemi,
    double ReachedQuarter,
    double ReachedR16,
    double ReachedR32,
    double TopGroup,
    double ExpectedGroupPoints);

/// <summary>Probability of a particular final matchup.</summary>
public sealed record FinalMatchupOdds(
    string CodeA, string NameA, string CodeB, string NameB, double Probability);

/// <summary>The headline report from a full-tournament Monte Carlo run.</summary>
public sealed record TournamentMonteCarloReport(
    long Iterations,
    string ParameterLabel,
    ulong Seed,
    Fidelity Fidelity,
    bool IncludeThirdPlace,
    bool CurrentState,
    double ElapsedSeconds,
    double SimsPerSecond,
    IReadOnlyList<TeamTournamentOdds> Teams,
    IReadOnlyList<FinalMatchupOdds> TopFinalMatchups)
{
    public TeamTournamentOdds? MostLikelyChampion => Teams.MaxBy(t => t.Champion);
    public FinalMatchupOdds? MostLikelyFinal => TopFinalMatchups.Count > 0 ? TopFinalMatchups[0] : null;
}

/// <summary>Thread-safe live progress counter that long runs update and the UI polls.</summary>
public sealed class ProgressCounter
{
    private long _completed;

    public long Total { get; init; }

    public long Completed => Interlocked.Read(ref _completed);

    public void Add(long n) => Interlocked.Add(ref _completed, n);
}
