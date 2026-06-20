using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Tournament;

namespace WorldCup.Engine.Stats;

/// <summary>
/// Accumulates detailed-mode statistics across one or many simulated tournaments: per-player and
/// per-team tallies, the leaderboards/awards (Golden Boot, MVP, Golden Glove, discipline), the
/// extensible "crazy stats" records, streaks/comebacks and the injury list. Across a Monte Carlo
/// run it also tracks how often each player wins each award. Locked (real) results carry no event
/// detail and are excluded from detailed stats.
/// </summary>
public sealed class TournamentStatsAggregator
{
    private readonly TournamentData _data;
    private readonly MvpWeights _weights;
    private readonly Dictionary<string, Player> _playersById;

    private readonly Dictionary<string, PlayerAcc> _players = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TeamAcc> _teams = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IRecordTracker> _records = RecordTrackers.CreateDefault();
    private readonly List<InjuryItem> _injuries = new();
    private readonly Dictionary<GoalType, long> _goalTypes = new();

    private readonly Dictionary<string, long> _bootWins = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _bootTally = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _mvpWins = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _mvpTally = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _gloveWins = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _gloveTally = new(StringComparer.Ordinal);

    private int _longestUnbeaten = -1;
    private string _longestUnbeatenDesc = "—";
    private int _longestWinning = -1;
    private string _longestWinningDesc = "—";
    private int _mostComebacks = -1;
    private string _mostComebacksDesc = "—";

    public TournamentStatsAggregator(TournamentData data, MvpWeights weights)
    {
        _data = data;
        _weights = weights;
        _playersById = data.Teams.SelectMany(t => t.Squad).ToDictionary(p => p.Id, StringComparer.Ordinal);
    }

    public int Tournaments { get; private set; }

    /// <summary>Ingest one simulated tournament.</summary>
    public void Add(TournamentResult result)
    {
        Tournaments++;

        // Per-tournament tallies (only simulated, event-bearing matches).
        var localPlayers = new Dictionary<string, PlayerAcc>(StringComparer.Ordinal);

        foreach (var match in result.AllMatches)
        {
            _records.ForEach(r => r.Observe(match, NameOf(match.HomeCode), NameOf(match.AwayCode)));

            if (match.IsLocked || match.HomeBox is null || match.AwayBox is null)
            {
                continue;
            }

            AccumulateTeam(match);
            AccumulateMatchPlayers(match, localPlayers);
        }

        // Merge local → cumulative and compute this tournament's awards.
        foreach (var (id, acc) in localPlayers)
        {
            GetPlayer(id).MergeFrom(acc);
        }

        AwardPerTournament(result, localPlayers);
        ProcessStreaks(result);
    }

    public StatsReport Build(int topN = 15)
    {
        var goldenBoot = _players.Values
            .Where(p => p.Goals > 0)
            .OrderByDescending(p => p.Goals).ThenByDescending(p => p.Assists).ThenBy(p => p.Minutes)
            .Take(topN)
            .Select(p => new ScorerRow(p.Id, p.Name, p.Team, p.Goals, p.Assists, p.Minutes))
            .ToList();

        var assists = _players.Values
            .Where(p => p.Assists > 0)
            .OrderByDescending(p => p.Assists).ThenByDescending(p => p.Goals)
            .Take(topN)
            .Select(p => new AssistRow(p.Id, p.Name, p.Team, p.Assists, p.Goals))
            .ToList();

        var mvp = _players.Values
            .Where(p => p.MvpScore > 0)
            .OrderByDescending(p => p.MvpScore)
            .Take(topN)
            .Select(p => new MvpRow(p.Id, p.Name, p.Team, Math.Round(p.MvpScore, 1),
                p.Goals, p.Assists, p.CleanSheets, p.Minutes, p.FurthestStage))
            .ToList();

        var glove = _players.Values
            .Where(p => p.Position == Position.GK && p.Minutes > 0)
            .OrderByDescending(p => p.CleanSheets).ThenByDescending(p => p.Saves).ThenBy(p => p.GoalsConceded)
            .Take(topN)
            .Select(p => new GoalkeeperRow(p.Id, p.Name, p.Team, p.CleanSheets, p.Saves, p.AmazingSaves, p.GoalsConceded, p.Minutes))
            .ToList();

        var mostYellows = _players.Values
            .Where(p => p.Yellows > 0)
            .OrderByDescending(p => p.Yellows).ThenByDescending(p => p.Reds)
            .Take(topN)
            .Select(p => new DisciplineRow(p.Id, p.Name, p.Team, p.Yellows, p.Reds))
            .ToList();

        var mostReds = _players.Values
            .Where(p => p.Reds > 0)
            .OrderByDescending(p => p.Reds).ThenByDescending(p => p.Yellows)
            .Take(topN)
            .Select(p => new DisciplineRow(p.Id, p.Name, p.Team, p.Yellows, p.Reds))
            .ToList();

        var teams = _teams.Values
            .OrderByDescending(t => t.GoalsFor)
            .Select(t => new TeamStatRow(t.Code, NameOf(t.Code), t.GoalsFor, t.GoalsAgainst, t.CleanSheets,
                t.Shots, t.ShotsOnTarget, t.Corners, t.Yellows, t.Reds,
                t.PossessionMatches > 0 ? Math.Round(t.PossessionSum / t.PossessionMatches, 1) : 0))
            .ToList();

        var penaltyTakers = _players.Values
            .Where(p => p.PenaltiesScored + p.PenaltiesMissed > 0)
            .OrderByDescending(p => p.PenaltiesScored).ThenBy(p => p.PenaltiesMissed)
            .Take(topN)
            .Select(p => new PenaltyTakerRow(p.Id, p.Name, p.Team, p.PenaltiesScored, p.PenaltiesMissed))
            .ToList();

        long totalGoals = _goalTypes.Values.Sum();
        var goalsByType = _goalTypes
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new GoalTypeRow(GoalTypeName(kv.Key), (int)kv.Value, totalGoals > 0 ? 100.0 * kv.Value / totalGoals : 0))
            .ToList();

        var crazy = BuildRecords();

        var injuries = _injuries
            .OrderBy(i => i.Minute)
            .Take(30)
            .ToList();

        return new StatsReport(
            Tournaments, goldenBoot, assists, mvp, glove, mostYellows, mostReds, teams,
            penaltyTakers, goalsByType, crazy,
            injuries, _injuries.Count,
            FrequencyTable(_bootWins, _bootTally.ToDictionary(k => k.Key, v => (double)v.Value)),
            FrequencyTable(_mvpWins, _mvpTally),
            FrequencyTable(_gloveWins, _gloveTally.ToDictionary(k => k.Key, v => (double)v.Value)));
    }

    // --- accumulation helpers ---

    private void AccumulateTeam(MatchResult m)
    {
        var home = GetTeam(m.HomeCode);
        var away = GetTeam(m.AwayCode);
        ApplyTeam(home, m.HomeBox!, conceded: m.AwayGoals);
        ApplyTeam(away, m.AwayBox!, conceded: m.HomeGoals);
    }

    private static void ApplyTeam(TeamAcc t, TeamBoxScore box, int conceded)
    {
        t.GoalsFor += box.Goals;
        t.GoalsAgainst += conceded;
        t.Shots += box.Shots;
        t.ShotsOnTarget += box.ShotsOnTarget;
        t.Corners += box.Corners;
        t.Yellows += box.Yellows;
        t.Reds += box.Reds;
        t.PossessionSum += box.PossessionPercent;
        t.PossessionMatches++;
        if (conceded == 0)
        {
            t.CleanSheets++;
        }
    }

    private void AccumulateMatchPlayers(MatchResult m, Dictionary<string, PlayerAcc> local)
    {
        PlayerAcc Local(string id) => GetLocal(local, id);

        foreach (var goal in m.Goals)
        {
            _goalTypes[goal.Type] = _goalTypes.GetValueOrDefault(goal.Type) + 1;

            if (!goal.IsOwnGoal)
            {
                Local(goal.ScorerId).Goals++;
            }

            if (goal.AssistId is not null)
            {
                Local(goal.AssistId).Assists++;
            }
        }

        foreach (var c in m.Cards)
        {
            if (c.IsRed)
            {
                Local(c.PlayerId).Reds++;
            }
            else
            {
                Local(c.PlayerId).Yellows++;
            }
        }

        foreach (var pen in m.Penalties)
        {
            if (pen.Outcome == PenaltyOutcome.Scored)
            {
                Local(pen.TakerId).PenaltiesScored++;
            }
            else
            {
                Local(pen.TakerId).PenaltiesMissed++;
            }
        }

        foreach (var inj in m.Injuries)
        {
            Local(inj.PlayerId).Injuries++;
            _injuries.Add(new InjuryItem(inj.PlayerName, inj.TeamCode, inj.Minute, inj.Severity, inj.CouldBeReplaced,
                inj.BodyPart, inj.Diagnosis, inj.RecoveryDays));
        }

        foreach (var save in m.SaveEvents)
        {
            if (save.IsAmazing && _playersById.ContainsKey(save.KeeperId))
            {
                Local(save.KeeperId).AmazingSaves++;
            }
        }

        foreach (var (id, mins) in m.Minutes)
        {
            Local(id).Minutes += mins;
        }

        // Goalkeeper credit: the keeper with the most minutes for each side.
        CreditKeeper(m, local, m.HomeLineup, m.HomeBox!, concededByTeam: m.AwayGoals);
        CreditKeeper(m, local, m.AwayLineup, m.AwayBox!, concededByTeam: m.HomeGoals);
    }

    private void CreditKeeper(
        MatchResult m, Dictionary<string, PlayerAcc> local, IReadOnlyList<string> lineup,
        TeamBoxScore box, int concededByTeam)
    {
        string? keeperId = lineup
            .Where(id => _playersById.TryGetValue(id, out var pl) && pl.Position == Position.GK)
            .OrderByDescending(id => m.Minutes.TryGetValue(id, out int mn) ? mn : 0)
            .FirstOrDefault();
        if (keeperId is null)
        {
            return;
        }

        var acc = GetLocal(local, keeperId);
        acc.Saves += box.Saves;
        acc.GoalsConceded += concededByTeam;
        if (concededByTeam == 0)
        {
            acc.CleanSheets++;
        }
    }

    private void AwardPerTournament(TournamentResult result, Dictionary<string, PlayerAcc> local)
    {
        if (local.Count == 0)
        {
            return;
        }

        // Compute this tournament's MVP score for each player and merge into cumulative.
        foreach (var (id, acc) in local)
        {
            var team = _playersById[id].TeamCode;
            Stage furthest = result.FurthestStage.TryGetValue(team, out var st) ? st : Stage.Group;
            bool champ = string.Equals(team, result.ChampionCode, StringComparison.OrdinalIgnoreCase);
            double score = MvpScore(acc, furthest, champ);
            acc.MvpScore = score;
            GetPlayer(id).MvpScore += score;
            GetPlayer(id).FurthestStage = Stages.DisplayName(furthest);
        }

        // Golden Boot winner.
        var boot = local.Values
            .Where(p => p.Goals > 0)
            .OrderByDescending(p => p.Goals).ThenByDescending(p => p.Assists).ThenBy(p => p.Minutes)
            .FirstOrDefault();
        if (boot is not null)
        {
            Increment(_bootWins, boot.Id);
            _bootTally[boot.Id] = _bootTally.GetValueOrDefault(boot.Id) + boot.Goals;
        }

        var mvp = local.Values.OrderByDescending(p => p.MvpScore).FirstOrDefault();
        if (mvp is not null && mvp.MvpScore > 0)
        {
            Increment(_mvpWins, mvp.Id);
            _mvpTally[mvp.Id] = _mvpTally.GetValueOrDefault(mvp.Id) + mvp.MvpScore;
        }

        var glove = local.Values
            .Where(p => p.Position == Position.GK && p.Minutes > 0)
            .OrderByDescending(p => p.CleanSheets).ThenByDescending(p => p.Saves).ThenBy(p => p.GoalsConceded)
            .FirstOrDefault();
        if (glove is not null)
        {
            Increment(_gloveWins, glove.Id);
            _gloveTally[glove.Id] = _gloveTally.GetValueOrDefault(glove.Id) + glove.CleanSheets;
        }
    }

    private double MvpScore(PlayerAcc p, Stage furthest, bool champion)
    {
        double raw = _weights.Goal * p.Goals + _weights.Assist * p.Assists
            + _weights.CleanSheet * p.CleanSheets + _weights.DefensiveAction * p.Saves
            + _weights.Minutes * (p.Minutes / 90.0);
        return raw * AdvancementMultiplier(furthest, champion);
    }

    private double AdvancementMultiplier(Stage furthest, bool champion)
    {
        if (champion)
        {
            return _weights.ChampionMultiplier;
        }

        return furthest switch
        {
            Stage.Final => _weights.FinalistMultiplier,
            Stage.SemiFinal or Stage.ThirdPlacePlayoff => _weights.SemiFinalMultiplier,
            Stage.QuarterFinal => _weights.QuarterFinalMultiplier,
            Stage.RoundOf16 => _weights.RoundOf16Multiplier,
            Stage.RoundOf32 => _weights.RoundOf32Multiplier,
            _ => _weights.GroupStageMultiplier,
        };
    }

    private void ProcessStreaks(TournamentResult result)
    {
        // Build each team's ordered W/D/L sequence and comeback count for this tournament.
        var sequence = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase); // 1 win, 0 draw, -1 loss
        var comebacks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var ordered = result.GroupResults.Concat(
            result.KnockoutResults.OrderBy(k => k.MatchId).Select(k => k.Result));

        foreach (var m in ordered)
        {
            Record(m.HomeCode, m);
            Record(m.AwayCode, m);
            DetectComeback(m);
        }

        foreach (var (code, results) in sequence)
        {
            int unbeaten = 0, bestUnbeaten = 0, winning = 0, bestWinning = 0;
            foreach (int r in results)
            {
                if (r >= 0) { unbeaten++; bestUnbeaten = Math.Max(bestUnbeaten, unbeaten); }
                else { unbeaten = 0; }

                if (r > 0) { winning++; bestWinning = Math.Max(bestWinning, winning); }
                else { winning = 0; }
            }

            if (bestUnbeaten > _longestUnbeaten)
            {
                _longestUnbeaten = bestUnbeaten;
                _longestUnbeatenDesc = $"{NameOf(code)} — {bestUnbeaten} matches";
            }

            if (bestWinning > _longestWinning)
            {
                _longestWinning = bestWinning;
                _longestWinningDesc = $"{NameOf(code)} — {bestWinning} wins";
            }
        }

        foreach (var (code, n) in comebacks)
        {
            if (n > _mostComebacks)
            {
                _mostComebacks = n;
                _mostComebacksDesc = $"{NameOf(code)} — {n} comeback win(s)";
            }
        }

        void Record(string code, MatchResult m)
        {
            if (!sequence.TryGetValue(code, out var list))
            {
                list = new List<int>();
                sequence[code] = list;
            }

            // Classify by the decided winner so penalty-shootout results aren't treated as draws.
            int outcome = m.WinnerCode.Length == 0
                ? 0
                : string.Equals(m.WinnerCode, code, StringComparison.OrdinalIgnoreCase) ? 1 : -1;
            list.Add(outcome);
        }

        void DetectComeback(MatchResult m)
        {
            if (m.Goals.Count == 0)
            {
                return;
            }

            string winner = m.WinnerCode;
            if (winner.Length == 0)
            {
                return;
            }

            int h = 0, a = 0;
            bool winnerTrailed = false;
            foreach (var goal in m.Goals.OrderBy(x => x.Minute))
            {
                if (string.Equals(goal.TeamCode, m.HomeCode, StringComparison.OrdinalIgnoreCase)) h++; else a++;
                bool winnerIsHome = string.Equals(winner, m.HomeCode, StringComparison.OrdinalIgnoreCase);
                int winnerScore = winnerIsHome ? h : a;
                int otherScore = winnerIsHome ? a : h;
                if (winnerScore < otherScore)
                {
                    winnerTrailed = true;
                }
            }

            if (winnerTrailed)
            {
                comebacks[winner] = comebacks.GetValueOrDefault(winner) + 1;
            }
        }
    }

    private List<RecordEntry> BuildRecords()
    {
        var list = new List<RecordEntry>();
        foreach (var tracker in _records)
        {
            if (tracker.Result is { } entry)
            {
                list.Add(entry);
            }
        }

        if (_longestUnbeaten >= 0)
        {
            list.Add(new RecordEntry("Longest unbeaten run", _longestUnbeatenDesc, _longestUnbeaten));
        }

        if (_longestWinning >= 0)
        {
            list.Add(new RecordEntry("Longest winning streak", _longestWinningDesc, _longestWinning));
        }

        if (_mostComebacks >= 0)
        {
            list.Add(new RecordEntry("Most comeback wins", _mostComebacksDesc, _mostComebacks));
        }

        var played = _teams.Values.Where(t => t.PossessionMatches > 0).ToList();
        if (played.Count > 0)
        {
            var dirtiest = played.MaxBy(t => t.Yellows + t.Reds * 3)!;
            var cleanest = played.MinBy(t => t.Yellows + t.Reds * 3)!;
            list.Add(new RecordEntry("Dirtiest team", $"{NameOf(dirtiest.Code)} — {dirtiest.Yellows}Y {dirtiest.Reds}R", dirtiest.Yellows + dirtiest.Reds));
            list.Add(new RecordEntry("Cleanest team", $"{NameOf(cleanest.Code)} — {cleanest.Yellows}Y {cleanest.Reds}R", cleanest.Yellows + cleanest.Reds));
        }

        return list;
    }

    private List<AwardFrequencyRow> FrequencyTable(Dictionary<string, long> wins, Dictionary<string, double> tally)
    {
        double inv = Tournaments > 0 ? 1.0 / Tournaments : 0;
        return wins
            .OrderByDescending(kv => kv.Value)
            .Take(15)
            .Select(kv =>
            {
                var p = _playersById[kv.Key];
                double avg = kv.Value > 0 ? tally.GetValueOrDefault(kv.Key) / kv.Value : 0;
                return new AwardFrequencyRow(kv.Key, p.Name, p.TeamCode, kv.Value, kv.Value * inv, Math.Round(avg, 2));
            })
            .ToList();
    }

    private static void Increment(Dictionary<string, long> d, string key) => d[key] = d.GetValueOrDefault(key) + 1;

    private PlayerAcc GetLocal(Dictionary<string, PlayerAcc> local, string id)
    {
        if (!local.TryGetValue(id, out var acc))
        {
            var p = _playersById[id];
            acc = new PlayerAcc(id, p.Name, p.TeamCode, p.Position);
            local[id] = acc;
        }

        return acc;
    }

    private PlayerAcc GetPlayer(string id)
    {
        if (!_players.TryGetValue(id, out var acc))
        {
            var p = _playersById[id];
            acc = new PlayerAcc(id, p.Name, p.TeamCode, p.Position);
            _players[id] = acc;
        }

        return acc;
    }

    private TeamAcc GetTeam(string code)
    {
        if (!_teams.TryGetValue(code, out var acc))
        {
            acc = new TeamAcc(code);
            _teams[code] = acc;
        }

        return acc;
    }

    private string NameOf(string code) =>
        _data.TryGetTeam(code, out var t) ? t.Name : code;

    private static string GoalTypeName(GoalType t) => t switch
    {
        GoalType.OpenPlay => "Open play",
        GoalType.Header => "Header",
        GoalType.FreeKick => "Free kick",
        GoalType.Penalty => "Penalty",
        GoalType.LongRange => "Long range",
        GoalType.BicycleKick => "Bicycle kick",
        GoalType.OwnGoal => "Own goal",
        _ => t.ToString(),
    };

    private sealed class PlayerAcc
    {
        public PlayerAcc(string id, string name, string team, Position position)
        {
            Id = id;
            Name = name;
            Team = team;
            Position = position;
        }

        public string Id { get; }
        public string Name { get; }
        public string Team { get; }
        public Position Position { get; }
        public int Goals, Assists, Minutes, Yellows, Reds, PenaltiesScored, PenaltiesMissed, Saves, AmazingSaves, CleanSheets, GoalsConceded, Injuries;
        public double MvpScore;
        public string FurthestStage = "Group stage";

        public void MergeFrom(PlayerAcc o)
        {
            Goals += o.Goals;
            Assists += o.Assists;
            Minutes += o.Minutes;
            Yellows += o.Yellows;
            Reds += o.Reds;
            PenaltiesScored += o.PenaltiesScored;
            PenaltiesMissed += o.PenaltiesMissed;
            Saves += o.Saves;
            AmazingSaves += o.AmazingSaves;
            CleanSheets += o.CleanSheets;
            GoalsConceded += o.GoalsConceded;
            Injuries += o.Injuries;
        }
    }

    private sealed class TeamAcc
    {
        public TeamAcc(string code)
        {
            Code = code;
        }

        public string Code { get; }
        public int GoalsFor, GoalsAgainst, CleanSheets, Shots, ShotsOnTarget, Corners, Yellows, Reds, PossessionMatches;
        public double PossessionSum;
    }
}
