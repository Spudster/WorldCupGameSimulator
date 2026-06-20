namespace WorldCup.Data.Models;

/// <summary>
/// A national team. <see cref="Strength"/> is an abstract 0–100 rating derived from
/// FIFA ranking / Elo (or hand-seeded in the offline data); it is the primary driver
/// of expected goals in the match model. The squad is the list of <see cref="Player"/>s
/// used by the detailed event simulation.
/// </summary>
/// <param name="Code">3-letter FIFA code (e.g. "BRA"). Unique key for the team.</param>
/// <param name="Name">Display name (e.g. "Brazil").</param>
/// <param name="Confederation">Owning confederation.</param>
/// <param name="Group">Group letter A–L.</param>
/// <param name="Pot">Draw pot (1–4); pot 1 = top seed. Also used as the in-group seed position.</param>
/// <param name="Strength">Abstract strength rating, 0–100.</param>
/// <param name="FifaRanking">World ranking position at seed time (0 if unknown).</param>
/// <param name="Squad">The player roster (~23–26 players).</param>
/// <param name="IsSyntheticSquad">True when the squad was generated rather than sourced from real data.</param>
public sealed record Team(
    string Code,
    string Name,
    Confederation Confederation,
    char Group,
    int Pot,
    double Strength,
    int FifaRanking,
    IReadOnlyList<Player> Squad,
    bool IsSyntheticSquad)
{
    /// <summary>Players at a given position.</summary>
    public IEnumerable<Player> PlayersAt(Position position) =>
        Squad.Where(p => p.Position == position);
}
