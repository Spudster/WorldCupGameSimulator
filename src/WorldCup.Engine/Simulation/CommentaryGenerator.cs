using System.Text;
using WorldCup.Engine.Random;
using WorldCup.Engine.Tournament;

namespace WorldCup.Engine.Simulation;

/// <summary>One spoken line of match commentary, attributed to a voice.</summary>
/// <param name="Minute">The match minute the line is spoken at (0 = pre-match).</param>
/// <param name="Speaker">"PBP" for the play-by-play commentator, "CO" for the colour analyst.</param>
/// <param name="Text">The spoken line.</param>
public sealed record CommentaryLine(int Minute, string Speaker, string Text);

/// <summary>
/// Turns a finished <see cref="MatchResult"/> into a play-by-play radio/TV commentary transcript: an
/// intro, kickoff, a called line for every event in chronological order (goals, penalties, cards with
/// their reason, errors, bad calls, injuries with the diagnosis, subs and great saves), half-time and
/// full-time summaries, and extra-time / shootout narration. Two voices — a play-by-play commentator
/// and a colour analyst. Fully deterministic for a given match (seeded from the match's own data), so
/// the same game always tells the same story; no network and no model calls.
/// </summary>
public static class CommentaryGenerator
{
    /// <summary>The play-by-play commentator voice id.</summary>
    public const string PlayByPlay = "PBP";

    /// <summary>The colour-analyst voice id.</summary>
    public const string Analyst = "CO";

    /// <summary>The crowd voice id (chants and reactions from the stands).</summary>
    public const string CrowdVoice = "CRD";

    private enum Kind { StandaloneError, Card, Penalty, Goal, Save, Injury, Sub, BadCall, CoolingBreak, Confrontation, NearMiss, Var }

    private sealed record Moment(int Minute, Kind Kind, int Order, object Payload);

    public static IReadOnlyList<CommentaryLine> Generate(MatchResult m)
    {
        var rng = new Xoshiro256(SeedFor(m));
        var lines = new List<CommentaryLine>();
        var crowd = new Crowd(m);
        int hg = 0, ag = 0; // running score as the story unfolds

        void Pbp(int min, string text) => lines.Add(new CommentaryLine(min, PlayByPlay, text));
        void Co(int min, string text) => lines.Add(new CommentaryLine(min, Analyst, text));
        void Crd(int min, string text) => lines.Add(new CommentaryLine(min, CrowdVoice, text));
        string Score() => $"{m.HomeName} {hg}, {m.AwayName} {ag}";
        string TeamName(string code) => string.Equals(code, m.HomeCode, StringComparison.OrdinalIgnoreCase) ? m.HomeName : m.AwayName;

        // --- Pre-match ---
        Pbp(0, $"{Punctuate(CommentaryPhrases.IntroOpener(ref rng))} It's {Stages.DisplayName(m.Stage).ToLowerInvariant()} at the 2026 World Cup: {m.HomeName} against {m.AwayName}.");
        Co(0, $"And what an atmosphere. {crowd.Summary}.");
        if (m.TemperatureC >= 30)
        {
            Co(0, $"Sweltering out there too — {m.TemperatureC}°C — so we'll get the mandatory cooling breaks, compulsory under the 2026 heat protocol.");
        }

        if (m.Weather is { } weather)
        {
            Co(0, WeatherNarratives.Mention(weather.Kind, ref rng));
        }

        Crd(0, crowd.PreMatch(ref rng));
        if (m.Miracle is { } mira)
        {
            Co(mira.Minute, $"And keep an eye on {mira.TeamName} here — there's a real belief about them today, {mira.Description}. Don't you dare rule out a shock.");
        }
        if (m.Upset is { } u)
        {
            string? favourite = u.PreMatchHomeWin > u.PreMatchAwayWin + 0.08 ? m.HomeName
                : u.PreMatchAwayWin > u.PreMatchHomeWin + 0.08 ? m.AwayName : null;
            string aside = CommentaryPhrases.AnalystAside(ref rng);
            Co(0, favourite is null
                ? $"Too close to call, this one. {aside}"
                : $"{favourite} come in as the favourites, but {Lower1(aside)}");
        }

        Pbp(0, CommentaryPhrases.Kickoff(ref rng));

        // --- Build the chronological event list ---
        var moments = BuildMoments(m);

        // Look up the player error that produced a given goal minute (to narrate the howler).
        PlayerErrorEvent? ErrorFor(int minute, ErrorKind kind) =>
            m.Errors.FirstOrDefault(e => e.LedToGoal && e.Minute == minute && e.Kind == kind);

        bool halfTimeDone = false, extraTimeDone = false;
        int lastSpokenMinute = 0;

        foreach (var mo in moments)
        {
            // Cross the half-time line.
            if (!halfTimeDone && mo.Minute > 45)
            {
                EmitHalfTime();
            }

            // Cross into extra time.
            if (!extraTimeDone && m.Method != MatchMethod.Regulation && mo.Minute > 90)
            {
                EmitExtraTime();
            }

            // Occasional ambient line in a quiet spell — sometimes the commentator, sometimes a chant
            // rolling down from the stands; late and tight, the crowd roars their side forward.
            if (mo.Minute - lastSpokenMinute >= 16 && mo.Minute < 90)
            {
                int fm = (mo.Minute + lastSpokenMinute) / 2;
                if (fm >= 75 && Math.Abs(hg - ag) <= 1)
                {
                    // Late and tight: with a single goal in it the leaders' end is a bag of nerves while
                    // the chasing end roars for an equaliser; level and late, both ends push it forward.
                    Crd(fm, Math.Abs(hg - ag) == 1 && rng.NextDouble() < 0.5
                        ? crowd.Tension(ref rng)
                        : crowd.LatePush(ref rng));
                }
                else if (rng.NextDouble() < 0.45)
                {
                    Crd(fm, crowd.Chant(ref rng));
                }
                else
                {
                    Pbp(fm, CommentaryPhrases.Filler(ref rng));
                }
            }

            lastSpokenMinute = mo.Minute;

            switch (mo.Kind)
            {
                case Kind.Goal: EmitGoal((GoalEvent)mo.Payload); break;
                case Kind.Penalty: EmitPenalty((PenaltyEvent)mo.Payload); break;
                case Kind.Card: EmitCard((CardEvent)mo.Payload); break;
                case Kind.Sub: EmitSub((SubstitutionEvent)mo.Payload); break;
                case Kind.Injury: EmitInjury((InjuryEvent)mo.Payload); break;
                case Kind.StandaloneError: EmitError((PlayerErrorEvent)mo.Payload); break;
                case Kind.BadCall: EmitBadCall((BadCallEvent)mo.Payload); break;
                case Kind.Save: EmitSave((SaveEvent)mo.Payload); break;
                case Kind.CoolingBreak: EmitCoolingBreak((CoolingBreak)mo.Payload); break;
                case Kind.Confrontation: EmitConfrontation((Confrontation)mo.Payload); break;
                case Kind.NearMiss: EmitNearMiss((NearMiss)mo.Payload); break;
                case Kind.Var: EmitVar((VarCheck)mo.Payload); break;
            }
        }

        if (!halfTimeDone)
        {
            EmitHalfTime();
        }

        if (!extraTimeDone && m.Method != MatchMethod.Regulation)
        {
            EmitExtraTime();
        }

        EmitEnding();
        return lines;

        // ---- local emitters ----

        void EmitHalfTime()
        {
            halfTimeDone = true;
            if (m.FirstHalfStoppage > 0)
            {
                Pbp(45, $"The fourth official's board goes up — {m.FirstHalfStoppage} added {(m.FirstHalfStoppage == 1 ? "minute" : "minutes")} to be played.");
            }

            Pbp(45, $"{CommentaryPhrases.HalfTime(ref rng)} At the break, it's {Score()}.");
            if (m.HomeBox is { } hb && m.AwayBox is { } ab)
            {
                Co(45, $"{hb.PossessionPercent:0}% of the ball for {m.HomeName}, and {hb.Shots + ab.Shots} shots between them. {CommentaryPhrases.AnalystAside(ref rng)}");
            }

            Pbp(46, CommentaryPhrases.SecondHalfStart(ref rng));
        }

        void EmitExtraTime()
        {
            extraTimeDone = true;
            Pbp(90, $"{CommentaryPhrases.ExtraTimeStart(ref rng)} Still {Score()}.");
        }

        void EmitGoal(GoalEvent g)
        {
            if (g.IsOwnGoal)
            {
                if (string.Equals(g.TeamCode, m.HomeCode, StringComparison.OrdinalIgnoreCase)) hg++; else ag++;
                Pbp(g.Minute, $"Oh, disaster! {g.ScorerName} {CommentaryPhrases.OwnGoal(ref rng)} — and it counts for {TeamName(g.TeamCode)}! {Score()}.");
                if (!string.IsNullOrEmpty(g.Celebration))
                {
                    Pbp(g.Minute, $"{g.ScorerName} {g.Celebration}.");
                }

                Crd(g.Minute, crowd.OnGoal(g.TeamCode, ref rng));
                Co(g.Minute, CommentaryPhrases.AnalystAside(ref rng));
                return;
            }

            if (string.Equals(g.TeamCode, m.HomeCode, StringComparison.OrdinalIgnoreCase)) hg++; else ag++;

            string descriptor = CommentaryPhrases.GoalDescriptor(g.Type, g.Vergazo, g.DistanceMeters, g.DefendersPassed, ref rng);
            string assist = g.AssistName is not null
                ? $", {CommentaryPhrases.AssistConnector(ref rng)} {g.AssistName}"
                : g.DefendersPassed >= 2 || g.Vergazo >= 7.5 ? $" — {CommentaryPhrases.SoloEffort(ref rng)}" : "";
            Pbp(g.Minute, $"{CommentaryPhrases.GoalShout(g.Vergazo, ref rng)} {g.ScorerName} for {TeamName(g.TeamCode)}! {descriptor}{assist}. {Score()}.");
            if (!string.IsNullOrEmpty(g.Celebration))
            {
                Pbp(g.Minute, $"And the celebration — {g.ScorerName} {g.Celebration}!");
                if (CelebrationNarratives.IsProvocative(g.Celebration))
                {
                    Crd(g.Minute, crowd.Boo(ref rng));
                }
            }

            Crd(g.Minute, crowd.OnGoal(g.TeamCode, ref rng));

            if (g.CausedByError != ErrorKind.None)
            {
                var err = ErrorFor(g.Minute, g.CausedByError);
                string howl = g.CausedByError == ErrorKind.GoalkeeperError ? CommentaryPhrases.KeeperHowler(ref rng) : CommentaryPhrases.DefensiveHowler(ref rng);
                Co(g.Minute, err is not null ? $"{howl} {err.PlayerName} with {err.Description} — gifted it." : howl);
            }
            else if (g.Vergazo >= 8.0)
            {
                Co(g.Minute, $"{CommentaryPhrases.AnalystAside(ref rng)} You will see that one again and again.");
            }
        }

        void EmitPenalty(PenaltyEvent pen)
        {
            string team = TeamName(pen.TeamCode);
            Pbp(pen.Minute, $"{CommentaryPhrases.PenaltyAwarded(ref rng)} It's a spot-kick for {team}, {pen.TakerName} to take it…");
            if (pen.Controversial)
            {
                Co(pen.Minute, $"{CommentaryPhrases.Controversy(ref rng)} There are big question marks over that decision.");
            }

            switch (pen.Outcome)
            {
                case PenaltyOutcome.Scored:
                    if (string.Equals(pen.TeamCode, m.HomeCode, StringComparison.OrdinalIgnoreCase)) hg++; else ag++;
                    Pbp(pen.Minute, $"{CommentaryPhrases.PenaltyScored(ref rng)} {Score()}.");
                    Crd(pen.Minute, crowd.OnGoal(pen.TeamCode, ref rng));
                    break;
                case PenaltyOutcome.Missed:
                    Pbp(pen.Minute, $"{pen.TakerName} {CommentaryPhrases.PenaltyMissed(ref rng)} It stays {Score()}.");
                    break;
                default:
                    Pbp(pen.Minute, $"{CommentaryPhrases.PenaltySaved(ref rng)} {pen.KeeperName} is the hero! Still {Score()}.");
                    break;
            }
        }

        void EmitCard(CardEvent c)
        {
            string team = TeamName(c.TeamCode);
            string reason = string.IsNullOrEmpty(c.Reason) ? "a foul" : c.Reason;
            if (c.IsRed && c.IsSecondYellow)
            {
                Pbp(c.Minute, $"{c.PlayerName} ({team}) {CommentaryPhrases.SecondYellowRed(ref rng)} — {reason}. Down to ten men.");
            }
            else if (c.IsRed)
            {
                Pbp(c.Minute, $"{c.PlayerName} ({team}) — {CommentaryPhrases.StraightRed(ref rng)} — {reason}! {team} will finish a man light.");
            }
            else
            {
                Pbp(c.Minute, $"{c.PlayerName} ({team}) {CommentaryPhrases.Yellow(ref rng)} — {reason}.");
            }

            if (c.Controversial)
            {
                Co(c.Minute, $"{CommentaryPhrases.Controversy(ref rng)} Harsh, that.");
            }
            else
            {
                bool carderHome = string.Equals(c.TeamCode, m.HomeCode, StringComparison.OrdinalIgnoreCase);
                bool trailing = carderHome ? hg < ag : ag < hg;
                if (trailing && c.Minute >= 60)
                {
                    Co(c.Minute, FrustrationAsides[rng.NextInt(FrustrationAsides.Length)]);
                }
            }
        }

        void EmitSub(SubstitutionEvent s)
        {
            string tag = s.Injury ? " — forced by injury" : "";
            Pbp(s.Minute, $"{CommentaryPhrases.Substitution(ref rng)} for {TeamName(s.TeamCode)}: {s.OnName} on for {s.OffName}{tag}.");
        }

        void EmitInjury(InjuryEvent inj)
        {
            string diag = string.IsNullOrEmpty(inj.Diagnosis) ? "a knock" : inj.Diagnosis;
            string outlook = inj.RecoveryDays <= 0 ? "but he should be fine to continue"
                : $"and it looks like {InjuryCatalog.RecoveryText(inj.RecoveryDays)}";
            string sub = inj.CouldBeReplaced ? "" : " — and with no substitutions left, they'll have to play on a man down";
            Pbp(inj.Minute, $"{inj.PlayerName} ({TeamName(inj.TeamCode)}) {CommentaryPhrases.Injury(ref rng)} — {diag}, {outlook}{sub}.");
            if (inj.Severity == InjurySeverity.Major)
            {
                Co(inj.Minute, CommentaryPhrases.SeriousInjuryReaction(ref rng));
            }
        }

        void EmitError(PlayerErrorEvent e)
        {
            string howl = e.Kind == ErrorKind.GoalkeeperError ? CommentaryPhrases.KeeperHowler(ref rng) : CommentaryPhrases.DefensiveHowler(ref rng);
            Pbp(e.Minute, $"{howl} {e.PlayerName} ({TeamName(e.TeamCode)}) with {e.Description} — but they get away with it!");
        }

        void EmitBadCall(BadCallEvent bc)
        {
            string var = bc.VarChecked ? " VAR took a look, but the decision stands." : " No review — and it could prove costly.";
            Pbp(bc.Minute, $"{CommentaryPhrases.Controversy(ref rng)} {bc.Description}.{var}");
        }

        void EmitSave(SaveEvent sv)
        {
            Pbp(sv.Minute, $"{CommentaryPhrases.GreatSave(ref rng)} {sv.KeeperName} ({TeamName(sv.TeamCode)}) somehow keeps it out — rated {sv.Rating:0.0} out of ten.");
        }

        void EmitCoolingBreak(CoolingBreak cb)
        {
            Pbp(cb.Minute, $"And the referee signals the mandatory cooling break — it's {cb.TemperatureC}°C out here, well into the heat protocol, and the players are gulping down water and draping ice towels around their necks.");
            Co(cb.Minute, "These breaks are compulsory in this heat at the 2026 finals — and a chance for the coaches to get their messages across, too. Tactical gold.");
        }

        void EmitNearMiss(NearMiss nm)
        {
            Pbp(nm.Minute, $"OH, so close! {nm.Description}!");
            Crd(nm.Minute, crowd.NearMiss(ref rng));
        }

        void EmitVar(VarCheck v)
        {
            Crd(v.Minute, crowd.Tension(ref rng)); // the stadium holds its breath through the review
            Pbp(v.Minute, v.Description);
        }

        void EmitConfrontation(Confrontation cf)
        {
            Pbp(cf.Minute, cf.Description);
            if (cf.Level >= ConfrontationLevel.Scuffle)
            {
                Crd(cf.Minute, crowd.Boo(ref rng));
                Co(cf.Minute, cf.BenchInvolved
                    ? "That is an ugly scene — the benches emptying, and there could well be repercussions after the final whistle."
                    : "It's all kicked off — the referee needs to get a grip of this one quickly before it spreads.");
            }
        }

        void EmitEnding()
        {
            int baseLast = m.Method == MatchMethod.Regulation ? 90 : 120;
            int last = moments.Count > 0 ? Math.Max(baseLast, moments[^1].Minute) : baseLast;

            if (m.SecondHalfStoppage > 0)
            {
                Pbp(90, $"And there it is — {m.SecondHalfStoppage} {(m.SecondHalfStoppage == 1 ? "minute" : "minutes")} of added time signalled, the nerves jangling.");
            }

            if (m.Method == MatchMethod.Penalties)
            {
                Pbp(120, CommentaryPhrases.ShootoutOpener(ref rng));
                int sh = 0, sa = 0, total = m.ShootoutKicks.Count;
                for (int i = 0; i < m.ShootoutKicks.Count; i++)
                {
                    var k = m.ShootoutKicks[i];
                    string team = TeamName(k.TeamCode);
                    string lead = i >= total - 3 ? CommentaryPhrases.ShootoutPressure(ref rng) + " " : string.Empty;
                    if (k.Scored)
                    {
                        if (k.IsHome) sh++; else sa++;
                        Pbp(120, $"{lead}{k.Player} ({team}) {CommentaryPhrases.ShootoutScored(ref rng)} — {sh}–{sa}.");
                    }
                    else
                    {
                        string fail = rng.NextDouble() < 0.5 ? CommentaryPhrases.ShootoutSaved(ref rng) : CommentaryPhrases.ShootoutMissed(ref rng);
                        Pbp(120, $"{lead}{k.Player} ({team}) — {fail} Still {sh}–{sa}.");
                    }

                    if (i == 9 && total > 10)
                    {
                        Co(120, CommentaryPhrases.ShootoutSuddenDeath(ref rng));
                    }
                }

                string shootoutWinner = m.WinnerCode == m.HomeCode ? m.HomeName : m.AwayName;
                Pbp(120, $"{CommentaryPhrases.ShootoutWon(ref rng)} {shootoutWinner} win it {m.HomePens}–{m.AwayPens}!");
            }

            Pbp(last, $"{CommentaryPhrases.FullTimeWhistle(ref rng)} Final score: {m.HomeName} {m.HomeGoals}, {m.AwayName} {m.AwayGoals}" +
                (m.Method == MatchMethod.ExtraTime ? " after extra time" : m.Method == MatchMethod.Penalties ? $" ({m.HomePens}–{m.AwayPens} on penalties)" : "") + ".");

            if (!string.IsNullOrEmpty(m.WinnerCode))
            {
                string winner = TeamName(m.WinnerCode);
                string? motm = ManOfTheMatch(m);
                Pbp(last, motm is null
                    ? $"{winner} take it."
                    : $"{winner} are the winners, and the standout man was {motm}.");
            }
            else
            {
                Pbp(last, "Honours even — a point apiece.");
            }

            if (m.Miracle is { } mira2)
            {
                if (string.Equals(m.WinnerCode, mira2.TeamCode, StringComparison.OrdinalIgnoreCase))
                {
                    Pbp(last, $"AN ABSOLUTE MIRACLE! {mira2.TeamName} have pulled off one of the great World Cup shocks — they will dance in the streets tonight!");
                    Crd(last, CrowdChants.GoalRoar(ref rng));
                }
                else if (string.IsNullOrEmpty(m.WinnerCode))
                {
                    Pbp(last, $"A famous, hard-earned point for {mira2.TeamName} — they rose up and simply refused to be beaten!");
                }
                else
                {
                    Co(last, $"{mira2.TeamName} gave absolutely everything and came so close to the miracle — they leave with their heads held high.");
                }
            }

            if (m.Upset is { } up && up.MiracleRating >= 7.0)
            {
                Co(last, $"You can call that a genuine shock — nobody saw that coming. {CommentaryPhrases.AnalystAside(ref rng)}");
            }
            else
            {
                Co(last, CommentaryPhrases.AnalystAside(ref rng));
            }
        }
    }

    /// <summary>Render the lines as a plain-text transcript with a header (for the sibling .txt file).</summary>
    public static string ToTranscript(MatchResult m, IReadOnlyList<CommentaryLine> lines)
    {
        string method = m.Method switch
        {
            MatchMethod.ExtraTime => " (a.e.t.)",
            MatchMethod.Penalties => $" (pens {m.HomePens}–{m.AwayPens})",
            _ => "",
        };

        var sb = new StringBuilder();
        string title = $"FIFA World Cup 2026 — {Stages.DisplayName(m.Stage)}";
        string score = $"{m.HomeName} {m.HomeGoals}–{m.AwayGoals} {m.AwayName}{method}";
        string bar = new string('=', Math.Max(title.Length, score.Length) + 4);
        sb.AppendLine(bar);
        sb.AppendLine("  " + title);
        sb.AppendLine("  " + score);
        sb.AppendLine("  Play-by-play commentary");
        sb.AppendLine(bar);
        sb.AppendLine();

        foreach (var line in lines)
        {
            string who = line.Speaker == Analyst ? "Analyst    " : line.Speaker == CrowdVoice ? "Crowd      " : "Commentator";
            sb.AppendLine($"[{line.Minute,3}'] {who}: {line.Text}");
        }

        return sb.ToString();
    }

    private static List<Moment> BuildMoments(MatchResult m)
    {
        var list = new List<Moment>();
        foreach (var g in m.Goals.Where(x => !x.IsPenalty))
        {
            list.Add(new Moment(g.Minute, Kind.Goal, (int)Kind.Goal, g));
        }

        foreach (var p in m.Penalties)
        {
            list.Add(new Moment(p.Minute, Kind.Penalty, (int)Kind.Penalty, p));
        }

        foreach (var c in m.Cards.Where(x => !x.IsSecondYellow || x.IsRed))
        {
            // A second-yellow produces two CardEvents (the yellow and the red); narrate only the red one.
            if (c is { IsRed: false, IsSecondYellow: false } || c.IsRed)
            {
                list.Add(new Moment(c.Minute, Kind.Card, (int)Kind.Card, c));
            }
        }

        foreach (var s in m.Substitutions)
        {
            list.Add(new Moment(s.Minute, Kind.Sub, (int)Kind.Sub, s));
        }

        foreach (var inj in m.Injuries)
        {
            list.Add(new Moment(inj.Minute, Kind.Injury, (int)Kind.Injury, inj));
        }

        foreach (var e in m.Errors.Where(x => !x.LedToGoal))
        {
            list.Add(new Moment(e.Minute, Kind.StandaloneError, (int)Kind.StandaloneError, e));
        }

        foreach (var bc in m.BadCalls.Where(IsStandaloneBadCall))
        {
            list.Add(new Moment(bc.Minute, Kind.BadCall, (int)Kind.BadCall, bc));
        }

        foreach (var sv in m.SaveEvents.Where(x => x.IsAmazing))
        {
            list.Add(new Moment(sv.Minute, Kind.Save, (int)Kind.Save, sv));
        }

        foreach (var cb in m.CoolingBreaks)
        {
            list.Add(new Moment(cb.Minute, Kind.CoolingBreak, (int)Kind.CoolingBreak, cb));
        }

        foreach (var cf in m.Confrontations)
        {
            list.Add(new Moment(cf.Minute, Kind.Confrontation, (int)Kind.Confrontation, cf));
        }

        foreach (var nm in m.NearMisses)
        {
            list.Add(new Moment(nm.Minute, Kind.NearMiss, (int)Kind.NearMiss, nm));
        }

        foreach (var v in m.VarChecks)
        {
            list.Add(new Moment(v.Minute, Kind.Var, (int)Kind.Var, v));
        }

        return list.OrderBy(x => x.Minute).ThenBy(x => x.Order).ToList();
    }

    private static bool IsStandaloneBadCall(BadCallEvent bc) =>
        bc.Type is BadCallType.PenaltyDenied or BadCallType.MissedCard or BadCallType.GoalWronglyDisallowed;

    private static readonly string[] FrustrationAsides =
    {
        "You can see the frustration boiling over — chasing the game and it's getting niggly.",
        "Tempers fraying now; the bookings are coming from sheer frustration as much as anything.",
        "That's a frustrated team — they know the clock's against them and it's showing.",
        "The discipline is starting to crack as they push for a way back into it.",
        "Niggle creeping in, players losing their heads a little as the equaliser won't come.",
        "Heads going down, and the fouls getting cynical with it. Pure frustration.",
    };

    private static string Lower1(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    private static string Punctuate(string s) =>
        string.IsNullOrEmpty(s) || s[^1] is '.' or '!' or '?' ? s : s + ".";

    /// <summary>Pick the standout player from the goal/assist contributions (mirrors the box-score MOTM).</summary>
    private static string? ManOfTheMatch(MatchResult m)
    {
        var score = new Dictionary<string, (string Name, double Pts)>(StringComparer.Ordinal);
        void Add(string id, string name, double pts)
        {
            var cur = score.TryGetValue(id, out var v) ? v : (Name: name, Pts: 0.0);
            score[id] = (name, cur.Pts + pts);
        }

        foreach (var g in m.Goals)
        {
            if (g.IsOwnGoal) continue;
            Add(g.ScorerId, g.ScorerName, 2 + g.Vergazo * 0.1);
            if (g.AssistId is not null && g.AssistName is not null) Add(g.AssistId, g.AssistName, 1);
        }

        return score.Count == 0 ? null : score.Values.OrderByDescending(x => x.Pts).First().Name;
    }

    private static ulong SeedFor(MatchResult m)
    {
        unchecked
        {
            ulong s = 1469598103934665603UL; // FNV-1a offset
            void Mix(int v) => s = (s ^ (uint)v) * 1099511628211UL;
            foreach (char c in m.HomeCode) Mix(c);
            foreach (char c in m.AwayCode) Mix(c);
            Mix(m.HomeGoals);
            Mix(m.AwayGoals);
            Mix(m.Goals.Count);
            Mix(m.Cards.Count);
            Mix((int)m.Method);
            Mix(m.HomePens);
            Mix(m.AwayPens);
            foreach (var g in m.Goals) Mix(g.Minute * 31 + (int)g.Type);
            return s == 0 ? 0x9E3779B97F4A7C15UL : s;
        }
    }
}
