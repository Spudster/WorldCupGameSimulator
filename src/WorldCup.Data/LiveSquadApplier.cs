using WorldCup.Data.Models;

namespace WorldCup.Data;

/// <summary>
/// Fills in a real World Cup squad from the live API (player names + positions) ONLY for teams whose
/// bundled roster is synthetic. The bundled <c>squads_2026.json</c> rosters are researched, current and
/// individually rated, whereas the live national-team lists are often stale/provisional (e.g. still
/// listing players who aren't actually in the matchday squad), so a good bundled squad is never clobbered.
/// The API has no ability ratings, so filled-in players get an estimate anchored on the team's strength
/// with a mild within-position gradient (first-listed ≈ regulars).
/// </summary>
public static class LiveSquadApplier
{
    /// <param name="applied">Number of teams whose (synthetic) squad was replaced with live data.</param>
    public static TournamentData Apply(
        TournamentData data,
        IReadOnlyDictionary<string, IReadOnlyList<(string Name, Position Position)>> squads,
        out int applied)
    {
        applied = 0;
        var newTeams = new List<Team>(data.Teams.Count);
        foreach (var team in data.Teams)
        {
            // Only fall back to live data when we have no researched bundled squad for this team.
            if (team.IsSyntheticSquad && squads.TryGetValue(team.Code, out var roster) && roster.Count >= 14)
            {
                var triples = BuildTriples(roster, team.Strength);
                var squad = RealSquadBuilder.Build(team.Code, triples);
                newTeams.Add(team with { Squad = squad, IsSyntheticSquad = false });
                applied++;
            }
            else
            {
                newTeams.Add(team);
            }
        }

        // Rebuild TournamentData so its internal team-by-code cache is fresh.
        return new TournamentData(data.Metadata, newTeams, data.GroupSchedule, data.Bracket);
    }

    private static List<(string Name, Position Position, int Rating)> BuildTriples(
        IReadOnlyList<(string Name, Position Position)> roster, double teamStrength)
    {
        var perPositionIndex = new Dictionary<Position, int>();
        var triples = new List<(string, Position, int)>(roster.Count);
        foreach (var (name, pos) in roster)
        {
            int idx = perPositionIndex.TryGetValue(pos, out int c) ? c : 0;
            perPositionIndex[pos] = idx + 1;

            // Earlier-listed players in a position get a small rating bump (they tend to be regulars).
            int bonus = Math.Clamp((int)Math.Round(4 - idx * 1.5), -5, 4);
            int rating = (int)Math.Clamp(Math.Round(teamStrength) + bonus, 30, 95);
            triples.Add((name, pos, rating));
        }

        return triples;
    }
}
