using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>A team's computed recent-form strength adjustment, with a short human explanation for the UI.</summary>
/// <param name="TeamCode">3-letter team code.</param>
/// <param name="TeamName">Display name.</param>
/// <param name="Delta">Strength points (0–100 scale) to add to the team's effective strength going forward
/// (positive = over-performed its rating, negative = under-performed). Already capped.</param>
/// <param name="GamesUsed">How many played games fed the adjustment.</param>
/// <param name="Summary">A short "what happened" line, e.g. "D 0–0 v Spain — above expectations".</param>
public sealed record FormAdjustment(string TeamCode, string TeamName, double Delta, int GamesUsed, string Summary);

/// <summary>
/// Turns already-played results into a per-team "recent form" strength adjustment for forward
/// predictions. Each played game is scored by how its actual goal difference compared with what the
/// strength model expected: a team that over-performs (e.g. an underdog holding a favourite to a draw)
/// is nudged up for its upcoming games, and one that under-performs is nudged down. The adjustment is
/// symmetric (both teams in a game move in opposite directions), recency-weighted and capped.
/// <para>
/// The result is only ever applied through <see cref="SimulationParameters.TeamFormDeltas"/>, so the
/// pre-tournament odds and the model calibration — which use no played results — are unaffected.
/// </para>
/// </summary>
public static class FormModel
{
    /// <summary>
    /// Compute the recent-form strength delta for every team that has played at least one of the supplied
    /// results, strongest movers first. Returns an empty list when form is disabled
    /// (<see cref="GlobalParameters.FormWeight"/> ≤ 0) or there are no results.
    /// <para>
    /// Call this <em>before</em> assigning the deltas onto <paramref name="p"/> — the expected scores are
    /// read from <paramref name="p"/>'s current (form-free) strengths.
    /// </para>
    /// </summary>
    public static IReadOnlyList<FormAdjustment> Compute(
        TournamentData data, IReadOnlyList<PlayedResult> played, SimulationParameters p)
    {
        var g = p.Global;
        if (g.FormWeight <= 0 || played is null || played.Count == 0)
        {
            return Array.Empty<FormAdjustment>();
        }

        // Recency signal: match each result to its scheduled fixture to read the matchday (a higher
        // matchday is more recent). Falls back to the result's position in the list when no fixture matches.
        var matchdayByPair = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in data.GroupSchedule)
        {
            matchdayByPair[Pair(f.HomeCode, f.AwayCode)] = f.Matchday;
        }

        var perTeam = new Dictionary<string, List<Game>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < played.Count; i++)
        {
            var r = played[i];
            if (!data.TryGetTeam(r.HomeCode, out var home) || !data.TryGetTeam(r.AwayCode, out var away))
            {
                continue; // a result for a team we don't model — skip it.
            }

            double hs = p.EffectiveStrength(home);
            double as_ = p.EffectiveStrength(away);
            // Score played games at a neutral venue: the results file notes that the home/away orientation
            // here need not match the real schedule, and host advantage in expectation is small.
            var (eh, ea) = MatchModel.ExpectedGoals(hs, as_, g, neutralVenue: true);
            double expGdHome = eh - ea;
            double actGdHome = r.HomeGoals - r.AwayGoals;
            double surpriseHome = actGdHome - expGdHome; // positive ⇒ the home side over-performed

            int recency = matchdayByPair.TryGetValue(Pair(r.HomeCode, r.AwayCode), out int md) ? md : i + 1;

            Record(perTeam, home.Code, surpriseHome, recency, r.HomeGoals, r.AwayGoals, away.Name);
            Record(perTeam, away.Code, -surpriseHome, recency, r.AwayGoals, r.HomeGoals, home.Name);
        }

        var result = new List<FormAdjustment>(perTeam.Count);
        foreach (var (code, games) in perTeam)
        {
            games.Sort((x, y) => y.Recency.CompareTo(x.Recency)); // most recent first

            double weighted = 0, wsum = 0, w = 1.0;
            foreach (var gm in games)
            {
                weighted += w * gm.Surprise;
                wsum += w;
                w *= g.FormRecencyDecay;
            }

            double avgSurprise = wsum > 0 ? weighted / wsum : 0;
            double delta = Math.Clamp(g.FormGoalDiffToStrength * avgSurprise, -g.FormMaxDelta, g.FormMaxDelta);
            string name = data.TryGetTeam(code, out var team) ? team.Name : code;
            result.Add(new FormAdjustment(code, name, delta, games.Count, Summarize(games[0], delta, games.Count)));
        }

        result.Sort((x, y) => Math.Abs(y.Delta).CompareTo(Math.Abs(x.Delta))); // biggest movers first
        return result;
    }

    private static void Record(
        Dictionary<string, List<Game>> perTeam, string code,
        double surprise, int recency, int goalsFor, int goalsAgainst, string opponent)
    {
        if (!perTeam.TryGetValue(code, out var list))
        {
            list = new List<Game>();
            perTeam[code] = list;
        }

        list.Add(new Game(surprise, recency, goalsFor, goalsAgainst, opponent));
    }

    private static string Summarize(Game latest, double delta, int gamesUsed)
    {
        char outcome = latest.GoalsFor > latest.GoalsAgainst ? 'W' : latest.GoalsFor < latest.GoalsAgainst ? 'L' : 'D';
        string verdict = delta > 0.05 ? "above expectations" : delta < -0.05 ? "below expectations" : "as expected";
        string more = gamesUsed > 1 ? $" (+{gamesUsed - 1} earlier)" : "";
        return $"{outcome} {latest.GoalsFor}–{latest.GoalsAgainst} v {latest.Opponent} — {verdict}{more}";
    }

    /// <summary>An order-independent key for an unordered team pair.</summary>
    private static string Pair(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

    private readonly record struct Game(double Surprise, int Recency, int GoalsFor, int GoalsAgainst, string Opponent);
}
