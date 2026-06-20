namespace WorldCup.Engine.Simulation;

/// <summary>
/// The in-match "flow" reconstructed from the goal timeline: the biggest lead each side held and how
/// many times the lead changed hands. Used for per-match flow stats and the comeback records.
/// </summary>
public readonly record struct MatchFlow(int MaxHomeLead, int MaxAwayLead, int LeadChanges)
{
    public static MatchFlow Analyze(MatchResult m)
    {
        int h = 0, a = 0, maxHome = 0, maxAway = 0, changes = 0, prevLeader = 0;
        foreach (var g in m.Goals.OrderBy(x => x.Minute))
        {
            if (string.Equals(g.TeamCode, m.HomeCode, StringComparison.OrdinalIgnoreCase))
            {
                h++;
            }
            else
            {
                a++;
            }

            int margin = h - a;
            if (margin > maxHome) maxHome = margin;
            if (-margin > maxAway) maxAway = -margin;

            int leader = margin > 0 ? 1 : margin < 0 ? -1 : 0;
            if (leader != 0 && prevLeader != 0 && leader != prevLeader) changes++;
            if (leader != 0) prevLeader = leader;
        }

        return new MatchFlow(maxHome, maxAway, changes);
    }
}
