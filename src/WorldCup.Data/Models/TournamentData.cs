namespace WorldCup.Data.Models;

/// <summary>Top-level tournament metadata surfaced in reports.</summary>
/// <param name="Name">e.g. "FIFA World Cup 2026".</param>
/// <param name="Hosts">Host nations.</param>
/// <param name="FinalDateUtc">Date of the final (2026-07-19).</param>
/// <param name="SourceNote">Free-text note about where the data came from (real draw, synthetic squads, etc.).</param>
public sealed record TournamentMetadata(
    string Name,
    IReadOnlyList<string> Hosts,
    DateTime FinalDateUtc,
    string SourceNote);

/// <summary>
/// The complete, immutable description of a tournament instance: who is playing,
/// in which groups, the group-stage schedule, and the knockout bracket structure.
/// Produced by an <c>ITeamDataProvider</c>.
/// </summary>
public sealed record TournamentData(
    TournamentMetadata Metadata,
    IReadOnlyList<Team> Teams,
    IReadOnlyList<GroupFixture> GroupSchedule,
    BracketDefinition Bracket)
{
    private Dictionary<string, Team>? _byCode;

    /// <summary>The distinct group letters, ordered A..L.</summary>
    public IReadOnlyList<char> Groups =>
        Teams.Select(t => t.Group).Distinct().OrderBy(g => g).ToList();

    /// <summary>The four teams in a group, ordered by pot (seed).</summary>
    public IReadOnlyList<Team> TeamsInGroup(char group) =>
        Teams.Where(t => t.Group == group).OrderBy(t => t.Pot).ToList();

    /// <summary>Look up a team by its 3-letter code.</summary>
    public Team Team(string code)
    {
        _byCode ??= Teams.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        return _byCode[code];
    }

    /// <summary>Try to look up a team by code.</summary>
    public bool TryGetTeam(string code, out Team team)
    {
        _byCode ??= Teams.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        return _byCode.TryGetValue(code, out team!);
    }
}
