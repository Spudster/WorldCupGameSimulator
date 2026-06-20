using WorldCup.Data.Models;

namespace WorldCup.Data;

/// <summary>
/// Pluggable source of tournament data (teams, groups, schedule, bracket) and of the
/// real results already played. Implementations: an offline seed-file provider (default,
/// always available) and a live football-data API provider (preferred when configured).
/// </summary>
public interface ITeamDataProvider
{
    /// <summary>Short human-readable name of this provider (shown in reports).</summary>
    string Name { get; }

    /// <summary>True when this provider is usable in the current environment.</summary>
    bool IsAvailable { get; }

    /// <summary>Load the full immutable tournament definition.</summary>
    TournamentData GetTournamentData();

    /// <summary>
    /// Load the results of matches already played in the real tournament (for "current state"
    /// mode). Returns an empty list when no results are available.
    /// </summary>
    IReadOnlyList<PlayedResult> GetPlayedResults();
}
