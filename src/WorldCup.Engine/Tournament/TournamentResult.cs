using System.Text.Json.Serialization;
using WorldCup.Data.Models;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>One played knockout match within a single-tournament playthrough.</summary>
public sealed record KnockoutOutcome(int MatchId, Stage Stage, string Label, MatchResult Result);

/// <summary>
/// The full result of a single simulated tournament: group standings, the qualifying third-placed
/// teams, every match result (group + knockout), and the path to the champion. In detailed mode the
/// match results carry full box scores and events for stat aggregation.
/// </summary>
public sealed class TournamentResult
{
    public required Fidelity Fidelity { get; init; }
    public required string ParameterLabel { get; init; }
    public required ulong Seed { get; init; }

    public required IReadOnlyDictionary<char, IReadOnlyList<TeamStanding>> GroupStandings { get; init; }
    public required IReadOnlyList<TeamStanding> QualifiedThirds { get; init; }
    public required IReadOnlyList<TeamStanding> EliminatedThirds { get; init; }

    public required IReadOnlyList<MatchResult> GroupResults { get; init; }
    public required IReadOnlyList<KnockoutOutcome> KnockoutResults { get; init; }

    public required string ChampionCode { get; init; }
    public string RunnerUpCode { get; init; } = string.Empty;
    public string ThirdPlaceCode { get; init; } = string.Empty;

    /// <summary>The deepest stage each team appeared in (drives awards/MVP advancement weighting).</summary>
    public required IReadOnlyDictionary<string, Stage> FurthestStage { get; init; }

    /// <summary>All match results (group then knockout) for convenience.</summary>
    [JsonIgnore]
    public IEnumerable<MatchResult> AllMatches =>
        GroupResults.Concat(KnockoutResults.Select(k => k.Result));

    public KnockoutOutcome? Match(int id) => KnockoutResults.FirstOrDefault(k => k.MatchId == id);
}
