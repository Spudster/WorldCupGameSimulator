using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>
/// Simulates one complete tournament (group stage → final) at a chosen fidelity, producing a rich
/// <see cref="TournamentResult"/>. Supports "current state" mode by locking already-played results.
/// This is the reference, object-based path used for single playthroughs and detailed runs; the
/// millions-of-runs fast path lives in <c>FastTournamentSimulator</c>.
/// </summary>
public sealed class TournamentSimulator
{
    private readonly TournamentData _data;
    private readonly SimulationParameters _p;
    private readonly bool _includeThirdPlace;
    private readonly Dictionary<(string, string), PlayedResult> _locked;
    private readonly HashSet<string> _hostCodes;
    private readonly Dictionary<string, Team> _teamsByCode;

    public TournamentSimulator(
        TournamentData data,
        SimulationParameters parameters,
        bool includeThirdPlacePlayoff = true,
        IReadOnlyList<PlayedResult>? lockedResults = null)
    {
        _data = data;
        _p = parameters;
        _includeThirdPlace = includeThirdPlacePlayoff;
        _teamsByCode = data.Teams.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        _hostCodes = data.Teams
            .Where(t => data.Metadata.Hosts.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
            .Select(t => t.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _locked = new Dictionary<(string, string), PlayedResult>();
        if (lockedResults is not null)
        {
            foreach (var r in lockedResults)
            {
                _locked[Key(r.HomeCode, r.AwayCode)] = r;
            }
        }
    }

    public TournamentResult Simulate(Fidelity fidelity, ref Xoshiro256 rng)
    {
        var groupResults = new List<MatchResult>(72);
        var standings = new Dictionary<char, IReadOnlyList<TeamStanding>>();
        var outcomesByGroup = _data.Groups.ToDictionary(g => g, _ => new List<GroupMatchOutcome>());

        // Cross-match discipline/fitness state, fresh per simulated tournament: a player on two yellows
        // (or shown a red) sits out his team's next game; a Minor/Major injury rules him out for the rest.
        // (Empty in fast mode, which has no cards/injuries, so the millions-of-runs path pays nothing.)
        var yellowTally = new Dictionary<string, int>(StringComparer.Ordinal);
        var banned = new HashSet<string>(StringComparer.Ordinal);
        var injuredOut = new HashSet<string>(StringComparer.Ordinal);

        // --- Group stage ---
        foreach (var fixture in _data.GroupSchedule)
        {
            var home = _teamsByCode[fixture.HomeCode];
            var away = _teamsByCode[fixture.AwayCode];

            var serving = ServingThisMatch(home, away, banned);
            MatchResult result = PlayGroupMatch(home, away, Effective(banned, injuredOut), fidelity, ref rng);
            groupResults.Add(result);
            outcomesByGroup[fixture.Group].Add(new GroupMatchOutcome(
                home.Code, away.Code, result.HomeGoals, result.AwayGoals,
                FairPlay(result, home.Code), FairPlay(result, away.Code)));

            UpdateDiscipline(result, yellowTally, banned, injuredOut);
            banned.ExceptWith(serving); // these players have now served their ban
        }

        var winners = new Dictionary<char, string>();
        var runnersUp = new Dictionary<char, string>();
        var thirds = new List<TeamStanding>();

        foreach (var group in _data.Groups)
        {
            var codes = _data.TeamsInGroup(group).Select(t => t.Code).ToList();
            var table = GroupStandingsCalculator.Compute(group, codes, outcomesByGroup[group], ref rng);
            standings[group] = table;
            winners[group] = table[0].Code;
            runnersUp[group] = table[1].Code;
            thirds.Add(table[2]);
        }

        // --- Best third-placed teams ---
        var rankedThirds = GroupStandingsCalculator.RankThirdPlaced(thirds, ref rng);
        var qualifiedThirds = rankedThirds.Take(8).ToList();
        var eliminatedThirds = rankedThirds.Skip(8).ToList();

        var thirdForWinnerGroup = AssignThirds(winners, qualifiedThirds);

        // --- Knockout stage ---
        var resolver = new KnockoutResolver(winners, runnersUp, thirdForWinnerGroup);
        var knockout = new List<KnockoutOutcome>();
        string champion = string.Empty, runnerUp = string.Empty, thirdPlace = string.Empty;

        foreach (var def in _data.Bracket.Matches)
        {
            if (def.Stage == Stage.ThirdPlacePlayoff && !_includeThirdPlace)
            {
                continue;
            }

            var home = _teamsByCode[resolver.Resolve(def.Top)];
            var away = _teamsByCode[resolver.Resolve(def.Bottom)];
            double hs = _p.EffectiveStrength(home);
            double as_ = _p.EffectiveStrength(away);

            var serving = ServingThisMatch(home, away, banned);
            MatchResult result = MatchSimulator.Simulate(home, away, def.Stage, fidelity, Effective(banned, injuredOut), ref rng, neutralVenue: true);
            UpdateDiscipline(result, yellowTally, banned, injuredOut);
            banned.ExceptWith(serving);
            // Knockout must have a winner; MatchSimulator handles ET/pens for non-group stages.
            string winnerCode = result.WinnerCode.Length > 0
                ? result.WinnerCode
                : (hs >= as_ ? home.Code : away.Code);
            string loserCode = winnerCode == home.Code ? away.Code : home.Code;
            resolver.Record(def.Id, winnerCode, loserCode);
            knockout.Add(new KnockoutOutcome(def.Id, def.Stage, def.Label, result));

            if (def.Stage == Stage.Final)
            {
                champion = winnerCode;
                runnerUp = loserCode;
            }
            else if (def.Stage == Stage.ThirdPlacePlayoff)
            {
                thirdPlace = winnerCode;
            }
        }

        return new TournamentResult
        {
            Fidelity = fidelity,
            ParameterLabel = _p.Label,
            Seed = _p.Global.Seed,
            GroupStandings = standings,
            QualifiedThirds = qualifiedThirds,
            EliminatedThirds = eliminatedThirds,
            GroupResults = groupResults,
            KnockoutResults = knockout,
            ChampionCode = champion,
            RunnerUpCode = runnerUp,
            ThirdPlaceCode = thirdPlace,
            FurthestStage = ComputeFurthestStage(knockout),
        };
    }

    private MatchResult PlayGroupMatch(Team home, Team away, SimulationParameters p, Fidelity fidelity, ref Xoshiro256 rng)
    {
        if (_locked.TryGetValue(Key(home.Code, away.Code), out var locked))
        {
            // Orient the locked score to this fixture's home/away.
            bool sameOrientation = string.Equals(locked.HomeCode, home.Code, StringComparison.OrdinalIgnoreCase);
            int hg = sameOrientation ? locked.HomeGoals : locked.AwayGoals;
            int ag = sameOrientation ? locked.AwayGoals : locked.HomeGoals;
            string winner = hg > ag ? home.Code : ag > hg ? away.Code : string.Empty;
            return new MatchResult
            {
                Fidelity = fidelity,
                Stage = Stage.Group,
                HomeCode = home.Code,
                AwayCode = away.Code,
                HomeName = home.Name,
                AwayName = away.Name,
                HomeGoals = hg,
                AwayGoals = ag,
                Method = MatchMethod.Regulation,
                WinnerCode = winner,
                IsLocked = true,
            };
        }

        bool neutral = !_hostCodes.Contains(home.Code);
        return MatchSimulator.Simulate(home, away, Stage.Group, fidelity, p, ref rng, neutral);
    }

    /// <summary>The banned players who belong to the two sides in this match (they sit it out, then are cleared).</summary>
    private static HashSet<string> ServingThisMatch(Team home, Team away, HashSet<string> banned)
    {
        var serving = new HashSet<string>(StringComparer.Ordinal);
        if (banned.Count == 0)
        {
            return serving;
        }

        foreach (var pl in home.Squad.Concat(away.Squad))
        {
            if (banned.Contains(pl.Id))
            {
                serving.Add(pl.Id);
            }
        }

        return serving;
    }

    /// <summary>The parameter set for a match: the base set plus any currently suspended / injured players
    /// added to the unavailable list, so the line-up selector picks their replacements. Returns the base
    /// set unchanged (no allocation) when nobody is out — which is always the case in fast mode.</summary>
    private SimulationParameters Effective(HashSet<string> banned, HashSet<string> injuredOut)
    {
        if (banned.Count == 0 && injuredOut.Count == 0)
        {
            return _p;
        }

        var clone = _p.Clone();
        clone.UnavailablePlayers.UnionWith(banned);
        clone.UnavailablePlayers.UnionWith(injuredOut);
        return clone;
    }

    /// <summary>Accrue a match's bookings and injuries: a red (or a second yellow) earns a one-match ban,
    /// two yellows across games earns one (and resets the count), and a Minor/Major injury ends the run.</summary>
    private static void UpdateDiscipline(MatchResult result, Dictionary<string, int> yellowTally, HashSet<string> banned, HashSet<string> injuredOut)
    {
        foreach (var c in result.Cards)
        {
            if (c.IsRed)
            {
                banned.Add(c.PlayerId);
            }
            else
            {
                int tally = yellowTally.GetValueOrDefault(c.PlayerId) + 1;
                if (tally >= 2)
                {
                    banned.Add(c.PlayerId);
                    yellowTally[c.PlayerId] = 0; // count wiped once the ban is incurred
                }
                else
                {
                    yellowTally[c.PlayerId] = tally;
                }
            }
        }

        foreach (var inj in result.Injuries)
        {
            if (inj.Severity != InjurySeverity.Knock)
            {
                injuredOut.Add(inj.PlayerId);
            }
        }
    }

    private Dictionary<char, string> AssignThirds(
        IReadOnlyDictionary<char, string> winners, IReadOnlyList<TeamStanding> qualifiedThirds)
    {
        var qualifyingGroups = qualifiedThirds.Select(t => t.Group).ToList();
        var assignment = ThirdPlaceAssigner.Assign(
            _data.Bracket.ThirdPlaceWinnerGroups, qualifyingGroups, _data.Bracket.ThirdPlaceEligibleGroups);

        var thirdByGroup = qualifiedThirds.ToDictionary(t => t.Group, t => t.Code);
        var result = new Dictionary<char, string>();
        foreach (var (winnerGroup, sourceGroup) in assignment)
        {
            result[winnerGroup] = thirdByGroup[sourceGroup];
        }

        return result;
    }

    private IReadOnlyDictionary<string, Stage> ComputeFurthestStage(IReadOnlyList<KnockoutOutcome> knockout)
    {
        var furthest = new Dictionary<string, Stage>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in _data.Teams)
        {
            furthest[t.Code] = Stage.Group;
        }

        foreach (var k in knockout)
        {
            Update(k.Result.HomeCode, k.Stage);
            Update(k.Result.AwayCode, k.Stage);
        }

        return furthest;

        void Update(string code, Stage stage)
        {
            if (furthest.TryGetValue(code, out var cur) && Stages.Rank(stage) > Stages.Rank(cur))
            {
                furthest[code] = stage;
            }
        }
    }

    private static int FairPlay(MatchResult r, string teamCode)
    {
        int points = 0;
        foreach (var c in r.Cards)
        {
            if (string.Equals(c.TeamCode, teamCode, StringComparison.OrdinalIgnoreCase))
            {
                points += c.IsRed ? 3 : 1;
            }
        }

        return points;
    }

    private static (string, string) Key(string a, string b)
    {
        return string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
    }
}
