using WorldCup.Data.Models;

namespace WorldCup.Data;

/// <summary>
/// Deterministically generates a plausible synthetic squad for a team from its strength rating.
/// Squads produced here are flagged <see cref="Player.IsSynthetic"/> = true and
/// <see cref="Team.IsSyntheticSquad"/> = true. Player attribute means scale with team strength
/// (so stronger nations field stronger players), with deterministic per-player variation seeded by
/// the team code — i.e. the same team always yields the same squad across runs.
/// </summary>
public static class SyntheticSquadGenerator
{
    // Composition: 26-man squad (3 GK, 8 DEF, 8 MID, 7 FWD).
    private const int Goalkeepers = 3;
    private const int Defenders = 8;
    private const int Midfielders = 8;
    private const int Forwards = 7;

    public const int SquadSize = Goalkeepers + Defenders + Midfielders + Forwards;

    private static readonly string[] Surnames =
    {
        "Silva", "Santos", "Müller", "Rossi", "Kovač", "Nguyen", "Okafor", "Tanaka", "Dubois", "Ferreira",
        "Hansen", "Novak", "Costa", "Yilmaz", "Ahmed", "Khan", "Petrov", "Morales", "Andersen", "Bauer",
        "Lopez", "Kim", "Park", "Diallo", "Traoré", "Mensah", "Hassan", "Castro", "Vidal", "Romero",
        "Schneider", "Larsson", "Nielsen", "Walsh", "Murphy", "Sanchez", "Mbappé", "Owusu", "Suzuki", "Ivanov",
        "Conte", "Bianchi", "Reyes", "Flores", "Adebayo", "Sow", "Haaland", "Vargas", "Becker", "Horvat",
        "Marin", "Popescu", "Cruz", "Mendoza", "Akande", "Toure", "Nakamura", "Almeida", "Gomez", "Fernandez",
        "Wright", "Taylor", "Brown", "Jensen", "Bjornsson", "Eriksen", "Kane", "Sterling", "Foden", "Bellingham",
        "Modric", "Perišić", "Kvaratskhelia", "Osimhen", "Salah", "Mahrez", "Son", "Lee", "Endo", "Mitoma",
    };

    /// <summary>Generate a full synthetic squad for a team.</summary>
    /// <param name="teamCode">3-letter team code (used for ids and as the RNG seed).</param>
    /// <param name="strength">Team strength rating (0–100), used as the attribute baseline.</param>
    public static IReadOnlyList<Player> Generate(string teamCode, double strength)
    {
        var rng = new Random(StableSeed(teamCode));
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var players = new List<Player>(SquadSize);

        int shirt = 1;
        AddBlock(Position.GK, Goalkeepers);
        AddBlock(Position.DEF, Defenders);
        AddBlock(Position.MID, Midfielders);
        AddBlock(Position.FWD, Forwards);
        return players;

        void AddBlock(Position pos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                string id = $"{teamCode}-{shirt:D2}";
                string name = UniqueName(rng, usedNames);
                players.Add(new Player(id, name, teamCode, pos, BuildAttributes(pos, strength, rng), IsSynthetic: true));
                shirt++;
            }
        }
    }

    /// <summary>
    /// Build position-shaped attributes anchored on a 0–100 quality value (a team's strength for a
    /// synthetic player, or a real player's overall rating). Shared by synthetic and real squads.
    /// </summary>
    public static PlayerAttributes BuildAttributes(Position pos, double anchor, Random rng)
    {
        // Means are anchored on the quality value and shaped by position; variation is +/- a spread.
        double Vary(double mean, double spread) =>
            Math.Clamp(mean + (rng.NextDouble() * 2 - 1) * spread, 1, 99);

        double finishing = pos switch
        {
            Position.FWD => Vary(anchor * 0.90 + 8, 10),
            Position.MID => Vary(anchor * 0.70, 12),
            Position.DEF => Vary(anchor * 0.42, 12),
            _ => Vary(8, 5),
        };
        double creativity = pos switch
        {
            Position.MID => Vary(anchor * 0.88 + 5, 11),
            Position.FWD => Vary(anchor * 0.72, 12),
            Position.DEF => Vary(anchor * 0.50, 12),
            _ => Vary(12, 6),
        };
        // Discipline: higher = cleaner. Defenders/midfielders foul a touch more.
        double discipline = pos switch
        {
            Position.DEF => Vary(58, 16),
            Position.MID => Vary(60, 16),
            _ => Vary(66, 14),
        };
        double injury = Vary(30, 18);
        double goalkeeping = pos == Position.GK ? Vary(anchor * 0.92 + 6, 9) : Vary(6, 4);

        return new PlayerAttributes(finishing, creativity, discipline, injury, goalkeeping).Clamped();
    }

    private static string UniqueName(Random rng, HashSet<string> used)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            string surname = Surnames[rng.Next(Surnames.Length)];
            char initial = (char)('A' + rng.Next(26));
            string name = $"{initial}. {surname}";
            if (used.Add(name))
            {
                return name;
            }
        }

        // Fallback that is guaranteed unique.
        string fallback = $"Player {used.Count + 1}";
        used.Add(fallback);
        return fallback;
    }

    /// <summary>A stable, run-independent seed derived from a string (FNV-1a).</summary>
    internal static int StableSeed(string text)
    {
        unchecked
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            uint hash = offset;
            foreach (char c in text)
            {
                hash = (hash ^ c) * prime;
            }

            return (int)hash;
        }
    }
}
