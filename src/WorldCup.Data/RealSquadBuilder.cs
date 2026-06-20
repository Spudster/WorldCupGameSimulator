using WorldCup.Data.Models;

namespace WorldCup.Data;

/// <summary>
/// Builds a squad of real players from <c>(name, position, rating)</c> triples. Players are ordered
/// best-first within each position (GK → DEF → MID → FWD) so the lineup selector picks the
/// first-choice XI, and the five event-probability attributes are derived deterministically from
/// each player's overall rating (same player ⇒ same attributes across runs). Player identities are
/// real; the attribute numbers are best-effort estimates from the rating.
/// </summary>
public static class RealSquadBuilder
{
    public static IReadOnlyList<Player> Build(string teamCode, IReadOnlyList<(string Name, Position Position, int Rating)> players)
    {
        var ordered = players
            .OrderBy(p => PositionOrder(p.Position))
            .ThenByDescending(p => p.Rating)
            .ToList();

        var result = new List<Player>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++)
        {
            var (name, position, rating) = ordered[i];
            string id = $"{teamCode}-{i + 1:D2}";
            var rng = new Random(SyntheticSquadGenerator.StableSeed(teamCode + "|" + name));
            var attributes = SyntheticSquadGenerator.BuildAttributes(position, rating, rng);
            result.Add(new Player(id, name, teamCode, position, attributes, IsSynthetic: false));
        }

        return result;
    }

    private static int PositionOrder(Position position) => position switch
    {
        Position.GK => 0,
        Position.DEF => 1,
        Position.MID => 2,
        Position.FWD => 3,
        _ => 4,
    };
}
