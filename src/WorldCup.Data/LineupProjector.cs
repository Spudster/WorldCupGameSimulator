using WorldCup.Data.Models;

namespace WorldCup.Data;

/// <summary>
/// Derives a team's most-probable starting XI for a given formation, excluding any unavailable
/// (injured/suspended/rested) players. Real squads are stored best-first within each position
/// (see <see cref="RealSquadBuilder"/>), so taking the top players per position yields the likely
/// first-choice line-up. This is the same selection the match engine uses, so the displayed line-up
/// matches the one actually simulated.
/// </summary>
public static class LineupProjector
{
    public static (IReadOnlyList<Player> Xi, IReadOnlyList<Player> Bench, string Formation) Project(
        Team team, string formation = "4-3-3", Func<Player, bool>? isAvailable = null,
        IReadOnlyCollection<string>? preferredStarterIds = null)
    {
        var (def, mid, fwd) = ParseFormation(formation);
        var available = isAvailable is null ? team.Squad.ToList() : team.Squad.Where(isAvailable).ToList();

        // A team can't take the field with nobody — if availability filters out (nearly) everyone,
        // fall back to the full squad so the selection never produces a degenerate/empty XI.
        if (available.Count < 7)
        {
            available = team.Squad.ToList();
        }

        // Players are already best-first within a position; a preferred starter (e.g. the real
        // first-choice keeper) is pulled to the front of its group so it makes the XI. OrderBy is
        // stable, so non-preferred players keep their rating order.
        IEnumerable<Player> Pick(Position pos, int count)
        {
            var pool = available.Where(pl => pl.Position == pos);
            if (preferredStarterIds is { Count: > 0 })
            {
                pool = pool.OrderByDescending(pl => preferredStarterIds.Contains(pl.Id));
            }

            return pool.Take(count);
        }

        var xi = new List<Player>(11);
        xi.AddRange(Pick(Position.GK, 1));
        xi.AddRange(Pick(Position.DEF, def));
        xi.AddRange(Pick(Position.MID, mid));
        xi.AddRange(Pick(Position.FWD, fwd));

        // Backfill to 11 from the remaining available players if a position is short.
        if (xi.Count < 11)
        {
            foreach (var p in available)
            {
                if (xi.Count >= 11)
                {
                    break;
                }

                if (!xi.Contains(p))
                {
                    xi.Add(p);
                }
            }
        }

        var bench = available.Where(p => !xi.Contains(p)).ToList();
        return (xi, bench, formation);
    }

    /// <summary>
    /// Parse a formation string into outfield counts. The first number is defenders, the last is
    /// forwards, and everything in between is midfield — so "4-3-3", "4-4-2", "3-5-2", "4-2-3-1" and
    /// "5-3-2" all work. Invalid input falls back to 4-3-3.
    /// </summary>
    public static (int Def, int Mid, int Fwd) ParseFormation(string formation)
    {
        var parts = (formation ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2 && parts.All(s => int.TryParse(s, out _)))
        {
            var nums = parts.Select(int.Parse).ToList();
            int def = nums[0];
            int fwd = nums[^1];
            int mid = nums.Skip(1).Take(nums.Count - 2).Sum();
            if (def >= 2 && fwd >= 1 && mid >= 1 && def + mid + fwd == 10)
            {
                return (def, mid, fwd);
            }
        }

        return (4, 3, 3);
    }

    /// <summary>The common formations offered in the UI.</summary>
    public static readonly IReadOnlyList<string> Common = new[]
    {
        "4-3-3", "4-4-2", "4-2-3-1", "4-5-1", "3-5-2", "3-4-3", "5-3-2", "5-4-1",
    };
}
