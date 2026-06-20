using WorldCup.Data.Models;

namespace WorldCup.Data;

/// <summary>
/// Builds the single round-robin group-stage schedule (3 matchdays, 6 matches per group)
/// deterministically from the seeded order of teams within each group. Kickoff dates are
/// spread across the group-stage window so the "current/next fixture" feature has something
/// realistic to point at; orientation (home/away) is taken from the round-robin pattern.
/// </summary>
public static class ScheduleGenerator
{
    // Group stage of the 2026 World Cup runs ~June 11–27. Matchdays are spaced a few days apart.
    private static readonly DateTime GroupStageStart = new(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Generate the full group schedule. Each group's four teams are ordered by pot; the standard
    /// 4-team round robin is: MD1 (1v2, 3v4), MD2 (1v3, 4v2), MD3 (1v4, 2v3).
    /// </summary>
    public static IReadOnlyList<GroupFixture> Build(TournamentData data) => Build(data.Teams);

    /// <summary>Generate the group schedule directly from a team list.</summary>
    public static IReadOnlyList<GroupFixture> Build(IReadOnlyList<Team> teams)
    {
        var fixtures = new List<GroupFixture>();
        var groups = teams.Select(t => t.Group).Distinct().OrderBy(g => g).ToList();

        for (int gi = 0; gi < groups.Count; gi++)
        {
            char group = groups[gi];
            var inGroup = teams.Where(t => t.Group == group).OrderBy(t => t.Pot).ToList();
            if (inGroup.Count != 4)
            {
                throw new InvalidOperationException(
                    $"Group {group} must contain exactly 4 teams but has {inGroup.Count}.");
            }

            string p1 = inGroup[0].Code, p2 = inGroup[1].Code, p3 = inGroup[2].Code, p4 = inGroup[3].Code;

            // (matchday, home, away) tuples for the standard 4-team round robin.
            (int Md, string Home, string Away)[] pattern =
            {
                (1, p1, p2), (1, p3, p4),
                (2, p1, p3), (2, p4, p2),
                (3, p1, p4), (3, p2, p3),
            };

            foreach (var (md, home, away) in pattern)
            {
                // Stagger kickoffs: matchdays 4 days apart, groups staggered within each matchday.
                DateTime kickoff = GroupStageStart
                    .AddDays((md - 1) * 4 + gi / 4)
                    .AddHours(13 + (gi % 4) * 3);
                fixtures.Add(new GroupFixture(group, md, home, away, kickoff));
            }
        }

        return fixtures.OrderBy(f => f.KickoffUtc).ThenBy(f => f.Group).ToList();
    }

    /// <summary>
    /// True when the fixtures form a complete single round-robin: every team plays exactly 3 and each
    /// group has exactly 6 matches with valid in-group codes. Used to validate live fixtures before use.
    /// </summary>
    public static bool IsComplete(IReadOnlyList<GroupFixture> fixtures, IReadOnlyList<Team> teams)
    {
        if (fixtures.Count == 0)
        {
            return false;
        }

        var groupOf = teams.ToDictionary(t => t.Code, t => t.Group, StringComparer.OrdinalIgnoreCase);
        var appearances = teams.ToDictionary(t => t.Code, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var f in fixtures)
        {
            if (!groupOf.TryGetValue(f.HomeCode, out var hg) || !groupOf.TryGetValue(f.AwayCode, out var ag) ||
                hg != f.Group || ag != f.Group)
            {
                return false;
            }

            appearances[f.HomeCode]++;
            appearances[f.AwayCode]++;
        }

        if (appearances.Values.Any(c => c != 3))
        {
            return false;
        }

        foreach (var group in teams.Select(t => t.Group).Distinct())
        {
            if (fixtures.Count(f => f.Group == group) != 6)
            {
                return false;
            }
        }

        return true;
    }
}
