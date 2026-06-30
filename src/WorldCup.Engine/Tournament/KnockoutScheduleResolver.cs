using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>One scheduled knockout fixture, with its teams resolved as far as the current results allow.</summary>
/// <param name="MatchId">FIFA match number (73–104).</param>
/// <param name="Stage">Knockout round.</param>
/// <param name="Label">Bracket label, e.g. "R32-2".</param>
/// <param name="KickoffUtc">Scheduled kickoff (UTC).</param>
/// <param name="HomeCode">Resolved home/top team code, or null when not yet known.</param>
/// <param name="AwayCode">Resolved away/bottom team code, or null when not yet known.</param>
/// <param name="HomeLabel">Display label for the top side — the team name if resolved, else its bracket slot ("Winner Grp E", "Winner R32-2"…).</param>
/// <param name="AwayLabel">Display label for the bottom side.</param>
/// <param name="Projected">True when at least one side is only known because an unfinished group was projected from current form.</param>
/// <param name="Played">True when this match already has a real result loaded.</param>
/// <param name="HomeGoals">Home goals if <see cref="Played"/>.</param>
/// <param name="AwayGoals">Away goals if <see cref="Played"/>.</param>
public sealed record KnockoutScheduleFixture(
    int MatchId,
    Stage Stage,
    string Label,
    DateTime KickoffUtc,
    string? HomeCode,
    string? AwayCode,
    string HomeLabel,
    string AwayLabel,
    bool Projected,
    bool Played,
    int HomeGoals,
    int AwayGoals)
{
    /// <summary>True when both teams are known, so the match can actually be forecast / played out.</summary>
    public bool IsResolved => HomeCode is not null && AwayCode is not null;
}

/// <summary>The dated knockout schedule with its matchups resolved against the current results.</summary>
/// <param name="Fixtures">All 32 knockout matches in id order.</param>
/// <param name="AllGroupsComplete">True when every group has all six results in, so R32 matchups are settled (not projected).</param>
/// <param name="IncompleteGroups">Groups still missing results (their winner/runner-up/third are projected from current form).</param>
public sealed record KnockoutSchedule(
    IReadOnlyList<KnockoutScheduleFixture> Fixtures,
    bool AllGroupsComplete,
    IReadOnlyList<char> IncompleteGroups);

/// <summary>
/// Turns the fixed knockout bracket plus the results played so far into a dated, team-resolved schedule.
/// <para>
/// Group winners, runners-up and the best-third assignment come from the current group tables; where a
/// group has not finished its standings are <em>projected</em> from current form (a single deterministic
/// current-state run of the remaining group games), so the Round of 32 always has concrete matchups to
/// forecast. Later rounds (R16 onward) stay as bracket placeholders ("Winner R32-2") until the feeding
/// knockout match has actually been played — those are filled in from real results only, never guessed.
/// </para>
/// </summary>
public static class KnockoutScheduleResolver
{
    public static KnockoutSchedule Resolve(
        TournamentData data,
        IReadOnlyList<PlayedResult> playedResults,
        SimulationParameters parameters,
        ulong seed,
        bool includeThirdPlacePlayoff = true)
    {
        var bracket = data.Bracket;
        var playedByPair = new Dictionary<(string, string), PlayedResult>();
        foreach (var r in playedResults)
        {
            playedByPair[Pair(r.HomeCode, r.AwayCode)] = r;
        }

        // Which groups have all six games in (so their final table is settled, not projected).
        var incompleteGroups = data.Groups
            .Where(g => data.GroupSchedule.Count(f => f.Group == g && playedByPair.ContainsKey(Pair(f.HomeCode, f.AwayCode))) < 6)
            .ToList();
        bool allGroupsComplete = incompleteGroups.Count == 0;

        // One deterministic current-state run gives complete group tables: real where the games are in,
        // projected from current form where they are not. We only read the group resolution from it.
        var rng = new Xoshiro256(seed);
        var sim = new TournamentSimulator(data, parameters, includeThirdPlacePlayoff, playedResults);
        var run = sim.Simulate(Fidelity.Fast, ref rng);

        var winners = new Dictionary<char, string>();
        var runnersUp = new Dictionary<char, string>();
        foreach (var (group, table) in run.GroupStandings)
        {
            winners[group] = table[0].Code;
            runnersUp[group] = table[1].Code;
        }

        // Best-third assignment to the eight group-winner slots (same logic the simulator uses).
        var qualifyingThirdGroups = run.QualifiedThirds.Select(t => t.Group).ToList();
        var thirdByGroup = run.QualifiedThirds.ToDictionary(t => t.Group, t => t.Code);
        var assignment = ThirdPlaceAssigner.Assign(
            bracket.ThirdPlaceWinnerGroups, qualifyingThirdGroups, bracket.ThirdPlaceEligibleGroups);
        var thirdForWinnerGroup = new Dictionary<char, string>();
        foreach (var (winnerGroup, sourceGroup) in assignment)
        {
            thirdForWinnerGroup[winnerGroup] = thirdByGroup[sourceGroup];
        }

        var labelById = bracket.Matches.ToDictionary(m => m.Id, m => m.Label);
        var incomplete = incompleteGroups.ToHashSet();
        var matchWinner = new Dictionary<int, string>();
        var matchLoser = new Dictionary<int, string>();

        var fixtures = new List<KnockoutScheduleFixture>(bracket.Matches.Count);
        foreach (var def in bracket.Matches)
        {
            var (homeCode, homeProjected) = ResolveSide(def.Top);
            var (awayCode, awayProjected) = ResolveSide(def.Bottom);

            string homeLabel = homeCode is not null ? data.Team(homeCode).Name : SlotLabel(def.Top);
            string awayLabel = awayCode is not null ? data.Team(awayCode).Name : SlotLabel(def.Bottom);

            bool played = false;
            int hg = 0, ag = 0;
            if (homeCode is not null && awayCode is not null &&
                playedByPair.TryGetValue(Pair(homeCode, awayCode), out var pr))
            {
                played = true;
                bool same = string.Equals(pr.HomeCode, homeCode, StringComparison.OrdinalIgnoreCase);
                hg = same ? pr.HomeGoals : pr.AwayGoals;
                ag = same ? pr.AwayGoals : pr.HomeGoals;

                // Record the decided side so later rounds resolve from this real result.
                if (hg != ag)
                {
                    string winnerCode = hg > ag ? homeCode : awayCode;
                    string loserCode = hg > ag ? awayCode : homeCode;
                    matchWinner[def.Id] = winnerCode;
                    matchLoser[def.Id] = loserCode;
                }
            }

            fixtures.Add(new KnockoutScheduleFixture(
                def.Id, def.Stage, def.Label, def.KickoffUtc,
                homeCode, awayCode, homeLabel, awayLabel,
                Projected: homeProjected || awayProjected,
                Played: played, HomeGoals: hg, AwayGoals: ag));
        }

        return new KnockoutSchedule(fixtures, allGroupsComplete, incompleteGroups);

        // Resolve one side of a match to a concrete code (+ whether that code came from a projected group),
        // or null when it is not yet determined (an earlier knockout match that has not been played).
        (string? Code, bool Projected) ResolveSide(Feeder f)
        {
            switch (f.Kind)
            {
                case FeederKind.GroupSlot:
                    return f.Slot.Kind switch
                    {
                        SlotSpecKind.Winner => (winners[f.Slot.Group], incomplete.Contains(f.Slot.Group)),
                        SlotSpecKind.RunnerUp => (runnersUp[f.Slot.Group], incomplete.Contains(f.Slot.Group)),
                        // The best-third ranking is cross-group, so it is only settled once every group is in.
                        SlotSpecKind.ThirdForWinner => (
                            thirdForWinnerGroup.TryGetValue(f.Slot.Group, out var c) ? c : null,
                            !allGroupsComplete),
                        _ => (null, false),
                    };
                case FeederKind.MatchWinner:
                    return (matchWinner.TryGetValue(f.MatchId, out var w) ? w : null, false);
                case FeederKind.MatchLoser:
                    return (matchLoser.TryGetValue(f.MatchId, out var l) ? l : null, false);
                default:
                    return (null, false);
            }
        }

        string SlotLabel(Feeder f) => f.Kind switch
        {
            FeederKind.GroupSlot => f.Slot.Kind switch
            {
                SlotSpecKind.Winner => $"Winner Grp {f.Slot.Group}",
                SlotSpecKind.RunnerUp => $"2nd Grp {f.Slot.Group}",
                SlotSpecKind.ThirdForWinner => "Best 3rd",
                _ => "?",
            },
            FeederKind.MatchWinner => $"Winner {Lbl(f.MatchId)}",
            FeederKind.MatchLoser => $"Loser {Lbl(f.MatchId)}",
            _ => "?",
        };

        string Lbl(int id) => labelById.TryGetValue(id, out var s) ? s : $"M{id}";
    }

    private static (string, string) Pair(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
}
