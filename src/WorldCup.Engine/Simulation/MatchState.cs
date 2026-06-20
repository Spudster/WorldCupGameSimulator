using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Mutable working state for a single detailed-mode match. Holds the live score, on-pitch players,
/// accumulated events, and exposes the per-minute hazards the <see cref="DetailedMatchSimulator"/>
/// drives. Not thread-safe (one instance per match).
/// </summary>
internal sealed class MatchState
{
    private readonly Team _home;
    private readonly Team _away;
    private readonly SimulationParameters _p;
    private readonly GlobalParameters _g;
    private readonly bool _neutral;

    private readonly double _homeStrengthBase;
    private readonly double _awayStrengthBase;
    private double _homeStrength;
    private double _awayStrength;

    private readonly List<Player> _homeOnPitch;
    private readonly List<Player> _awayOnPitch;
    private readonly List<Player> _homeBench;
    private readonly List<Player> _awayBench;
    private int _homeSubs;
    private int _awaySubs;

    private readonly Dictionary<string, int> _yellowCount = new(StringComparer.Ordinal);
    private readonly HashSet<string> _sentOff = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _entryMinute = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _offAtMinute = new(StringComparer.Ordinal);
    private int _homePenaltySaves;
    private int _awayPenaltySaves;
    private int _homePenaltyGoals;
    private int _awayPenaltyGoals;

    private readonly List<GoalEvent> _goals = new();
    private readonly List<CardEvent> _cards = new();
    private readonly List<PenaltyEvent> _penalties = new();
    private readonly List<InjuryEvent> _injuries = new();
    private readonly List<SubstitutionEvent> _substitutions = new();
    private readonly List<PlayerErrorEvent> _errors = new();
    private readonly List<BadCallEvent> _badCalls = new();
    private readonly List<CoolingBreak> _coolingBreaks = new();
    private readonly List<Confrontation> _confrontations = new();
    private int _temperatureC;
    private readonly MiracleEvent? _miracle;

    private static readonly string[] MiracleTags =
    {
        "an inspired, against-all-odds performance for the ages",
        "the underdogs playing completely out of their skins",
        "a fearless, fairytale display in the face of the odds",
        "wave after wave of belief — the minnows refusing to know their place",
        "a backs-to-the-wall, heart-on-sleeve effort for the history books",
        "the spirit of every giant-killer there's ever been coursing through them",
    };

    private static string MiracleTag(ref Xoshiro256 rng) => MiracleTags[rng.NextInt(MiracleTags.Length)];

    private readonly double _homeForm;
    private readonly double _awayForm;
    private readonly double _tempo;

    /// <summary>In-match momentum: positive favours the home side, negative the away side; mean-reverts.</summary>
    private double _swing;

    public MatchState(Team home, Team away, SimulationParameters p, bool neutral, ref Xoshiro256 rng)
    {
        _home = home;
        _away = away;
        _p = p;
        _g = p.Global;
        _neutral = neutral;

        _homeStrengthBase = _homeStrength = p.EffectiveStrength(home);
        _awayStrengthBase = _awayStrength = p.EffectiveStrength(away);

        // "Day of destiny": a rare, realistic chance the underdog catches fire — the actual CAUSE of a
        // giant-killing. We boost the WORKING strengths (which drive the goals) but keep the BASE strengths
        // for the pre-match odds and the upset rating, so the rating still measures how shocking it was.
        var miracle = MatchModel.RollMiracle(_homeStrength, _awayStrength, _g, ref rng);
        _homeStrength = miracle.HomeStrength;
        _awayStrength = miracle.AwayStrength;
        if (miracle.Fired)
        {
            var underdog = miracle.ForHome ? home : away;
            _miracle = new MiracleEvent(1 + rng.NextInt(20), underdog.Code, underdog.Name, MiracleTag(ref rng));
        }

        // One per-match form draw per team ("any given day") so results carry realistic upset variance,
        // plus one shared "tempo" draw so some games are open shootouts and others cagey grinds.
        _homeForm = MatchModel.FormFactor(ref rng, _g.UpsetVariance);
        _awayForm = MatchModel.FormFactor(ref rng, _g.UpsetVariance);
        _tempo = MatchModel.FormFactor(ref rng, _g.MatchTempoVariance);

        (_homeOnPitch, _homeBench) = SelectLineup(home, p);
        (_awayOnPitch, _awayBench) = SelectLineup(away, p);
        foreach (var pl in _homeOnPitch) _entryMinute[pl.Id] = 0;
        foreach (var pl in _awayOnPitch) _entryMinute[pl.Id] = 0;

        HomeAttackShare = _homeStrength / Math.Max(1e-6, _homeStrength + _awayStrength);
        AwayAttackShare = 1.0 - HomeAttackShare;
        HomeIndiscipline = Indiscipline(_homeOnPitch);
        AwayIndiscipline = Indiscipline(_awayOnPitch);

        RecomputeLambdas();
    }

    public int HomeGoals { get; private set; }
    public int AwayGoals { get; private set; }
    public int LastMinute { get; set; }

    /// <summary>Referee's added time at the end of each half (display only), set by the simulator.</summary>
    public int FirstHalfStoppage { get; set; }
    public int SecondHalfStoppage { get; set; }

    private bool Trailing(bool isHome) => isHome ? HomeGoals < AwayGoals : AwayGoals < HomeGoals;

    /// <summary>A side chasing the game late genuinely picks up more bookings (tactical/frustration fouls),
    /// so the card hazard is lifted for a trailing team after the hour. Mild, so cards stay near target.</summary>
    public double LateFrustrationMultiplier(bool isHome, int minute) => minute >= 60 && Trailing(isHome) ? 1.25 : 1.0;

    private static readonly string[] FrustrationReasons =
    {
        "dissent — arguing furiously with the referee",
        "a frustrated, late lunge chasing the game",
        "kicking the ball away in anger after the whistle",
        "throwing his arms up and protesting the decision",
        "a cynical, frustrated trip to stop the break",
        "hacking down an opponent out of pure frustration",
        "a needless shove off the ball as tempers fray",
        "complaining his way into the book",
        "a rash, frustrated challenge",
        "lashing out after being dispossessed",
    };

    /// <summary>The booking reason — a frustration reason for a side trailing late, otherwise the usual one.</summary>
    private string YellowReason(ref Xoshiro256 rng, bool isHome, int minute)
    {
        if (minute >= 60 && Trailing(isHome) && rng.NextDouble() < 0.55)
        {
            return FrustrationReasons[rng.NextInt(FrustrationReasons.Length)];
        }

        return CardNarratives.Yellow(ref rng);
    }

    public double HomeStrength => _homeStrength;
    public double AwayStrength => _awayStrength;
    public double HomeAttackShare { get; }
    public double AwayAttackShare { get; }
    public double HomeIndiscipline { get; }
    public double AwayIndiscipline { get; }

    public double HomeOpenPlayLambda { get; private set; }
    public double AwayOpenPlayLambda { get; private set; }

    // --- per-minute event handlers (called by the simulator) ---

    public void ScoreOpenPlay(ref Xoshiro256 rng, bool isHome, int minute)
    {
        var ev = _g.Events;
        var scoringTeam = isHome ? _home : _away;
        var defendingTeam = isHome ? _away : _home;
        var onPitch = isHome ? _homeOnPitch : _awayOnPitch;
        var defenders = isHome ? _awayOnPitch : _homeOnPitch;

        var keeper = FindKeeper(defenders);
        double keeperSkill = keeper is null ? 50 : _p.EffectiveAttributes(keeper).Goalkeeping;

        // Rare own goal: credited to the scoring team but attributed to an opposing defender.
        if (Distributions.Chance(ref rng, 0.035))
        {
            var ogPlayer = PickByPosition(ref rng, defenders, Position.DEF);
            double ogVergazo = ComputeVergazo(GoalType.OwnGoal, 8.0, 0, minute, assisterCreativity: -1, keeperSkill, scorerFinishing: 30, ref rng);
            AddGoal(isHome, new GoalEvent(minute, scoringTeam.Code, ogPlayer.Id, ogPlayer.Name,
                null, null, GoalType.OwnGoal, 8.0, IsPenalty: false, IsOwnGoal: true, DefendersPassed: 0, Vergazo: ogVergazo));
            Swing(towardHome: isHome, weight: 1.2, minute); // a gifted own goal really deflates the conceding side
            return;
        }

        var scorer = PickScorer(ref rng, onPitch);
        double distance = Distributions.SampleShotDistance(ref rng);
        int defendersPassed = SampleDefendersPassed(ref rng);
        GoalType type = ClassifyGoal(ref rng, distance);

        string? assistId = null, assistName = null;
        double assisterCreativity = -1; // < 0 means unassisted (a solo goal)
        if (Distributions.Chance(ref rng, 0.74))
        {
            var assister = PickAssister(ref rng, onPitch, scorer.Id);
            if (assister is not null)
            {
                assistId = assister.Id;
                assistName = assister.Name;
                assisterCreativity = _p.EffectiveAttributes(assister).Creativity;
            }
        }

        double finishing = _p.EffectiveAttributes(scorer).Finishing;
        double vergazo = ComputeVergazo(type, distance, defendersPassed, minute,
            assisterCreativity, keeperSkill, finishing, ref rng);

        // Was the goal gifted by a mistake from the conceding side? A weaker keeper fumbles/flaps more
        // often; a defensive error (slip, miscontrol, blind back-pass) is the other common gift. A
        // gifted goal is never a screamer, so its vergazo is capped low. (This re-labels existing goals
        // — it never adds one — so the goals/match calibration is unchanged.)
        ErrorKind error = ErrorKind.None;
        double keeperFrailty = Math.Clamp(1.0 + (60.0 - keeperSkill) / 45.0, 0.4, 2.2);
        double pKeeperErr = ev.GoalkeeperErrorGoalShare * keeperFrailty;
        double pDefErr = ev.DefensiveErrorGoalShare;
        double errRoll = rng.NextDouble();
        if (keeper is not null && errRoll < pKeeperErr)
        {
            error = ErrorKind.GoalkeeperError;
            vergazo = Math.Round(Math.Min(vergazo, 1.0 + rng.NextDouble() * 2.2), 1);
            _errors.Add(new PlayerErrorEvent(minute, defendingTeam.Code, keeper.Id, keeper.Name,
                error, LedToGoal: true, MistakeNarratives.GoalkeeperError(ledToGoal: true, ref rng)));
        }
        else if (errRoll < pKeeperErr + pDefErr)
        {
            error = ErrorKind.DefensiveError;
            var culprit = PickByPosition(ref rng, defenders, Position.DEF);
            vergazo = Math.Round(Math.Min(vergazo, 3.0 + rng.NextDouble() * 2.5), 1);
            _errors.Add(new PlayerErrorEvent(minute, defendingTeam.Code, culprit.Id, culprit.Name,
                error, LedToGoal: true, MistakeNarratives.DefensiveError(ledToGoal: true, ref rng)));
        }
        else if (Distributions.Chance(ref rng, 0.03))
        {
            // A "good" goal that actually should have been ruled out (a tight offside / a foul missed in
            // the build-up). The goal stands — that's the injustice — so the score is unaffected.
            _badCalls.Add(new BadCallEvent(minute, BadCallType.GoalWronglyAllowed, scoringTeam.Code,
                defendingTeam.Code, VarChecked: rng.NextDouble() < 0.5, scorer.Name,
                $"{scorer.Name}'s goal — {MistakeNarratives.BadCall(BadCallType.GoalWronglyAllowed, ref rng)}"));
        }

        AddGoal(isHome, new GoalEvent(minute, scoringTeam.Code, scorer.Id, scorer.Name,
            assistId, assistName, type, distance, IsPenalty: false, IsOwnGoal: false,
            DefendersPassed: defendersPassed, Vergazo: vergazo, CausedByError: error));

        // Momentum: the scorers ride the wave, the conceding side wobbles — and a goal gifted by a
        // howler stings even more. Early goals (handled in Swing) swing hardest.
        Swing(towardHome: isHome, weight: error != ErrorKind.None ? 1.4 : 1.0, minute);
    }

    public void AwardPenalty(ref Xoshiro256 rng, bool isHome, int minute)
    {
        var attackTeam = isHome ? _home : _away;
        var defendTeam = isHome ? _away : _home;
        var attackers = isHome ? _homeOnPitch : _awayOnPitch;
        var keeper = FindKeeper(isHome ? _awayOnPitch : _homeOnPitch);

        // Was the penalty itself a wrong call? (Tags this existing penalty — never adds one — so the
        // penalties/match calibration is unchanged.) VAR catches some, but plenty are upheld wrongly.
        bool controversial = Distributions.Chance(ref rng, _g.Events.WrongPenaltyShare);

        var taker = PickPenaltyTaker(attackers);
        double takerFinish = _p.EffectiveAttributes(taker).Finishing;
        double keeperSkill = keeper is null ? 30 : _p.EffectiveAttributes(keeper).Goalkeeping;

        double conv = Math.Clamp(
            _g.Events.PenaltyConversionBase + (takerFinish - 50) / 300.0 - (keeperSkill - 50) / 380.0,
            0.45, 0.96);
        double r = rng.NextDouble();

        PenaltyOutcome outcome;
        if (r < conv)
        {
            outcome = PenaltyOutcome.Scored;
            double penVergazo = ComputeVergazo(GoalType.Penalty, 11.0, 0, minute, assisterCreativity: -1, keeperSkill, takerFinish, ref rng);
            AddGoal(isHome, new GoalEvent(minute, attackTeam.Code, taker.Id, taker.Name,
                null, null, GoalType.Penalty, 11.0, IsPenalty: true, IsOwnGoal: false, DefendersPassed: 0, Vergazo: penVergazo));
            if (isHome) _homePenaltyGoals++; else _awayPenaltyGoals++;
        }
        else if (r < conv + (1 - conv) * 0.57)
        {
            // Of the misses, more go off-target / hit the woodwork than are saved (real ≈ 57/43).
            outcome = PenaltyOutcome.Missed;
        }
        else
        {
            outcome = PenaltyOutcome.Saved;
            if (isHome) _awayPenaltySaves++; else _homePenaltySaves++;
        }

        _penalties.Add(new PenaltyEvent(minute, attackTeam.Code, taker.Id, taker.Name,
            keeper?.Id ?? "?", keeper?.Name ?? "(no keeper)", outcome, Controversial: controversial));

        // Momentum: winning the penalty lifts the attackers; converting it lifts them more, while a
        // miss or a save is a huge reprieve that swings momentum back to the defending side.
        Swing(towardHome: isHome, weight: 0.5, minute);
        Swing(towardHome: outcome == PenaltyOutcome.Scored ? isHome : !isHome,
            weight: outcome == PenaltyOutcome.Scored ? 0.7 : 0.6, minute);

        if (controversial)
        {
            _badCalls.Add(new BadCallEvent(minute, BadCallType.WrongPenaltyAwarded, attackTeam.Code,
                defendTeam.Code, VarChecked: rng.NextDouble() < 0.5, taker.Name,
                $"Penalty to {attackTeam.Name} — {MistakeNarratives.BadCall(BadCallType.WrongPenaltyAwarded, ref rng)}"));
        }
    }

    public void ShowCard(ref Xoshiro256 rng, bool isHome, int minute, bool forceRed)
    {
        var team = isHome ? _home : _away;
        var onPitch = isHome ? _homeOnPitch : _awayOnPitch;
        if (onPitch.Count <= 7)
        {
            return; // A team cannot go below 7 players; stop sending off.
        }

        var player = PickFouler(ref rng, onPitch);

        // Did the referee get this card wrong (harsh / mistaken identity)? Tags the existing card — it
        // is still shown, so the cards/match calibration is unchanged.
        bool wrongCall = Distributions.Chance(ref rng, _g.Events.WrongCardShare);

        if (forceRed)
        {
            _cards.Add(new CardEvent(minute, team.Code, player.Id, player.Name, IsRed: true, IsSecondYellow: false,
                Controversial: wrongCall, Reason: CardNarratives.DirectRed(ref rng)));
            if (wrongCall)
            {
                _badCalls.Add(new BadCallEvent(minute, BadCallType.WrongCard, string.Empty, team.Code,
                    rng.NextDouble() < 0.5, player.Name,
                    $"Red for {player.Name} — {MistakeNarratives.BadCall(BadCallType.WrongCard, ref rng)}"));
            }

            SendOff(isHome, player, minute);
            return;
        }

        // Second-yellow dampening: a referee often books a different (un-booked) player rather than show
        // a second yellow — but real WC dismissals are MAJORITY second yellows, so we retarget only ~70%
        // of the time (not 82%), letting two-yellow reds be the larger share while staying near ~0.1 total.
        if (_yellowCount.ContainsKey(player.Id) && rng.NextDouble() > 0.30)
        {
            var fresh = onPitch.Where(pl => !_yellowCount.ContainsKey(pl.Id)).ToList();
            if (fresh.Count > 0)
            {
                player = fresh[rng.NextInt(fresh.Count)];
            }
            else
            {
                return; // everyone is booked — skip this caution rather than force a second-yellow red
            }
        }

        // Yellow (possibly a second yellow → red). A side chasing the game late often gets booked out
        // of frustration — re-label the reason accordingly (the card itself is unchanged, so card
        // calibration is untouched).
        _cards.Add(new CardEvent(minute, team.Code, player.Id, player.Name, IsRed: false, IsSecondYellow: false,
            Controversial: wrongCall, Reason: YellowReason(ref rng, isHome, minute)));
        if (wrongCall)
        {
            _badCalls.Add(new BadCallEvent(minute, BadCallType.WrongCard, string.Empty, team.Code,
                rng.NextDouble() < 0.5, player.Name,
                $"Yellow for {player.Name} — {MistakeNarratives.BadCall(BadCallType.WrongCard, ref rng)}"));
            Swing(towardHome: !isHome, weight: 0.3, minute); // a harsh booking riles one side, lifts the other
        }

        int count = _yellowCount.TryGetValue(player.Id, out int c) ? c + 1 : 1;
        _yellowCount[player.Id] = count;
        if (count >= 2)
        {
            _cards.Add(new CardEvent(minute, team.Code, player.Id, player.Name, IsRed: true, IsSecondYellow: true,
                Reason: CardNarratives.SecondYellow(ref rng)));
            SendOff(isHome, player, minute);
        }
    }

    public void Injure(ref Xoshiro256 rng, bool isHome, int minute)
    {
        var team = isHome ? _home : _away;
        var onPitch = isHome ? _homeOnPitch : _awayOnPitch;
        var bench = isHome ? _homeBench : _awayBench;
        var injured = PickByInjuryProneness(ref rng, onPitch);

        // Realistic severity mix: the vast majority are knocks shaken off or treated on the pitch;
        // strains/sprains that cost a few weeks are the minority; and serious injuries (fractures,
        // ruptured ligaments) are genuinely rare — only ~3% of injuries, i.e. a couple across a whole
        // tournament, not one every few games.
        double sev = rng.NextDouble();
        InjurySeverity severity = sev < 0.75 ? InjurySeverity.Knock
            : sev < 0.97 ? InjurySeverity.Minor : InjurySeverity.Major;

        // Diagnose it specifically — body part, exact diagnosis and an expected lay-off in days.
        var (bodyPart, diagnosis, recoveryDays) = InjuryCatalog.Diagnose(severity, ref rng);

        bool couldReplace = true;
        if (severity != InjurySeverity.Knock)
        {
            int subsUsed = isHome ? _homeSubs : _awaySubs;
            if (subsUsed < DetailedMatchSimulator.MaxSubs && bench.Count > 0)
            {
                // Bring on a replacement (prefer same position).
                var replacement = bench.FirstOrDefault(b => b.Position == injured.Position) ?? bench[0];
                bench.Remove(replacement);
                onPitch.Remove(injured);
                onPitch.Add(replacement);
                _offAtMinute[injured.Id] = minute;
                _entryMinute[replacement.Id] = minute;
                if (isHome) _homeSubs++; else _awaySubs++;
                _substitutions.Add(new SubstitutionEvent(minute, team.Code, injured.Id, injured.Name, replacement.Id, replacement.Name, Injury: true));
            }
            else
            {
                // No subs left: the team plays a man down (light strength hit).
                couldReplace = false;
                onPitch.Remove(injured);
                _offAtMinute[injured.Id] = minute;
                AdjustStrength(isHome, -6);
            }
        }

        _injuries.Add(new InjuryEvent(minute, team.Code, injured.Id, injured.Name, severity, couldReplace,
            bodyPart, diagnosis, recoveryDays));
    }

    /// <summary>
    /// A routine/tactical substitution: bring a fresh bench player on for an outfield player (managers
    /// refresh attack/midfield in the second half). Shares the 5-sub cap with injury subs; the keeper
    /// is never tactically replaced. Updates minutes so the bench actually plays and starters don't all
    /// finish 90'.
    /// </summary>
    public void TacticalSub(ref Xoshiro256 rng, bool isHome, int minute)
    {
        int subsUsed = isHome ? _homeSubs : _awaySubs;
        if (subsUsed >= DetailedMatchSimulator.MaxSubs)
        {
            return;
        }

        var onPitch = isHome ? _homeOnPitch : _awayOnPitch;
        var bench = isHome ? _homeBench : _awayBench;
        if (bench.Count == 0)
        {
            return;
        }

        var outfield = onPitch.Where(pl => pl.Position != Position.GK).ToList();
        if (outfield.Count == 0)
        {
            return;
        }

        var off = outfield[rng.NextInt(outfield.Count)];
        var on = bench.FirstOrDefault(b => b.Position == off.Position) ?? bench[0];
        bench.Remove(on);
        onPitch.Remove(off);
        onPitch.Add(on);
        _offAtMinute[off.Id] = minute;
        _entryMinute[on.Id] = minute;
        if (isHome) _homeSubs++; else _awaySubs++;
        _substitutions.Add(new SubstitutionEvent(minute, (isHome ? _home : _away).Code, off.Id, off.Name, on.Id, on.Name, Injury: false));
        Swing(towardHome: isHome, weight: 0.3, minute); // fresh legs against tiring opponents — a momentum burst
    }

    /// <summary>
    /// A mistake that did NOT cost a goal — a heavy touch, a sliced clearance, a keeper spilling one
    /// that was scrambled away. Pure colour: it adds no goal and no shot, so calibration is untouched.
    /// </summary>
    public void CommitUnpunishedError(ref Xoshiro256 rng, bool isHome, int minute)
    {
        var team = isHome ? _home : _away;
        var onPitch = isHome ? _homeOnPitch : _awayOnPitch;
        if (onPitch.Count == 0)
        {
            return;
        }

        bool keeperSlip = Distributions.Chance(ref rng, 0.18);
        Player culprit;
        ErrorKind kind;
        string desc;
        if (keeperSlip)
        {
            culprit = FindKeeper(onPitch) ?? onPitch[0];
            kind = ErrorKind.GoalkeeperError;
            desc = MistakeNarratives.GoalkeeperError(ledToGoal: false, ref rng);
        }
        else
        {
            culprit = PickByPosition(ref rng, onPitch, Position.DEF);
            kind = ErrorKind.DefensiveError;
            desc = MistakeNarratives.DefensiveError(ledToGoal: false, ref rng);
        }

        _errors.Add(new PlayerErrorEvent(minute, team.Code, culprit.Id, culprit.Name, kind, LedToGoal: false, desc));
    }

    /// <summary>
    /// A clear refereeing mistake with nothing else attached: a stonewall penalty waved away, a
    /// goal wrongly chalked off, or a red-card offence the referee missed. Adds no goal/card/penalty,
    /// so it never moves the calibrated totals — it is the controversy, not the consequence.
    /// </summary>
    public void CommitRefereeMistake(ref Xoshiro256 rng, int minute)
    {
        bool homeWronged = Distributions.Chance(ref rng, 0.5);
        var wronged = homeWronged ? _home : _away;
        var benefited = homeWronged ? _away : _home;
        bool varChecked = rng.NextDouble() < 0.5;
        double r = rng.NextDouble();

        if (r < 0.34)
        {
            _badCalls.Add(new BadCallEvent(minute, BadCallType.PenaltyDenied, benefited.Code, wronged.Code,
                varChecked, string.Empty,
                $"{wronged.Name} denied a penalty — {MistakeNarratives.BadCall(BadCallType.PenaltyDenied, ref rng)}"));
        }
        else if (r < 0.62)
        {
            _badCalls.Add(new BadCallEvent(minute, BadCallType.GoalWronglyDisallowed, benefited.Code, wronged.Code,
                varChecked, string.Empty,
                $"{wronged.Name} goal {MistakeNarratives.BadCall(BadCallType.GoalWronglyDisallowed, ref rng)}"));
        }
        else
        {
            // A red-card offence the referee missed — the offending side got away with one.
            var offenders = homeWronged ? _awayOnPitch : _homeOnPitch;
            var culprit = PickFouler(ref rng, offenders);
            _badCalls.Add(new BadCallEvent(minute, BadCallType.MissedCard, benefited.Code, wronged.Code,
                varChecked, culprit.Name,
                $"{culprit.Name} — {MistakeNarratives.BadCall(BadCallType.MissedCard, ref rng)}"));
        }

        // A bad call deflates the wronged side and emboldens the side that got away with it.
        Swing(towardHome: !homeWronged, weight: 0.5, minute);
    }

    /// <summary>
    /// Turn a keeper's save count into individual NOTABLE saves (rating ≥ 6) with spectacularity
    /// ratings — most saves are routine and stay just a box-score number; the highlights become events.
    /// A better keeper and a harder shot push the rating up; ≥ 7.5 is an "amazing" worldie save.
    /// </summary>
    private void GenerateSaves(List<SaveEvent> list, Team team, List<Player> onPitch, int count, ref Xoshiro256 rng)
    {
        if (count <= 0)
        {
            return;
        }

        var keeper = FindKeeper(onPitch);
        string keeperId = keeper?.Id ?? team.Code + "-GK";
        string keeperName = keeper?.Name ?? "Goalkeeper";
        double skill = keeper is null ? 50 : _p.EffectiveAttributes(keeper).Goalkeeping;

        for (int i = 0; i < count; i++)
        {
            double difficulty = rng.NextDouble();
            double rating = Math.Clamp(2.0 + difficulty * 5.5 + (skill - 55) / 45.0 * 1.5 + rng.NextDouble(), 1, 10);
            if (rating < 6.0)
            {
                continue; // routine save — not a highlight
            }

            double distance = 6 + difficulty * 18 + rng.NextDouble() * 4;
            int minute = 1 + rng.NextInt(Math.Max(1, LastMinute));
            list.Add(new SaveEvent(minute, team.Code, keeperId, keeperName, rating, rating >= 7.5, distance));
        }
    }

    /// <summary>
    /// Adds the match-day "atmosphere" events once play is done: hydration / cooling breaks if it is hot,
    /// and any on-field flashpoints (more likely with red cards, a tight scoreline and the heat). Called
    /// after the final whistle, so it never disturbs the calibrated goal/card/penalty event stream.
    /// </summary>
    public void GenerateAtmosphere(ref Xoshiro256 rng, int temperatureC)
    {
        _temperatureC = temperatureC;

        // FIFA mandates cooling breaks in the heat — roughly the 30th minute of each half.
        if (temperatureC >= 30)
        {
            _coolingBreaks.Add(new CoolingBreak(30, temperatureC));
            _coolingBreaks.Add(new CoolingBreak(75, temperatureC));
        }

        // Flashpoints are CAUSED by something specific — a wild challenge, a contentious decision, a
        // provocative goal — never out of nowhere. Build the candidate triggers from what actually
        // happened in the match, each with a realistic chance of boiling over (hot weather shortens
        // fuses), then roll them. A clean game produces no triggers and therefore no scuffles.
        double heat = temperatureC >= 33 ? 1.3 : temperatureC >= 30 ? 1.1 : 1.0;
        var triggers = new List<(int Minute, double Chance, ConfrontationLevel Base, string Cause)>();

        foreach (var c in _cards.Where(c => c.IsRed))
        {
            triggers.Add((c.Minute, 0.45, ConfrontationLevel.FaceOff,
                $"{c.PlayerName}'s ({TeamNameOf(c.TeamCode)}) red card — {ReasonOr(c.Reason, "a reckless, X-rated lunge")}"));
        }

        foreach (var bc in _badCalls)
        {
            triggers.Add((bc.Minute, 0.22, ConfrontationLevel.Handbags,
                $"furious protests at the officials over {LowerFirst(bc.Description)}"));
        }

        foreach (var pen in _penalties.Where(x => x.Controversial))
        {
            triggers.Add((pen.Minute, 0.20, ConfrontationLevel.Handbags,
                $"the soft penalty handed to {TeamNameOf(pen.TeamCode)} — the other side incensed"));
        }

        foreach (var c in _cards.Where(c => !c.IsRed && c.Minute >= 65))
        {
            triggers.Add((c.Minute, 0.10, ConfrontationLevel.Handbags,
                $"{c.PlayerName}'s cynical, frustrated foul — {ReasonOr(c.Reason, "a needless, niggly challenge")}"));
        }

        foreach (var gl in _goals.Where(g => g.Minute >= 80 && !g.IsOwnGoal))
        {
            triggers.Add((gl.Minute, 0.09, ConfrontationLevel.Handbags,
                $"{gl.ScorerName} celebrating the late goal right in the faces of the opposition"));
        }

        if (_cards.Count >= 5 && Math.Abs(HomeGoals - AwayGoals) <= 1)
        {
            triggers.Add((60 + rng.NextInt(30), 0.10, ConfrontationLevel.Handbags,
                "a chippy, niggly contest finally boiling over after a string of crunching tackles"));
        }

        // Off-the-ball niggle: sometimes a player simply winds up an opponent — a sly elbow, a stamp, a
        // shove after the whistle, a few choice words — and it flares up, no other trigger required.
        if (_homeOnPitch.Count > 0 && _awayOnPitch.Count > 0 && rng.NextDouble() < 0.14)
        {
            bool aggHome = rng.NextDouble() < 0.5;
            var aggPitch = aggHome ? _homeOnPitch : _awayOnPitch;
            var vicPitch = aggHome ? _awayOnPitch : _homeOnPitch;
            var aggressor = aggPitch[rng.NextInt(aggPitch.Count)];
            var victim = vicPitch[rng.NextInt(vicPitch.Count)];
            string act = OffTheBallActs[rng.NextInt(OffTheBallActs.Length)];
            triggers.Add((20 + rng.NextInt(65), 0.55, ConfrontationLevel.FaceOff,
                $"{aggressor.Name} ({(aggHome ? _home.Name : _away.Name)}) {act} {victim.Name} ({(aggHome ? _away.Name : _home.Name)}) off the ball"));
        }

        int made = 0;
        foreach (var t in triggers.OrderBy(x => x.Minute))
        {
            if (made >= 2 || !Distributions.Chance(ref rng, Math.Min(0.65, t.Chance * heat)))
            {
                continue;
            }

            double sev = rng.NextDouble();
            ConfrontationLevel level = sev < 0.55 ? t.Base
                : sev < 0.86 ? ConfrontationLevel.Scuffle
                : ConfrontationLevel.Brawl;
            bool bench = level == ConfrontationLevel.Brawl && rng.NextDouble() < 0.40;
            string core = level switch
            {
                ConfrontationLevel.Handbags => ConfrontationNarratives.Handbags(ref rng),
                ConfrontationLevel.FaceOff => ConfrontationNarratives.FaceOff(ref rng),
                ConfrontationLevel.Scuffle => ConfrontationNarratives.Shoving(ref rng),
                _ => ConfrontationNarratives.MassConfrontation(ref rng),
            };
            string benchPart = bench ? " " + ConfrontationNarratives.BenchInvolved(ref rng) : string.Empty;
            string desc = $"Sparked by {t.Cause} — {core}.{benchPart} {ConfrontationNarratives.RefCalms(ref rng)}";
            _confrontations.Add(new Confrontation(t.Minute, level, bench, t.Cause, desc));
            made++;
        }
    }

    private string TeamNameOf(string code) => string.Equals(code, _home.Code, StringComparison.OrdinalIgnoreCase) ? _home.Name : _away.Name;

    private static string ReasonOr(string reason, string fallback) => string.IsNullOrWhiteSpace(reason) ? fallback : reason;

    private static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    private static readonly string[] OffTheBallActs =
    {
        "elbowing", "stamping on", "shoving", "barging into", "needling and niggling at",
        "raking the studs down on", "tugging the shirt of", "throwing an arm at",
        "shoulder-barging", "kicking out at", "having a sly dig at", "winding up",
        "going through the back of", "squaring up to",
    };

    public MatchResult Build(Stage stage, MatchMethod method, ShootoutResult? shootout, ref Xoshiro256 rng)
    {
        if (LastMinute == 0)
        {
            LastMinute = 90;
        }

        // Possession is computed once so the two sides sum to 100%.
        double homePossession = Math.Clamp(
            50 + 0.45 * (_homeStrengthBase - _awayStrengthBase) + (rng.NextDouble() - 0.5) * 6, 25, 75);
        var homeBox = BuildBoxScore(ref rng, isHome: true, homePossession);
        var awayBox = BuildBoxScore(ref rng, isHome: false, 100 - homePossession);

        // Reconcile saves against the opponent's actual on-target shots that didn't score.
        int homeShotGoals = _goals.Count(x => x.TeamCode == _home.Code && !x.IsOwnGoal);
        int awayShotGoals = _goals.Count(x => x.TeamCode == _away.Code && !x.IsOwnGoal);
        homeBox = homeBox with { Saves = homeBox.Saves + Math.Max(0, awayBox.ShotsOnTarget - awayShotGoals) };
        awayBox = awayBox with { Saves = awayBox.Saves + Math.Max(0, homeBox.ShotsOnTarget - homeShotGoals) };

        // Notable goalkeeper saves (highlights), with spectacularity ratings — the keeper's vergazo.
        var saveEvents = new List<SaveEvent>();
        GenerateSaves(saveEvents, _home, _homeOnPitch, homeBox.Saves, ref rng);
        GenerateSaves(saveEvents, _away, _awayOnPitch, awayBox.Saves, ref rng);
        saveEvents.Sort((a, b) => a.Minute.CompareTo(b.Minute));

        string winner = method == MatchMethod.Penalties
            ? (shootout!.Value.HomeWon ? _home.Code : _away.Code)
            : HomeGoals > AwayGoals ? _home.Code
            : AwayGoals > HomeGoals ? _away.Code
            : string.Empty;

        // Attach a celebration to each goal, drawn here (post-play) so the calibrated event stream is untouched.
        var goals = new List<GoalEvent>(_goals.Count);
        foreach (var g in _goals.OrderBy(x => x.Minute))
        {
            goals.Add(g with { Celebration = CelebrationNarratives.For(g.Type, g.Vergazo, g.Minute, g.IsOwnGoal, g.IsPenalty, ref rng) });
        }
        var cards = _cards.OrderBy(x => x.Minute).ToList();
        var penalties = _penalties.OrderBy(x => x.Minute).ToList();
        var injuries = _injuries.OrderBy(x => x.Minute).ToList();
        var upset = ComputeUpset(winner);

        return new MatchResult
        {
            Fidelity = Fidelity.Detailed,
            Stage = stage,
            HomeCode = _home.Code,
            AwayCode = _away.Code,
            HomeName = _home.Name,
            AwayName = _away.Name,
            HomeGoals = HomeGoals,
            AwayGoals = AwayGoals,
            Method = method,
            WinnerCode = winner,
            HomePens = shootout?.HomeScored ?? 0,
            AwayPens = shootout?.AwayScored ?? 0,
            ShootoutRounds = shootout?.Rounds ?? 0,
            ShootoutKicks = shootout?.Kicks ?? Array.Empty<ShootoutKick>(),
            FirstHalfStoppage = FirstHalfStoppage,
            SecondHalfStoppage = SecondHalfStoppage,
            TemperatureC = _temperatureC,
            CoolingBreaks = _coolingBreaks,
            Confrontations = _confrontations,
            Goals = goals,
            Cards = cards,
            Penalties = penalties,
            Injuries = injuries,
            HomeBox = homeBox,
            AwayBox = awayBox,
            SaveEvents = saveEvents,
            Substitutions = _substitutions.OrderBy(x => x.Minute).ToList(),
            Errors = _errors.OrderBy(x => x.Minute).ToList(),
            BadCalls = _badCalls.OrderBy(x => x.Minute).ToList(),
            Upset = upset,
            Miracle = _miracle,
            HomeLineup = _home.Squad.Where(p => _entryMinute.ContainsKey(p.Id)).Select(p => p.Id).ToList(),
            AwayLineup = _away.Squad.Where(p => _entryMinute.ContainsKey(p.Id)).Select(p => p.Id).ToList(),
            Minutes = BuildMinutes(),
        };
    }

    // --- internals ---

    /// <summary>
    /// Pre-match odds (from base strengths, before any form/red-card adjustment) vs. the actual
    /// result, plus the 1–10 miracle rating measuring how surprising the outcome was.
    /// </summary>
    private UpsetInfo ComputeUpset(string winnerCode)
    {
        var (lh, la) = MatchModel.ExpectedGoals(_homeStrengthBase, _awayStrengthBase, _g, _neutral);
        // Marginalise over the per-match form AND tempo factors so these pre-match odds match what the
        // sampler draws (each team's goals carry both log-normal multipliers; their variances add).
        double sigmaEff = Math.Sqrt(_g.UpsetVariance * _g.UpsetVariance + _g.MatchTempoVariance * _g.MatchTempoVariance);
        var grid = MatchModel.ScoreGridWithForm(lh, la, _g.DrawCoupling, sigmaEff);
        var (pHome, pDraw, pAway) = MatchModel.OutcomeProbabilities(grid);
        double pScore = MatchModel.ScoreProbability(grid, HomeGoals, AwayGoals);

        // Probability of the outcome that actually happened. When a knockout was level and decided
        // by ET/shootout, the winner advanced from a "draw", so credit ~half the draw probability.
        bool level = HomeGoals == AwayGoals;
        double pOutcome =
            winnerCode == _home.Code ? pHome + (level ? 0.5 * pDraw : 0.0)
            : winnerCode == _away.Code ? pAway + (level ? 0.5 * pDraw : 0.0)
            : pDraw;

        return new UpsetInfo(pHome, pDraw, pAway, pScore, MatchModel.MiracleRating(pOutcome, pScore));
    }

    private void AddGoal(bool isHome, GoalEvent goal)
    {
        _goals.Add(goal);
        if (isHome) HomeGoals++; else AwayGoals++;
    }

    private void SendOff(bool isHome, Player player, int minute)
    {
        var onPitch = isHome ? _homeOnPitch : _awayOnPitch;
        onPitch.Remove(player);
        _sentOff.Add(player.Id);
        _offAtMinute[player.Id] = minute;
        AdjustStrength(isHome, -10);
        Swing(towardHome: !isHome, weight: 0.8, minute); // down to ten men — momentum swings to the opponent
    }

    /// <summary>The live momentum multiplier on a team's open-play scoring hazard (1.0 = neutral).</summary>
    public double MomentumFactor(bool isHome) =>
        Math.Clamp(1.0 + (isHome ? _swing : -_swing), 0.35, 1.9);

    /// <summary>Mean-revert the momentum swing toward neutral; called once per simulated minute.</summary>
    public void DecayMomentum() => _swing *= _g.MomentumDecayPerMinute;

    /// <summary>
    /// Shock the momentum toward one side. <paramref name="weight"/> scales the configured
    /// <see cref="GlobalParameters.MomentumStrength"/>; an early-match shock (a goal in the first
    /// 20 minutes, say) lands harder. The swing is capped so no side ever runs away completely.
    /// </summary>
    private void Swing(bool towardHome, double weight, int minute)
    {
        double early = 1.0 + 0.7 * Math.Clamp((30 - minute) / 30.0, 0.0, 1.0); // up to +70% for very early events
        double delta = _g.MomentumStrength * weight * early;
        _swing = Math.Clamp(_swing + (towardHome ? delta : -delta), -0.45, 0.45);
    }

    private void AdjustStrength(bool isHome, double delta)
    {
        if (isHome)
        {
            _homeStrength = Math.Max(10, _homeStrength + delta);
        }
        else
        {
            _awayStrength = Math.Max(10, _awayStrength + delta);
        }

        RecomputeLambdas();
    }

    private void RecomputeLambdas()
    {
        var (lh, la) = MatchModel.ExpectedGoals(_homeStrength, _awayStrength, _g, _neutral);
        lh *= _homeForm * _tempo; // per-match form/luck ("any given day") → upsets, plus the shared tempo
        la *= _awayForm * _tempo;
        var ev = _g.Events;
        double homePenGoals = ev.PenaltiesPerMatch * HomeAttackShare * ev.PenaltyConversionBase;
        double awayPenGoals = ev.PenaltiesPerMatch * AwayAttackShare * ev.PenaltyConversionBase;
        HomeOpenPlayLambda = Math.Max(0.05, lh - homePenGoals);
        AwayOpenPlayLambda = Math.Max(0.05, la - awayPenGoals);
    }

    private IReadOnlyDictionary<string, int> BuildMinutes()
    {
        var minutes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (id, entry) in _entryMinute)
        {
            int end = _offAtMinute.TryGetValue(id, out int off) ? off : LastMinute;
            minutes[id] = Math.Clamp(end - entry, 0, 130);
        }

        return minutes;
    }

    private TeamBoxScore BuildBoxScore(ref Xoshiro256 rng, bool isHome, double possession)
    {
        var ev = _g.Events;
        string code = isHome ? _home.Code : _away.Code;
        double share = isHome ? HomeAttackShare : AwayAttackShare;
        double indiscipline = isHome ? HomeIndiscipline : AwayIndiscipline;
        int goals = isHome ? HomeGoals : AwayGoals;

        int shotGoals = _goals.Count(x => x.TeamCode == code && !x.IsOwnGoal);
        // Shots come from an expected-shots baseline scaled by attack share (so even a goalless side
        // still attempts a realistic number), plus a small bonus per goal, never fewer than the goals.
        double expShots = ev.ShotsBaselinePerTeam * (2.0 * share) * (0.8 + rng.NextDouble() * 0.4) + shotGoals * 0.7;
        int shots = Math.Max(shotGoals, (int)Math.Round(expShots));
        int shotsOnTarget = Math.Clamp((int)Math.Round(shots * ev.ShotsOnTargetFraction), shotGoals, shots);

        int corners = (int)Math.Round(ev.CornersPerMatch * share * (0.7 + rng.NextDouble() * 0.6));
        int fouls = (int)Math.Round(ev.FoulsPerMatch / 2.0 * indiscipline * (0.7 + rng.NextDouble() * 0.6));
        int yellows = _cards.Count(c => c.TeamCode == code && !c.IsRed);
        int reds = _cards.Count(c => c.TeamCode == code && c.IsRed);
        fouls = Math.Max(fouls, yellows + reds);
        int offsides = (int)Math.Round(ev.OffsidesPerMatch / 2.0 * (0.6 + rng.NextDouble() * 0.8));

        // Saves: only the penalties this keeper saved here; the shot-saves (opponent on-target
        // non-goals) are added in Build() from the OPPONENT'S ACTUAL box so the numbers reconcile
        // (a team's shots-on-target = its goals-from-shots + the other keeper's shot-saves).
        int penSaves = isHome ? _homePenaltySaves : _awayPenaltySaves;

        // Restart stats. Throw-ins are roughly even with a mild possession lean; goal kicks go to the
        // side under more pressure (∝ the opponent's attacking share). Neither is a calibration target.
        double throwShare = 0.5 + (possession / 100.0 - 0.5) * 0.3;
        int throwIns = (int)Math.Round(ev.ThrowInsPerMatch * throwShare * (0.85 + rng.NextDouble() * 0.3));
        int goalKicks = (int)Math.Round(ev.GoalKicksPerMatch * (1.0 - share) * (0.7 + rng.NextDouble() * 0.6));

        return new TeamBoxScore(code, goals, shots, shotsOnTarget, Math.Round(possession, 1),
            corners, fouls, offsides, penSaves, yellows, reds, throwIns, goalKicks);
    }

    private static (List<Player> OnPitch, List<Player> Bench) SelectLineup(Team team, SimulationParameters p)
    {
        // The most-probable starting XI for the team's formation, excluding unavailable players
        // (shared with the line-up shown in the UI).
        var projected = LineupProjector.Project(team, p.Formation(team), p.IsAvailable, p.PreferredStarters);
        return (projected.Xi.ToList(), projected.Bench.ToList());
    }

    private double Indiscipline(IReadOnlyList<Player> onPitch)
    {
        // Population mean of (100 - discipline) ≈ 40 (see SyntheticSquadGenerator); normalise to ~1.0.
        double sum = 0;
        int n = 0;
        foreach (var pl in onPitch)
        {
            if (pl.Position == Position.GK)
            {
                continue;
            }

            sum += 100 - _p.EffectiveAttributes(pl).Discipline;
            n++;
        }

        double mean = n == 0 ? 40 : sum / n;
        return Math.Clamp(mean / 40.0, 0.4, 2.0);
    }

    private Player PickScorer(ref Xoshiro256 rng, List<Player> onPitch)
    {
        var weights = new double[onPitch.Count];
        for (int i = 0; i < onPitch.Count; i++)
        {
            var p = onPitch[i];
            double pos = p.Position switch
            {
                Position.FWD => 1.0,
                Position.MID => 0.55,
                Position.DEF => 0.18,
                _ => 0.02,
            };
            weights[i] = Math.Max(0.01, _p.EffectiveAttributes(p).Finishing) * pos;
        }

        return onPitch[Distributions.SampleWeighted(ref rng, weights)];
    }

    private Player? PickAssister(ref Xoshiro256 rng, List<Player> onPitch, string scorerId)
    {
        var weights = new double[onPitch.Count];
        for (int i = 0; i < onPitch.Count; i++)
        {
            var p = onPitch[i];
            if (p.Id == scorerId)
            {
                weights[i] = 0;
                continue;
            }

            double pos = p.Position switch
            {
                Position.MID => 1.0,
                Position.FWD => 0.7,
                Position.DEF => 0.45,
                _ => 0.05,
            };
            weights[i] = Math.Max(0.01, _p.EffectiveAttributes(p).Creativity) * pos;
        }

        int idx = Distributions.SampleWeighted(ref rng, weights);
        return onPitch[idx].Id == scorerId ? null : onPitch[idx];
    }

    private Player PickFouler(ref Xoshiro256 rng, List<Player> onPitch)
    {
        var weights = new double[onPitch.Count];
        for (int i = 0; i < onPitch.Count; i++)
        {
            var p = onPitch[i];
            double pos = p.Position switch
            {
                Position.DEF => 1.1,
                Position.MID => 1.0,
                Position.FWD => 0.7,
                _ => 0.3,
            };
            weights[i] = Math.Max(1, 100 - _p.EffectiveAttributes(p).Discipline) * pos;
        }

        return onPitch[Distributions.SampleWeighted(ref rng, weights)];
    }

    private Player PickByInjuryProneness(ref Xoshiro256 rng, List<Player> onPitch)
    {
        var weights = new double[onPitch.Count];
        for (int i = 0; i < onPitch.Count; i++)
        {
            weights[i] = Math.Max(1, _p.EffectiveAttributes(onPitch[i]).InjuryProneness);
        }

        return onPitch[Distributions.SampleWeighted(ref rng, weights)];
    }

    private Player PickByPosition(ref Xoshiro256 rng, List<Player> onPitch, Position preferred)
    {
        var pool = onPitch.Where(p => p.Position == preferred).ToList();
        if (pool.Count == 0)
        {
            pool = onPitch;
        }

        return pool[rng.NextInt(pool.Count)];
    }

    /// <summary>The on-pitch outfield players in shootout-taking order (best finisher first), by name.</summary>
    public IReadOnlyList<string> PenaltyTakers(bool isHome)
    {
        var onPitch = isHome ? _homeOnPitch : _awayOnPitch;
        return onPitch
            .Where(pl => pl.Position != Position.GK)
            .OrderByDescending(pl => _p.EffectiveAttributes(pl).Finishing)
            .Select(pl => pl.Name)
            .ToList();
    }

    private Player PickPenaltyTaker(List<Player> onPitch)
    {
        // Designated taker = best finisher on the pitch.
        Player best = onPitch[0];
        double bestFinish = -1;
        foreach (var p in onPitch)
        {
            double f = _p.EffectiveAttributes(p).Finishing;
            if (f > bestFinish)
            {
                bestFinish = f;
                best = p;
            }
        }

        return best;
    }

    private static Player? FindKeeper(List<Player> onPitch)
    {
        foreach (var p in onPitch)
        {
            if (p.Position == Position.GK)
            {
                return p;
            }
        }

        return onPitch.Count > 0 ? onPitch[0] : null;
    }

    private static GoalType ClassifyGoal(ref Xoshiro256 rng, double distance)
    {
        if (distance >= 25)
        {
            // A long-range strike: occasionally a direct free kick, very rarely a bicycle kick.
            double r = rng.NextDouble();
            if (r < 0.04) return GoalType.BicycleKick;
            if (r < 0.30) return GoalType.FreeKick;
            return GoalType.LongRange;
        }

        if (distance >= 19)
        {
            // Edge-of-box range: some direct free kicks, otherwise open play. (Free kicks are always
            // struck from distance — never a tap-in.)
            return rng.NextDouble() < 0.18 ? GoalType.FreeKick : GoalType.OpenPlay;
        }

        double r2 = rng.NextDouble();
        if (r2 < 0.004)
        {
            return GoalType.BicycleKick; // genuinely rare spectacular finish in/around the box
        }

        if (r2 < 0.20)
        {
            return GoalType.Header; // close-range, typically from a cross
        }

        return GoalType.OpenPlay;
    }

    /// <summary>Number of defenders beaten in the build-up (right-skewed: most goals beat none).</summary>
    private static int SampleDefendersPassed(ref Xoshiro256 rng)
    {
        double r = rng.NextDouble();
        if (r < 0.55) return 0;
        if (r < 0.80) return 1;
        if (r < 0.92) return 2;
        if (r < 0.97) return 3;
        if (r < 0.99) return 4;
        return 5;
    }

    /// <summary>
    /// The "vergazo" spectacularity rating (1–10): how great a goal is. The factors are modelled on
    /// the criteria FIFA uses for the Puskás Award (best goal): technical difficulty / beauty,
    /// long-range distance, acrobatic actions, solo runs past defenders, collective team moves,
    /// audacity, match importance — and explicitly NOT luck (own goals and deflection-style goals
    /// score low). Concretely it blends:
    /// <list type="bullet">
    /// <item>technique/style (bicycle kick &gt; long-range &gt; free kick &gt; open play &gt; header)</item>
    /// <item>shot distance (Ibrahimović's 30 m+ overhead is the archetype — distance scales to ~30 m)</item>
    /// <item>solo run: defenders beaten in the build-up</item>
    /// <item>collective build-up: a slick assist from a creative playmaker</item>
    /// <item>clutch timing (late winners / extra-time goals = greater importance)</item>
    /// <item>quality of the keeper beaten, finishing quality, plus a flair/execution roll</item>
    /// </list>
    /// Each style has a ceiling so that <b>only a top-class long-range bicycle kick can reach a
    /// perfect 10/10</b>; the best non-bicycle goals top out at ~9.5. Own goals are always ≤ 3 and
    /// penalties stay low (they are "lucky"/undramatic by the Puskás criteria).
    /// </summary>
    private static double ComputeVergazo(
        GoalType type, double distance, int defendersPassed, int minute, double assisterCreativity,
        double keeperSkill, double scorerFinishing, ref Xoshiro256 rng)
    {
        // Own goals and penalties are never spectacular.
        if (type == GoalType.OwnGoal)
        {
            return Math.Round(Math.Clamp(0.5 + rng.NextDouble() * 2.5, 1.0, 3.0), 1);
        }

        if (type == GoalType.Penalty)
        {
            return Math.Round(Math.Clamp(1.0 + (scorerFinishing - 50) / 40.0 + rng.NextDouble() * 1.2, 1.0, 4.0), 1);
        }

        double styleScore = type switch
        {
            GoalType.BicycleKick => 3.0,
            GoalType.LongRange => 2.3,
            GoalType.FreeKick => 1.8,
            GoalType.Header => 1.0,
            _ => 0.9, // open play
        };
        // Distance scales all the way to ~30 m, so only a genuine long-range strike maxes it out
        // (a close-range bicycle kick does NOT get the full distance bonus).
        double distanceScore = Math.Clamp((distance - 7.0) / 23.0, 0, 1) * 3.0;          // up to 3.0 at 30 m
        double defendersScore = Math.Min(defendersPassed, 5) / 5.0 * 2.0;                // up to 2.0 beating five
        // Build-up beauty: an unassisted solo goal, OR a slick assist from a creative playmaker (a
        // beautiful collective move) — both are Puskás-worthy, an ordinary tap-in pass is not.
        double buildUpScore = assisterCreativity < 0
            ? 0.8
            : Math.Clamp((assisterCreativity - 55.0) / 40.0, 0, 1) * 0.8;
        double clutchScore = minute >= 120 ? 1.0 : minute >= 90 ? 0.8 : minute >= 75 ? 0.4 : 0.0;
        double keeperScore = Math.Clamp((keeperSkill - 60) / 40.0, 0, 1) * 0.8;          // beating a good keeper
        double finishScore = Math.Clamp((scorerFinishing - 50) / 50.0, 0, 1) * 0.8;      // clean strike
        double flair = rng.NextDouble() * 1.0;                                           // execution variation

        double cap = type switch
        {
            GoalType.BicycleKick => 10.0,
            GoalType.LongRange => 9.5,
            GoalType.FreeKick => 8.7,
            GoalType.OpenPlay => 7.8,
            GoalType.Header => 7.5,
            _ => 7.0,
        };

        double raw = 1.0 + styleScore + distanceScore + defendersScore + buildUpScore
            + clutchScore + keeperScore + finishScore + flair;
        return Math.Round(Math.Clamp(raw, 1.0, cap), 1);
    }
}
