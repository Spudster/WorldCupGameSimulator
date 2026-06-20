using System.Globalization;
using System.Text;
using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Engine.Simulation;
using WorldCup.Engine.Stats;
using WorldCup.Engine.Tournament;

namespace WorldCup.Reporting;

/// <summary>
/// Renders self-contained, nicely-styled HTML reports (no external assets): a single-match card, a
/// match prediction page, and the full tournament bracket with scores through to the final.
/// </summary>
public static class HtmlExporter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ---------------------------------------------------------------- match prediction (aggregate)

    public static void MatchAggregateToHtml(MatchAggregateReport r, string path)
    {
        string outcome, fav;
        double prob;
        if (r.HomeWin >= r.Draw && r.HomeWin >= r.AwayWin) { outcome = $"{r.HomeName} win"; prob = r.HomeWin; fav = r.HomeName; }
        else if (r.AwayWin >= r.Draw && r.AwayWin >= r.HomeWin) { outcome = $"{r.AwayName} win"; prob = r.AwayWin; fav = r.AwayName; }
        else { outcome = "Draw"; prob = r.Draw; fav = "—"; }

        var modal = r.TopScorelines.Count > 0 ? r.TopScorelines[0] : null;
        // Headline predicted score = the most likely scoreline CONSISTENT with the favoured result, so
        // the big score agrees with the win %. (A favourite's win is spread over many scorelines, so the
        // single most-common exact score — the modal — is often a draw or 1–0; it is shown alongside.)
        int favSign = r.HomeWin >= r.Draw && r.HomeWin >= r.AwayWin ? 1
            : r.AwayWin >= r.Draw && r.AwayWin >= r.HomeWin ? -1 : 0;
        var predicted = r.TopScorelines.FirstOrDefault(s => Math.Sign(s.HomeGoals - s.AwayGoals) == favSign) ?? modal;
        string predScore = predicted is not null ? $"{predicted.HomeGoals}–{predicted.AwayGoals}" : "–";
        string top3 = string.Join(" · ", r.TopScorelines.Take(3).Select(s => $"<b>{s.HomeGoals}-{s.AwayGoals}</b> ({P(s.Probability)})"));
        string modalLine = modal is not null && predicted is not null && (modal.HomeGoals != predicted.HomeGoals || modal.AwayGoals != predicted.AwayGoals)
            ? $" · most common single score <b>{modal.HomeGoals}–{modal.AwayGoals}</b> ({P(modal.Probability)})" : "";
        string avgScore = $"rounds to {Math.Round(r.AvgHomeGoals, MidpointRounding.AwayFromZero):0}–{Math.Round(r.AvgAwayGoals, MidpointRounding.AwayFromZero):0}";

        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>MATCH PREDICTION · {r.Iterations:N0} simulations</div>");
        b.Append($"<div class='vs'><div class='side'>{E(FlagName(r.HomeCode, r.HomeName))}</div><div class='score'>{predScore}</div><div class='side'>{E(FlagName(r.AwayCode, r.AwayName))}</div></div>");
        b.Append($"<div class='hero-sub'>Most likely: <b>{E(outcome)}</b> ({P(prob)}) · average {N1(r.AvgHomeGoals)}–{N1(r.AvgAwayGoals)} <span class='dim'>({avgScore})</span>{modalLine}<br>Most probable scores: {top3}</div></div>");

        // W/D/L bars, each with a 95% confidence interval (±1.96·√(p(1-p)/N)). The label is HTML-encoded
        // by WdlBar, so the CI is appended as plain text rather than markup.
        string Ci(double p) => " ±" + P(1.96 * Math.Sqrt(p * (1 - p) / Math.Max(1, r.Iterations)));
        b.Append("<div class='card'><h2>Result probabilities</h2><div class='wdl'>");
        b.Append(WdlBar(r.HomeName + " win" + Ci(r.HomeWin), r.HomeWin, "home"));
        b.Append(WdlBar("Draw" + Ci(r.Draw), r.Draw, "draw"));
        b.Append(WdlBar(r.AwayName + " win" + Ci(r.AwayWin), r.AwayWin, "away"));
        b.Append("</div></div>");

        // Markets chips.
        b.Append("<div class='card'><h2>Match markets</h2><div class='chips'>");
        b.Append(Chip("Both teams score", r.BttsPercent / 100.0));
        b.Append(Chip("Over 2.5 goals", r.Over25Percent / 100.0));
        b.Append(Chip("Under 2.5 goals", 1 - r.Over25Percent / 100.0));
        b.Append(Chip(r.HomeName + " clean sheet", r.HomeCleanSheetPercent / 100.0));
        b.Append(Chip(r.AwayName + " clean sheet", r.AwayCleanSheetPercent / 100.0));
        b.Append(Chip("Underdog to win", Math.Min(r.HomeWin, r.AwayWin)));
        b.Append("</div></div>");

        // Stat comparison.
        b.Append($"<div class='card'><h2>Average match stats</h2><div class='cmp-head'><span>{E(r.HomeName)}</span><span></span><span>{E(r.AwayName)}</span></div>");
        string Acc(double sot, double shots) => shots > 0 ? $"{100.0 * sot / shots:0}%" : "—";
        b.Append(Cmp("Goals", r.Home.Goals, r.Away.Goals));
        b.Append(Cmp("Possession %", r.Home.Possession, r.Away.Possession, $"{r.Home.Possession:0}%", $"{r.Away.Possession:0}%"));
        b.Append(Cmp("Shots", r.Home.Shots, r.Away.Shots));
        b.Append(Cmp("On target", r.Home.ShotsOnTarget, r.Away.ShotsOnTarget));
        b.Append(Cmp("Shot accuracy", r.Home.ShotsOnTarget, r.Away.ShotsOnTarget, Acc(r.Home.ShotsOnTarget, r.Home.Shots), Acc(r.Away.ShotsOnTarget, r.Away.Shots)));
        b.Append(Cmp("Corners", r.Home.Corners, r.Away.Corners));
        b.Append(Cmp("Throw-ins", r.Home.ThrowIns, r.Away.ThrowIns));
        b.Append(Cmp("Goal kicks", r.Home.GoalKicks, r.Away.GoalKicks));
        b.Append(Cmp("Fouls", r.Home.Fouls, r.Away.Fouls));
        b.Append(Cmp("Offsides", r.Home.Offsides, r.Away.Offsides));
        b.Append(Cmp("Yellow cards", r.Home.Yellows, r.Away.Yellows));
        b.Append(Cmp("Red cards", r.Home.Reds, r.Away.Reds, N2(r.Home.Reds), N2(r.Away.Reds)));
        b.Append(Cmp("Penalties", r.Home.Penalties, r.Away.Penalties, N2(r.Home.Penalties), N2(r.Away.Penalties)));
        b.Append(Cmp("Injuries", r.Home.Injuries, r.Away.Injuries));
        b.Append(Cmp("Saves", r.Home.Saves, r.Away.Saves));
        b.Append("</div>");

        // Scorelines + scorers, side by side.
        b.Append("<div class='grid2'>");
        b.Append("<div class='card'><h2>Most common scorelines</h2><table class='lst'>");
        foreach (var s in r.TopScorelines.Take(8))
        {
            b.Append($"<tr><td>{s.HomeGoals}–{s.AwayGoals}</td><td class='bar-cell'>{MiniBar(s.Probability)}</td><td class='r'>{P(s.Probability)}</td></tr>");
        }
        b.Append("</table></div>");

        b.Append("<div class='card'><h2>Most likely scorers</h2><table class='lst'>");
        foreach (var s in r.TopScorers.Take(8))
        {
            b.Append($"<tr><td>{E(s.Name)}</td><td class='dim'>{E(s.TeamCode)}</td><td class='r'>{N2(s.GoalsPerMatch)}/game</td></tr>");
        }
        b.Append("</table></div>");
        b.Append("</div>");

        b.Append(ScorelineHeatmap(r));

        b.Append($"<div class='card'><h2>Goal quality</h2><p>Average vergazo <b>{N1(r.AverageVergazo)}/10</b> · certified vergazos (9+/10) <b>{N2(r.WorldiePercent)}%</b> of goals</p>");
        if (r.BestGoal is { } bg)
        {
            b.Append($"<p class='note'>Best goal across all sims: <b>{E(bg.ScorerName)}</b> ({E(bg.TeamCode)}) — vergazo <b>{N1(bg.Vergazo)}/10</b></p>");
        }

        b.Append("</div>");

        // Mistakes & officiating (averaged per game).
        var c = r.Controversy;
        b.Append("<div class='card'><h2>🤦 Mistakes &amp; officiating (per game)</h2><div class='chips'>");
        b.Append(Stat("🧤 Keeper errors → goal", N2(c.KeeperErrorGoals)));
        b.Append(Stat("Defensive errors → goal", N2(c.DefensiveErrorGoals)));
        b.Append(Stat("Errors, no goal", N2(c.UnpunishedErrors)));
        b.Append(Stat("Soft / wrong pens", N2(c.ControversialPenalties)));
        b.Append(Stat("Harsh / wrong cards", N2(c.ControversialCards)));
        b.Append(Stat("Other ref mistakes", N2(c.RefereeMistakes)));
        b.Append("</div></div>");

        Write(path, $"{r.HomeName} vs {r.AwayName} — Prediction", b.ToString(),
            $"{r.ParameterLabel} · seed {r.Seed} · {r.Iterations:N0} simulations");
    }

    // ---------------------------------------------------------------- all scheduled fixtures forecast

    public static void ScheduledForecastsToHtml(ScheduledForecastReport r, string path)
    {
        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>SCHEDULED FIXTURES · {r.IterationsPerGame:N0} SIMS / GAME</div>");
        b.Append($"<div class='vs'><div class='side'>{r.Games.Count} game(s) still to play</div></div>");
        b.Append($"<div class='hero-sub'>The most-likely result of every remaining fixture · {E(r.ParameterLabel)}</div></div>");

        // Highlights: the standout fixtures across the slate.
        if (r.Games.Count > 0)
        {
            // The favoured team's name for a fixture (home / away / "draw" between both).
            string FavName(ScheduledGameForecast g) => g.Favourite switch
            {
                1 => g.Report.HomeName,
                -1 => g.Report.AwayName,
                _ => $"{g.Report.HomeName} v {g.Report.AwayName}",
            };
            double FavProb(ScheduledGameForecast g) => Math.Max(g.Report.HomeWin, g.Report.AwayWin);

            var biggest = r.Games.MaxBy(g => Math.Max(g.Report.HomeWin, g.Report.AwayWin))!;
            var closest = r.Games.MinBy(g => Math.Max(g.Report.HomeWin, g.Report.AwayWin))!;
            var drawiest = r.Games.MaxBy(g => g.Report.Draw)!;

            b.Append("<div class='card'><h2>Highlights</h2><div class='chips'>");
            b.Append(Stat("Biggest favourite", $"{E(FavName(biggest))} <span class='dim'>{P(FavProb(biggest))}</span>"));
            b.Append(Stat("Closest match", $"{E(FavName(closest))} <span class='dim'>{P(FavProb(closest))}</span>"));
            b.Append(Stat("Most likely draw", $"{E(FavName(drawiest))} <span class='dim'>{P(drawiest.Report.Draw)}</span>"));
            b.Append("</div></div>");
        }

        b.Append("<div class='card'><h2>Remaining fixtures — forecast</h2><table class='lst'>");
        b.Append("<tr class='th'><td>Kickoff (local)</td><td>Grp</td><td>MD</td><td>Match <span class='dim'>(favourite bold)</span></td><td class='r'>Score</td><td>Result odds — home / draw / away</td><td class='r'>xG</td></tr>");
        foreach (var f in r.Games.OrderBy(g => g.KickoffUtc).ThenBy(g => g.Group).ThenBy(g => g.Matchday))
        {
            var m = f.Report;
            string home = f.Favourite == 1 ? $"<b class='gwin'>{E(FlagName(m.HomeCode, m.HomeName))}</b>" : E(FlagName(m.HomeCode, m.HomeName));
            string away = f.Favourite == -1 ? $"<b class='gwin'>{E(FlagName(m.AwayCode, m.AwayName))}</b>" : E(FlagName(m.AwayCode, m.AwayName));
            // Most likely scoreline for the forecast RESULT (agrees with the win odds).
            string score = f.PredictedScore is { } s ? $"{s.HomeGoals}–{s.AwayGoals}" : "–";
            string kickoff = f.KickoffUtc.ToLocalTime().ToString("MMM d HH:mm", Inv);
            b.Append($"<tr><td class='dim'>{E(kickoff)}</td><td class='dim'>{f.Group}</td><td class='dim'>{f.Matchday}</td><td>{home} <span class='dim'>v</span> {away}</td><td class='r'><b>{score}</b></td><td>{WdlSplit(m.HomeWin, m.Draw, m.AwayWin)}</td><td class='r dim'>{N1(m.AvgHomeGoals)}–{N1(m.AvgAwayGoals)}</td></tr>");
        }

        b.Append("</table><p class='note'>Score = the most likely scoreline for the forecast result. A favourite's win is spread over many scorelines, so the single most-common exact score is often a draw or 1–0.</p></div>");

        Write(path, "World Cup 2026 — Scheduled fixtures forecast", b.ToString(),
            $"{r.ParameterLabel} · seed {r.Seed} · {r.IterationsPerGame:N0} sims/game · {r.Games.Count} games · forecast in {N1(r.ElapsedSeconds)}s");
    }

    private static string WdlSplit(double h, double d, double a) =>
        "<div class='wdlsplit'>" +
        $"<span class='seg sh' style='width:{Px(h * 100)}%'></span>" +
        $"<span class='seg sd' style='width:{Px(d * 100)}%'></span>" +
        $"<span class='seg sa' style='width:{Px(a * 100)}%'></span></div>" +
        $"<div class='wdlnum'><span class='nh'>{P(h)}</span><span class='nd'>{P(d)}</span><span class='na'>{P(a)}</span></div>";

    // ---------------------------------------------------------------- single played match

    public static void MatchResultToHtml(MatchResult m, string path)
    {
        string method = m.Method switch
        {
            MatchMethod.ExtraTime => " (a.e.t.)",
            MatchMethod.Penalties => $" (pens {m.HomePens}–{m.AwayPens})",
            _ => "",
        };
        string winner = m.WinnerCode == m.HomeCode ? m.HomeName : m.WinnerCode == m.AwayCode ? m.AwayName : "Draw";
        int hPens = m.Penalties.Count(x => x.TeamCode == m.HomeCode), aPens = m.Penalties.Count(x => x.TeamCode == m.AwayCode);
        int hInj = m.Injuries.Count(x => x.TeamCode == m.HomeCode), aInj = m.Injuries.Count(x => x.TeamCode == m.AwayCode);
        int totY = m.Cards.Count(c => !c.IsRed), totR = m.Cards.Count(c => c.IsRed);

        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>{E(Stages.DisplayName(m.Stage))}{E(method)}</div>");
        b.Append($"<div class='vs'><div class='side {(m.WinnerCode == m.HomeCode ? "win" : "")}'>{E(FlagName(m.HomeCode, m.HomeName))}</div><div class='score'>{m.HomeGoals}–{m.AwayGoals}</div><div class='side {(m.WinnerCode == m.AwayCode ? "win" : "")}'>{E(FlagName(m.AwayCode, m.AwayName))}</div></div>");
        b.Append($"<div class='hero-sub'>{(m.IsDraw ? "Draw" : "Winner: <b>" + E(winner) + "</b>")}</div>");
        b.Append($"<div class='hero-date'>🔮 This was predicted on {E(Ui.PredictedOnText())}</div></div>");

        if (m.Upset is { } u)
        {
            var (label, cls) = MiracleClass(u.MiracleRating);
            b.Append($"<div class='card miracle {cls}'><div class='mr'><span class='mr-val'>{N1(u.MiracleRating)}</span><span class='mr-of'>/10</span></div>");
            b.Append($"<div><div class='mr-label'>🎲 {label}</div><div class='dim'>Pre-match: {E(m.HomeName)} {P(u.PreMatchHomeWin)} · draw {P(u.PreMatchDraw)} · {E(m.AwayName)} {P(u.PreMatchAwayWin)} · this exact score {P(u.ResultProbability)}</div></div></div>");
        }

        // Key facts chips.
        b.Append("<div class='card'><h2>Key facts</h2><div class='chips'>");
        var motm = PlayerOfMatch(m);
        if (motm is not null) b.Append(Fact("⭐ Player of the match", E(motm.Value.Name), E(motm.Value.Team) + " · " + motm.Value.Line));
        b.Append(Fact("Goals", (m.HomeGoals + m.AwayGoals).ToString(), m.Goals.Count(g => g.IsOwnGoal) is var og && og > 0 ? $"{og} own goal(s)" : "in normal time"));
        b.Append(Fact("Cards", $"{totY}🟨 {totR}🟥", "shown"));
        if (m.Penalties.Count > 0) b.Append(Fact("Penalties", m.Penalties.Count.ToString(), $"{m.Penalties.Count(p => p.Outcome == PenaltyOutcome.Scored)} scored"));
        if (m.Injuries.Count > 0) b.Append(Fact("Injuries", m.Injuries.Count.ToString(), "in the match"));
        if (m.Substitutions.Count > 0) b.Append(Fact("🔄 Substitutions", m.Substitutions.Count.ToString(), m.Substitutions.Count(s => s.Injury) is var inj && inj > 0 ? $"{inj} forced by injury" : "all tactical"));
        var topSave = m.SaveEvents.OrderByDescending(sv => sv.Rating).FirstOrDefault();
        if (topSave is not null) b.Append(Fact("🧤 Save of the match", N1(topSave.Rating) + "/10", E(topSave.KeeperName) + " (" + E(topSave.TeamCode) + ")"));
        var flow = MatchFlow.Analyze(m);
        if (flow.MaxHomeLead > 0 || flow.MaxAwayLead > 0)
            b.Append(Fact("Biggest lead", $"+{Math.Max(flow.MaxHomeLead, flow.MaxAwayLead)}", flow.MaxHomeLead >= flow.MaxAwayLead ? E(m.HomeName) : E(m.AwayName)));
        if (m.ShootoutRounds > 0) b.Append(Fact("Shootout", $"{m.HomePens}–{m.AwayPens}", $"{m.ShootoutRounds} rounds"));
        if (m.FirstHalfStoppage > 0 || m.SecondHalfStoppage > 0) b.Append(Fact("⏱ Added time", $"+{m.FirstHalfStoppage} / +{m.SecondHalfStoppage}", "1st / 2nd half"));
        if (m.TemperatureC > 0)
        {
            var (wIcon, wLabel) = m.Weather is { } cw ? WeatherNarratives.Badge(cw.Kind) : ("🌡", "");
            string condVal = string.IsNullOrEmpty(wLabel) ? $"{m.TemperatureC}°C" : $"{E(wLabel)}, {m.TemperatureC}°C";
            string condSub = m.Weather is { } cw2 ? E(cw2.Note) : (m.CoolingBreaks.Count > 0 ? $"{m.CoolingBreaks.Count} cooling breaks" : "no breaks needed");
            b.Append(Fact($"{wIcon} Conditions", condVal, condSub));
        }

        var crowd = new Crowd(m);
        b.Append(Fact("👥 Crowd", $"{crowd.Attendance:N0}", crowd.Partisan ? $"~{(int)Math.Round(Math.Max(crowd.HomeSupportShare, 1 - crowd.HomeSupportShare) * 100)}% {E(crowd.DominantName)}" : "even split"));
        if (m.NearMisses.Count > 0)
        {
            int wood = m.NearMisses.Count(n => n.Kind is NearMissKind.HitThePost or NearMissKind.HitTheBar or NearMissKind.RattledTheWoodwork);
            b.Append(Fact("🪵 Near misses", m.NearMisses.Count.ToString(), wood > 0 ? $"{wood} off the woodwork" : "close calls"));
        }

        if (m.VarChecks.Count > 0) b.Append(Fact("📺 VAR checks", m.VarChecks.Count.ToString(), "to the monitor"));
        if (m.Confrontations.Count > 0) b.Append(Fact("🤬 Flashpoints", m.Confrontations.Count.ToString(), m.Confrontations.Any(c => c.BenchInvolved) ? "benches involved" : "tempers frayed"));
        if (m.Miracle is { } mira) b.Append(Fact("✨ Miracle", E(mira.TeamName), "caught fire"));
        int errGoals = m.Errors.Count(e => e.LedToGoal);
        if (errGoals > 0) b.Append(Fact("🤦 Error goals", errGoals.ToString(), "gifted to the opponent"));
        if (m.BadCalls.Count > 0) b.Append(Fact("⚖️ Bad calls", m.BadCalls.Count.ToString(), $"{m.BadCalls.Count(bc => bc.VarChecked)} VAR-checked"));
        b.Append("</div></div>");

        b.Append(Timeline(m));

        // Goals.
        if (m.Goals.Count > 0)
        {
            b.Append("<div class='card'><h2>Goals</h2><table class='lst'><tr class='th'><td>Min</td><td>Scorer</td><td>Team</td><td>Assist</td><td>Type</td><td class='r'>Dist</td><td class='r'>Beat</td><td class='r'>Vergazo</td></tr>");
            foreach (var g in m.Goals)
            {
                string s = g.IsOwnGoal ? E(g.ScorerName) + " <span class='og'>OG</span>" : E(g.ScorerName);
                s += g.CausedByError switch
                {
                    ErrorKind.GoalkeeperError => " <span class='og'>GK error</span>",
                    ErrorKind.DefensiveError => " <span class='badge yellow'>def. error</span>",
                    _ => string.Empty,
                };
                b.Append($"<tr><td class='dim'>{g.Minute}'</td><td>{s}</td><td class='dim'>{E(g.TeamCode)}</td><td class='dim'>{E(g.AssistName ?? "—")}</td><td class='dim'>{E(GoalTypeName(g.Type))}</td><td class='r dim'>{(g.IsPenalty ? "—" : N1(g.DistanceMeters) + "m")}</td><td class='r dim'>{(g.DefendersPassed > 0 ? g.DefendersPassed.ToString() : "—")}</td><td class='r'>{Vergazo(g.Vergazo)}</td></tr>");
            }
            b.Append("</table>");
            foreach (var g in m.Goals)
            {
                if (!string.IsNullOrEmpty(g.Celebration))
                {
                    b.Append($"<p class='note'>🎉 <b>{g.Minute}'</b> {E(g.ScorerName)} {E(g.Celebration)}.</p>");
                }
            }

            double avgV = m.Goals.Average(x => x.Vergazo);
            int worldies = m.Goals.Count(x => x.Vergazo >= 9);
            double longest = m.Goals.Where(x => !x.IsPenalty && !x.IsOwnGoal).Select(x => x.DistanceMeters).DefaultIfEmpty(0).Max();
            b.Append($"<p class='note'>Goal quality: avg vergazo <b>{N1(avgV)}/10</b> · {worldies} certified vergazo(s) · longest strike <b>{N1(longest)}m</b></p></div>");
        }

        // Full box score.
        if (m.HomeBox is { } hb && m.AwayBox is { } ab)
        {
            string Pc(int num, int den) => den > 0 ? $"{100.0 * num / den:0}%" : "—";
            b.Append($"<div class='card'><h2>Match stats</h2><div class='cmp-head'><span>{E(m.HomeName)}</span><span></span><span>{E(m.AwayName)}</span></div>");
            b.Append(Cmp("Possession %", hb.PossessionPercent, ab.PossessionPercent, $"{hb.PossessionPercent:0}%", $"{ab.PossessionPercent:0}%"));
            b.Append(Cmp("Shots", hb.Shots, ab.Shots, hb.Shots.ToString(), ab.Shots.ToString()));
            b.Append(Cmp("On target", hb.ShotsOnTarget, ab.ShotsOnTarget, hb.ShotsOnTarget.ToString(), ab.ShotsOnTarget.ToString()));
            b.Append(Cmp("Shot accuracy", hb.ShotsOnTarget, ab.ShotsOnTarget, Pc(hb.ShotsOnTarget, hb.Shots), Pc(ab.ShotsOnTarget, ab.Shots)));
            b.Append(Cmp("Conversion", hb.Goals, ab.Goals, Pc(hb.Goals, hb.Shots), Pc(ab.Goals, ab.Shots)));
            b.Append(Cmp("Corners", hb.Corners, ab.Corners, hb.Corners.ToString(), ab.Corners.ToString()));
            b.Append(Cmp("Throw-ins", hb.ThrowIns, ab.ThrowIns, hb.ThrowIns.ToString(), ab.ThrowIns.ToString()));
            b.Append(Cmp("Goal kicks", hb.GoalKicks, ab.GoalKicks, hb.GoalKicks.ToString(), ab.GoalKicks.ToString()));
            b.Append(Cmp("Fouls", hb.Fouls, ab.Fouls, hb.Fouls.ToString(), ab.Fouls.ToString()));
            b.Append(Cmp("Offsides", hb.Offsides, ab.Offsides, hb.Offsides.ToString(), ab.Offsides.ToString()));
            b.Append(Cmp("Yellow cards", hb.Yellows, ab.Yellows, hb.Yellows.ToString(), ab.Yellows.ToString()));
            b.Append(Cmp("Red cards", hb.Reds, ab.Reds, hb.Reds.ToString(), ab.Reds.ToString()));
            b.Append(Cmp("Penalties", hPens, aPens, hPens.ToString(), aPens.ToString()));
            b.Append(Cmp("Injuries", hInj, aInj, hInj.ToString(), aInj.ToString()));
            b.Append(Cmp("Saves", hb.Saves, ab.Saves, hb.Saves.ToString(), ab.Saves.ToString()));

            // Mistakes & officiating, per team — keeps the box score consistent with the console.
            int hErr = m.Errors.Count(e => e.TeamCode == m.HomeCode), aErr = m.Errors.Count(e => e.TeamCode == m.AwayCode);
            int hErrG = m.Errors.Count(e => e.TeamCode == m.HomeCode && e.LedToGoal), aErrG = m.Errors.Count(e => e.TeamCode == m.AwayCode && e.LedToGoal);
            int hBcFor = m.BadCalls.Count(x => x.ForCode == m.HomeCode), aBcFor = m.BadCalls.Count(x => x.ForCode == m.AwayCode);
            int hBcAg = m.BadCalls.Count(x => x.AgainstCode == m.HomeCode), aBcAg = m.BadCalls.Count(x => x.AgainstCode == m.AwayCode);
            b.Append(Cmp("Errors (→ goal)", hErr, aErr, $"{hErr} ({hErrG})", $"{aErr} ({aErrG})"));
            b.Append(Cmp("Bad calls for / against", hBcFor, aBcFor, $"{hBcFor} / {hBcAg}", $"{aBcFor} / {aBcAg}"));
            b.Append("</div>");
        }

        // Cards.
        if (m.Cards.Count > 0)
        {
            b.Append("<div class='card'><h2>Cards</h2><table class='lst'><tr class='th'><td>Min</td><td>Player</td><td>Team</td><td>Reason</td><td class='r'>Card</td></tr>");
            foreach (var c in m.Cards)
            {
                string badge = c.IsRed
                    ? (c.IsSecondYellow ? "<span class='badge red'>RED (2nd yellow)</span>" : "<span class='badge red'>RED</span>")
                    : "<span class='badge yellow'>YELLOW</span>";
                string reason = E(string.IsNullOrEmpty(c.Reason) ? "—" : c.Reason) + (c.Controversial ? " <span class='badge yellow'>harsh</span>" : "");
                b.Append($"<tr><td class='dim'>{c.Minute}'</td><td>{E(c.PlayerName)}</td><td class='dim'>{E(c.TeamCode)}</td><td class='dim'>{reason}</td><td class='r'>{badge}</td></tr>");
            }
            b.Append("</table></div>");
        }

        // Penalties.
        if (m.Penalties.Count > 0)
        {
            b.Append("<div class='card'><h2>Penalties</h2><table class='lst'>");
            foreach (var pen in m.Penalties)
            {
                string pill = pen.Outcome switch
                {
                    PenaltyOutcome.Scored => "<span class='pill scored'>Scored</span>",
                    PenaltyOutcome.Saved => "<span class='pill saved'>Saved</span>",
                    _ => "<span class='pill missed'>Missed</span>",
                };
                if (pen.Controversial) pill += " <span class='badge yellow'>soft</span>";
                b.Append($"<tr><td class='dim'>{pen.Minute}'</td><td>{E(pen.TakerName)}</td><td class='dim'>{E(pen.TeamCode)}</td><td class='r'>{pill}</td><td class='dim'>kpr {E(pen.KeeperName)}</td></tr>");
            }
            b.Append("</table></div>");
        }

        // Penalty shootout — kick by kick.
        if (m.ShootoutKicks.Count > 0)
        {
            string soWinner = m.HomePens > m.AwayPens ? m.HomeName : m.AwayName;
            b.Append($"<div class='card'><h2>🥅 Penalty shootout — {E(m.HomeName)} {m.HomePens}–{m.AwayPens} {E(m.AwayName)}</h2>");
            b.Append("<table class='lst'><tr class='th'><td>#</td><td>Team</td><td>Taker</td><td>Result</td><td class='r'>Score</td></tr>");
            int hh = 0, aa = 0;
            foreach (var k in m.ShootoutKicks)
            {
                if (k.Scored)
                {
                    if (k.IsHome) hh++; else aa++;
                }

                string res = k.Scored ? "<span class='pill scored'>✓ scored</span>" : "<span class='pill missed'>✗ missed</span>";
                b.Append($"<tr><td class='dim'>{k.Number}</td><td class='dim'>{E(k.TeamCode)}</td><td>{E(k.Player)}</td><td class='r'>{res}</td><td class='r'><b>{hh}–{aa}</b></td></tr>");
            }

            b.Append($"</table><p class='note'><b>{E(soWinner)}</b> win the shootout {Math.Max(m.HomePens, m.AwayPens)}–{Math.Min(m.HomePens, m.AwayPens)} after {m.ShootoutRounds} rounds.</p></div>");
        }

        // Injuries (with the specific diagnosis and the expected lay-off).
        if (m.Injuries.Count > 0)
        {
            b.Append("<div class='card'><h2>Injuries</h2><table class='lst'><tr class='th'><td>Min</td><td>Player</td><td>Team</td><td>Diagnosis</td><td>Severity</td><td>Out for</td><td class='r'>Status</td></tr>");
            foreach (var inj in m.Injuries)
            {
                string rep = inj.CouldBeReplaced ? "<span class='dim'>subbed off</span>" : "<span class='pill missed'>played on / no sub</span>";
                string diagnosis = string.IsNullOrEmpty(inj.Diagnosis) ? "—" : inj.Diagnosis;
                b.Append($"<tr><td class='dim'>{inj.Minute}'</td><td>🩹 {E(inj.PlayerName)}</td><td class='dim'>{E(inj.TeamCode)}</td><td>{E(diagnosis)}</td><td class='dim'>{inj.Severity}</td><td class='dim'>{E(InjuryCatalog.RecoveryText(inj.RecoveryDays))}</td><td class='r'>{rep}</td></tr>");
            }
            b.Append("</table></div>");
        }

        // Substitutions.
        if (m.Substitutions.Count > 0)
        {
            b.Append("<div class='card'><h2>🔄 Substitutions</h2><table class='lst'>");
            foreach (var s in m.Substitutions)
            {
                string tag = s.Injury ? " <span class='pill missed'>injury</span>" : "";
                b.Append($"<tr><td class='dim'>{s.Minute}'</td><td class='dim'>{E(s.TeamCode)}</td><td>▶ {E(s.OnName)}</td><td class='dim'>◀ {E(s.OffName)}{tag}</td></tr>");
            }
            b.Append("</table></div>");
        }

        // Notable saves.
        if (m.SaveEvents.Count > 0)
        {
            b.Append("<div class='card'><h2>🧤 Notable saves</h2><table class='lst'>");
            foreach (var s in m.SaveEvents.OrderByDescending(x => x.Rating))
            {
                string r = s.IsAmazing ? $"<span class='verg v-hi'>{N1(s.Rating)} 🔥</span>" : $"<span class='verg v-mid'>{N1(s.Rating)}</span>";
                b.Append($"<tr><td class='dim'>{s.Minute}'</td><td>{E(s.KeeperName)}</td><td class='dim'>{E(s.TeamCode)}</td><td class='r dim'>{N1(s.ShotDistanceMeters)}m</td><td class='r'>{r}</td></tr>");
            }
            b.Append("</table></div>");
        }

        // Errors (player & goalkeeper mistakes).
        if (m.Errors.Count > 0)
        {
            b.Append("<div class='card'><h2>🤦 Errors</h2><table class='lst'><tr class='th'><td>Min</td><td>Player</td><td>Team</td><td>Type</td><td>What happened</td><td class='r'>Outcome</td></tr>");
            foreach (var e in m.Errors)
            {
                string kind = e.Kind == ErrorKind.GoalkeeperError ? "🧤 keeper" : "defensive";
                string outcome = e.LedToGoal ? "<span class='pill missed'>led to a goal</span>" : "<span class='dim'>got away with it</span>";
                b.Append($"<tr><td class='dim'>{e.Minute}'</td><td>{E(e.PlayerName)}</td><td class='dim'>{E(e.TeamCode)}</td><td class='dim'>{E(kind)}</td><td class='dim'>{E(e.Description)}</td><td class='r'>{outcome}</td></tr>");
            }

            b.Append("</table></div>");
        }

        // Refereeing controversy (bad calls).
        if (m.BadCalls.Count > 0)
        {
            b.Append("<div class='card'><h2>⚖️ Refereeing controversy</h2><table class='lst'><tr class='th'><td>Min</td><td>Call</td><td>Player</td><td>Detail</td><td class='r'>VAR</td></tr>");
            foreach (var bc in m.BadCalls)
            {
                string player = string.IsNullOrEmpty(bc.PlayerName) ? "—" : bc.PlayerName;
                b.Append($"<tr><td class='dim'>{bc.Minute}'</td><td><span class='badge yellow'>{E(BadCallLabel(bc.Type))}</span></td><td class='dim'>{E(player)}</td><td class='dim'>{E(bc.Description)}</td><td class='r dim'>{(bc.VarChecked ? "checked" : "—")}</td></tr>");
            }

            b.Append("</table></div>");
        }

        // On-field flashpoints.
        if (m.Confrontations.Count > 0)
        {
            b.Append("<div class='card'><h2>🤬 Flashpoints</h2><table class='lst'><tr class='th'><td>Min</td><td>Level</td><td>What happened</td></tr>");
            foreach (var cf in m.Confrontations.OrderBy(x => x.Minute))
            {
                string lvl = cf.Level switch
                {
                    ConfrontationLevel.Handbags => "<span class='dim'>handbags</span>",
                    ConfrontationLevel.FaceOff => "<span class='badge yellow'>face-off</span>",
                    ConfrontationLevel.Scuffle => "<span class='badge yellow'>scuffle</span>",
                    _ => $"<span class='badge red'>BRAWL{(cf.BenchInvolved ? " · benches" : "")}</span>",
                };
                b.Append($"<tr><td class='dim'>{cf.Minute}'</td><td>{lvl}</td><td>{E(cf.Description)}</td></tr>");
            }

            b.Append("</table></div>");
        }

        // Near-misses & woodwork.
        if (m.NearMisses.Count > 0)
        {
            b.Append("<div class='card'><h2>🪵 Near misses &amp; woodwork</h2><table class='lst'><tr class='th'><td>Min</td><td>Team</td><td>What happened</td></tr>");
            foreach (var nm in m.NearMisses.OrderBy(x => x.Minute))
            {
                var (icon, _) = NearMissNarratives.Badge(nm.Kind);
                b.Append($"<tr><td class='dim'>{nm.Minute}'</td><td class='dim'>{E(nm.TeamCode)}</td><td>{icon} {E(nm.Description)}</td></tr>");
            }

            b.Append("</table></div>");
        }

        // VAR reviews.
        if (m.VarChecks.Count > 0)
        {
            b.Append("<div class='card'><h2>📺 VAR drama</h2><table class='lst'><tr class='th'><td>Min</td><td>Team</td><td>Review</td></tr>");
            foreach (var v in m.VarChecks.OrderBy(x => x.Minute))
            {
                b.Append($"<tr><td class='dim'>{v.Minute}'</td><td class='dim'>{E(v.TeamCode)}</td><td>{E(v.Description)}</td></tr>");
            }

            b.Append("</table></div>");
        }

        // Hydration / cooling breaks.
        if (m.CoolingBreaks.Count > 0)
        {
            string mins = string.Join("' and ", m.CoolingBreaks.Select(c => c.Minute.ToString()));
            b.Append($"<div class='card'><h2>🥤 Hydration breaks</h2><p class='note'>{m.TemperatureC}°C — <b>FIFA-mandated cooling breaks</b> (2026 heat protocol) were taken around the {mins}' mark to keep the players hydrated.</p></div>");
        }

        // Play-by-play commentary (also written out as a sibling .txt transcript next to this page).
        var commentary = CommentaryGenerator.Generate(m);
        b.Append("<div class='card'><h2>📻 Live commentary</h2><div class='comm'>");
        foreach (var line in commentary)
        {
            bool analyst = line.Speaker == CommentaryGenerator.Analyst;
            bool isCrowd = line.Speaker == CommentaryGenerator.CrowdVoice;
            string cls = isCrowd ? "crd" : analyst ? "co" : "pbp";
            string label = isCrowd ? "🎉 Crowd" : analyst ? "Analyst" : "Commentator";
            string text = isCrowd ? $"<i>{E(line.Text)}</i>" : E(line.Text);
            b.Append($"<div class='cl {cls}'><span class='cmin'>{line.Minute}'</span><span class='cwho'>{label}</span><span class='ctxt'>{text}</span></div>");
        }

        b.Append("</div></div>");

        Write(path, $"{m.HomeName} {m.HomeGoals}–{m.AwayGoals} {m.AwayName}", b.ToString(), Stages.DisplayName(m.Stage) + method);

        // Save the transcript alongside the HTML, exactly as requested for any downloaded game. Written
        // as UTF-8 with a BOM so accented names and dashes render correctly in any text editor.
        string transcriptPath = Path.Combine(
            Path.GetDirectoryName(path) ?? ".", Path.GetFileNameWithoutExtension(path) + "_commentary.txt");
        File.WriteAllText(transcriptPath, CommentaryGenerator.ToTranscript(m, commentary), Encoding.UTF8);
    }

    private static string BadCallLabel(BadCallType t) => t switch
    {
        BadCallType.WrongPenaltyAwarded => "Soft penalty",
        BadCallType.PenaltyDenied => "Penalty denied",
        BadCallType.WrongCard => "Wrong card",
        BadCallType.MissedCard => "Missed red",
        BadCallType.GoalWronglyDisallowed => "Goal chalked off",
        BadCallType.GoalWronglyAllowed => "Should've been off",
        _ => "Bad call",
    };

    private static string Fact(string label, string value, string sub) =>
        $"<div class='chip'><div class='chip-l'>{E(label)}</div><div class='chip-v'>{value}</div><div class='chip-s'>{sub}</div></div>";

    private static string Timeline(MatchResult m)
    {
        int maxMin = m.Method == MatchMethod.Regulation ? 90 : 120;
        double Pos(int min) => Math.Clamp(min / (double)maxMin * 100, 0, 100);
        var sb = new StringBuilder("<div class='card'><h2>Timeline</h2><div class='tl'><div class='tl-axis'></div>");

        void Mark(int minute, string teamCode, string icon, string title)
        {
            string side = teamCode == m.HomeCode ? "home" : "away";
            sb.Append($"<div class='tl-m {side}' style='left:{Px(Pos(minute))}%' title='{minute}&#39; {E(title)}'>{icon}</div>");
        }

        foreach (var g in m.Goals)
        {
            Mark(g.Minute, g.TeamCode, g.IsOwnGoal ? "🥅" : "⚽",
                (g.IsOwnGoal ? "Own goal — " : "Goal — ") + g.ScorerName + (g.IsPenalty ? " (pen)" : ""));
        }

        foreach (var c in m.Cards)
        {
            Mark(c.Minute, c.TeamCode, c.IsRed ? "🟥" : "🟨",
                (c.IsRed ? (c.IsSecondYellow ? "Second yellow → red — " : "Red card — ") : "Yellow card — ") + c.PlayerName);
        }

        foreach (var pen in m.Penalties.Where(p => p.Outcome != PenaltyOutcome.Scored))
        {
            Mark(pen.Minute, pen.TeamCode, "❌", "Penalty " + pen.Outcome.ToString().ToLowerInvariant() + " — " + pen.TakerName);
        }

        foreach (var sub in m.Substitutions)
        {
            Mark(sub.Minute, sub.TeamCode, "🔄", $"Sub — {sub.OnName} on for {sub.OffName}" + (sub.Injury ? " (injury)" : ""));
        }

        foreach (var inj in m.Injuries)
        {
            Mark(inj.Minute, inj.TeamCode, "🩹", "Injury — " + inj.PlayerName);
        }

        foreach (var s in m.SaveEvents.Where(x => x.IsAmazing))
        {
            Mark(s.Minute, s.TeamCode, "🧤", $"Amazing save {s.Rating:0.0}/10 — " + s.KeeperName);
        }

        foreach (var e in m.Errors.Where(x => !x.LedToGoal))
        {
            Mark(e.Minute, e.TeamCode, e.Kind == ErrorKind.GoalkeeperError ? "🧤" : "🤦", "Error — " + e.Description);
        }

        foreach (var bc in m.BadCalls)
        {
            string side = bc.AgainstCode.Length > 0 ? bc.AgainstCode : bc.ForCode.Length > 0 ? bc.ForCode : m.HomeCode;
            Mark(bc.Minute, side, "⚖️", BadCallLabel(bc.Type) + " — " + bc.Description);
        }

        sb.Append("<div class='tl-half' style='left:50%'></div>");
        sb.Append($"<div class='tl-end'>{maxMin}'</div></div>");
        sb.Append("<div class='tl-legend'>⚽ goal · 🟨/🟥 card · 🔄 sub · 🩹 injury · 🧤 amazing save · ❌ pen miss · 🤦 error · ⚖️ bad call <span class='dim'>· home above, away below — hover for detail</span></div></div>");
        return sb.ToString();
    }

    // ---------------------------------------------------------------- tournament bracket

    public static void TournamentToHtml(TournamentResult result, TournamentData data, string path)
    {
        string Name(string code) => code.Length == 0 ? "—" : data.Team(code).Name;
        var byId = result.KnockoutResults.ToDictionary(k => k.MatchId, k => k);

        // Resolve the knockout tree from the official feeders so the bracket draws without crossings.
        var defs = data.Bracket.Matches.ToDictionary(d => d.Id);
        var children = new Dictionary<int, List<int>>();
        foreach (var d in data.Bracket.Matches)
        {
            var kids = new List<int>();
            foreach (var feeder in new[] { d.Top, d.Bottom })
            {
                if (feeder.Kind == FeederKind.MatchWinner)
                {
                    kids.Add(feeder.MatchId);
                }
            }

            children[d.Id] = kids;
        }

        int finalId = data.Bracket.Matches.First(d => d.Stage == Stage.Final).Id;
        var slot = new Dictionary<int, double>();
        double nextLeaf = 0;
        double Assign(int id)
        {
            var kids = children.GetValueOrDefault(id) ?? new List<int>();
            if (kids.Count < 2)
            {
                double s = nextLeaf++;
                slot[id] = s;
                return s;
            }

            double a = Assign(kids[0]);
            double c = Assign(kids[1]);
            double mid = (a + c) / 2;
            slot[id] = mid;
            return mid;
        }

        Assign(finalId);

        const double BoxW = 210, BoxH = 56, ColStride = 268, RowH = 64;
        int Col(Stage s) => s switch
        {
            Stage.RoundOf32 => 0,
            Stage.RoundOf16 => 1,
            Stage.QuarterFinal => 2,
            Stage.SemiFinal => 3,
            _ => 4,
        };
        double X(int col) => col * ColStride;
        double Y(double sl) => sl * RowH;
        double width = X(4) + BoxW;
        double height = nextLeaf * RowH;

        var b = new StringBuilder();

        // Champion banner.
        if (result.ChampionCode.Length > 0)
        {
            b.Append($"<div class='champ'><div class='trophy'>🏆</div><div><div class='champ-tag'>WORLD CHAMPIONS</div><div class='champ-name'>{E(FlagName(result.ChampionCode, Name(result.ChampionCode)))}</div>");
            if (result.RunnerUpCode.Length > 0)
            {
                b.Append($"<div class='dim'>Runner-up {E(Name(result.RunnerUpCode))}");
                if (result.ThirdPlaceCode.Length > 0) b.Append($" · Third {E(Name(result.ThirdPlaceCode))}");
                b.Append("</div>");
            }

            b.Append("</div></div>");
        }

        // Bracket — full-bleed (uses the whole window width) and auto-scaled by the fit script below so
        // the entire R32→Final tree is always visible, however wide the browser window is.
        b.Append("<div class='card bracket-card'><h2>Knockout bracket</h2><div class='bracket-scroll'>");
        b.Append($"<div class='bracket' style='width:{Px(width)}px;height:{Px(height)}px'>");

        // Round headers.
        string[] heads = { "Round of 32", "Round of 16", "Quarter-finals", "Semi-finals", "Final" };
        for (int c = 0; c < heads.Length; c++)
        {
            b.Append($"<div class='round-head' style='left:{Px(X(c))}px;width:{Px(BoxW)}px'>{heads[c]}</div>");
        }

        // Connector lines (SVG behind the boxes).
        b.Append($"<svg class='lines' width='{Px(width)}' height='{Px(height)}'>");
        foreach (var (id, kids) in children)
        {
            if (kids.Count < 2 || !slot.ContainsKey(id) || !defs.ContainsKey(id))
            {
                continue;
            }

            int pCol = Col(defs[id].Stage);
            double px = X(pCol), py = Y(slot[id]) + BoxH / 2;
            foreach (var kid in kids)
            {
                if (!slot.ContainsKey(kid) || !defs.ContainsKey(kid))
                {
                    continue;
                }

                int kCol = Col(defs[kid].Stage);
                double kx = X(kCol) + BoxW, ky = Y(slot[kid]) + BoxH / 2;
                double midX = (kx + px) / 2;
                b.Append($"<path d='M{Px(kx)},{Px(ky)} L{Px(midX)},{Px(ky)} L{Px(midX)},{Px(py)} L{Px(px)},{Px(py)}' />");
            }
        }

        b.Append("</svg>");

        // Match boxes.
        foreach (var d in data.Bracket.Matches)
        {
            if (d.Stage == Stage.ThirdPlacePlayoff || !slot.ContainsKey(d.Id))
            {
                continue;
            }

            double x = X(Col(d.Stage)), y = Y(slot[d.Id]) + 24; // +header offset
            b.Append($"<div class='match' style='left:{Px(x)}px;top:{Px(y)}px;width:{Px(BoxW)}px'>");
            if (byId.TryGetValue(d.Id, out var ko))
            {
                b.Append(MatchRow(ko.Result, true, Name));
                b.Append(MatchRow(ko.Result, false, Name));
            }
            else
            {
                b.Append("<div class='team'><span>—</span></div><div class='team'><span>—</span></div>");
            }

            b.Append("</div>");
        }

        b.Append("</div></div></div>");

        // Third-place playoff.
        var third = result.KnockoutResults.FirstOrDefault(k => k.Stage == Stage.ThirdPlacePlayoff);
        if (third is not null)
        {
            b.Append("<div class='card'><h2>Third-place play-off</h2><div class='match standalone'>");
            b.Append(MatchRow(third.Result, true, Name));
            b.Append(MatchRow(third.Result, false, Name));
            b.Append("</div></div>");
        }

        // Group standings.
        b.Append("<div class='card'><h2>Group stage — final standings</h2><div class='groups'>");
        var qThirdGroups = result.QualifiedThirds.Select(t => t.Group).ToHashSet();
        foreach (var (group, standings) in result.GroupStandings.OrderBy(g => g.Key))
        {
            b.Append($"<div class='grp'><div class='grp-h'>Group {group}</div><table class='std'>");
            b.Append("<tr class='th'><td>#</td><td>Team</td><td class='r'>P</td><td class='r'>W</td><td class='r'>D</td><td class='r'>L</td><td class='r'>GF</td><td class='r'>GA</td><td class='r'>GD</td><td class='r'>Pts</td></tr>");
            foreach (var s in standings)
            {
                bool adv = s.Rank <= 2 || (s.Rank == 3 && qThirdGroups.Contains(group));
                string gd = (s.GoalDifference >= 0 ? "+" : "") + s.GoalDifference;
                b.Append($"<tr class='{(adv ? "adv" : "")}'><td class='dim'>{s.Rank}</td><td>{E(FlagName(s.Code, Name(s.Code)))}</td><td class='r dim'>{s.Played}</td><td class='r dim'>{s.Won}</td><td class='r dim'>{s.Drawn}</td><td class='r dim'>{s.Lost}</td><td class='r dim'>{s.GoalsFor}</td><td class='r dim'>{s.GoalsAgainst}</td><td class='r dim'>{gd}</td><td class='r'><b>{s.Points}</b></td></tr>");
            }

            b.Append("</table></div>");
        }

        b.Append("</div></div>");

        // Best third-placed teams.
        if (result.QualifiedThirds.Count > 0 || result.EliminatedThirds.Count > 0)
        {
            b.Append("<div class='card'><h2>Best third-placed teams</h2><table class='lst'><tr class='th'><td>#</td><td>Team</td><td class='r'>Pts</td><td class='r'>GD</td><td class='r'>GF</td><td class='r'>Status</td></tr>");
            int rank = 1;
            void ThirdRow(TeamStanding s, bool qualified)
            {
                string gd = (s.GoalDifference >= 0 ? "+" : "") + s.GoalDifference;
                string mark = qualified ? "<span class='pill scored'>✓ qualified</span>" : "<span class='pill missed'>out</span>";
                b.Append($"<tr class='{(qualified ? "adv" : "")}'><td class='dim'>{rank++}</td><td>{E(FlagName(s.Code, Name(s.Code)))} <span class='dim'>(Grp {s.Group})</span></td><td class='r'><b>{s.Points}</b></td><td class='r dim'>{gd}</td><td class='r dim'>{s.GoalsFor}</td><td class='r'>{mark}</td></tr>");
            }

            foreach (var s in result.QualifiedThirds) ThirdRow(s, true);
            foreach (var s in result.EliminatedThirds) ThirdRow(s, false);
            b.Append("</table></div>");
        }

        // How far each team got.
        string FinishLabel(string code) =>
            code == result.ChampionCode ? "🏆 Champion"
            : code == result.RunnerUpCode ? "🥈 Runner-up"
            : code == result.ThirdPlaceCode ? "🥉 Third place"
            : Stages.DisplayName(result.FurthestStage.TryGetValue(code, out var st) ? st : Stage.Group);
        int FinishDepth(string code) =>
            code == result.ChampionCode ? 8
            : code == result.RunnerUpCode ? 7
            : code == result.ThirdPlaceCode ? 6
            : Stages.Rank(result.FurthestStage.TryGetValue(code, out var st) ? st : Stage.Group);
        b.Append("<div class='card'><h2>How far each team got</h2><table class='lst'><tr class='th'><td>Team</td><td>Reached</td></tr>");
        foreach (var t in data.Teams
            .OrderByDescending(t => FinishDepth(t.Code))
            .ThenBy(t => data.Team(t.Code).Name, StringComparer.Ordinal))
        {
            b.Append($"<tr><td>{E(FlagName(t.Code, data.Team(t.Code).Name))}</td><td class='dim'>{E(FinishLabel(t.Code))}</td></tr>");
        }

        b.Append("</table></div>");

        // Scale the fixed-size bracket down to fit the window width (never up), so the whole R32→Final
        // tree is always fully visible — re-fitting on resize.
        b.Append(
            "<script>(function(){function fit(){var b=document.querySelector('.bracket');if(!b)return;" +
            "var h=b.parentElement;b.style.transform='none';var w=b.offsetWidth,a=h.clientWidth;" +
            "var s=Math.min(1,a/w);b.style.transformOrigin='top left';b.style.transform='scale('+s+')';" +
            "h.style.height=(b.offsetHeight*s)+'px';}window.addEventListener('load',fit);" +
            "window.addEventListener('resize',fit);fit();})();</script>");

        Write(path, $"World Cup 2026 — {Name(result.ChampionCode)} champions", b.ToString(),
            $"{result.ParameterLabel} · seed {result.Seed}");
    }

    private static string MatchRow(MatchResult m, bool home, Func<string, string> name)
    {
        string code = home ? m.HomeCode : m.AwayCode;
        int gf = home ? m.HomeGoals : m.AwayGoals;
        bool won = m.WinnerCode == code;
        string pens = m.Method == MatchMethod.Penalties
            ? $" <span class='pens'>({(home ? m.HomePens : m.AwayPens)})</span>" : "";
        return $"<div class='team {(won ? "win" : "")}'><span class='nm'>{E(FlagName(code, name(code)))}</span><span class='sc'>{gf}{pens}</span></div>";
    }

    // ---------------------------------------------------------------- tournament Monte Carlo odds

    public static void TournamentMonteCarloToHtml(TournamentMonteCarloReport r, string path)
    {
        var b = new StringBuilder();
        var champ = r.MostLikelyChampion;
        var final = r.MostLikelyFinal;

        b.Append($"<div class='hero'><div class='hero-tag'>TOURNAMENT FORECAST · {r.Iterations:N0} simulations{(r.CurrentState ? " · from current state" : "")}</div>");
        if (champ is not null)
        {
            b.Append($"<div class='vs'><div style='font-size:46px'>🏆</div><div class='side win'>{E(FlagName(champ.Code, champ.Name))}</div></div>");
            b.Append($"<div class='hero-sub'>Most likely champion ({P(champ.Champion)})");
            if (final is not null)
            {
                b.Append($"<br>Most likely final: <b>{E(FlagName(final.CodeA, final.NameA))}</b> v <b>{E(FlagName(final.CodeB, final.NameB))}</b> ({P(final.Probability)})");
            }

            b.Append("</div>");
        }

        b.Append("</div>");

        b.Append("<div class='card'><h2>Title odds</h2><div class='wdl'>");
        foreach (var t in r.Teams.OrderByDescending(t => t.Champion).Take(12))
        {
            b.Append(WdlBar(FlagName(t.Code, t.Name), t.Champion, "home"));
        }

        b.Append("</div></div>");

        b.Append("<div class='card'><h2>Advancement probabilities</h2><table class='lst'><tr class='th'><td>Team</td><td class='r'>Champ</td><td class='r'>Final</td><td class='r'>SF</td><td class='r'>QF</td><td class='r'>R16</td><td class='r'>Top grp</td><td class='r'>xPts</td></tr>");
        foreach (var t in r.Teams.Where(x => x.ReachedR32 > 0.0005 || x.Champion > 0).Take(32))
        {
            b.Append($"<tr><td>{E(FlagName(t.Code, t.Name))}</td><td class='r'><b>{P(t.Champion)}</b></td><td class='r dim'>{P(t.ReachedFinal)}</td><td class='r dim'>{P(t.ReachedSemi)}</td><td class='r dim'>{P(t.ReachedQuarter)}</td><td class='r dim'>{P(t.ReachedR16)}</td><td class='r dim'>{P(t.TopGroup)}</td><td class='r dim'>{N1(t.ExpectedGroupPoints)}</td></tr>");
        }

        b.Append("</table></div>");

        if (r.TopFinalMatchups.Count > 0)
        {
            b.Append("<div class='card'><h2>Most likely final matchups</h2><table class='lst'>");
            foreach (var f in r.TopFinalMatchups.Take(10))
            {
                b.Append($"<tr><td>{E(FlagName(f.CodeA, f.NameA))} v {E(FlagName(f.CodeB, f.NameB))}</td><td class='bar-cell'>{MiniBar(f.Probability)}</td><td class='r'>{P(f.Probability)}</td></tr>");
            }

            b.Append("</table></div>");
        }

        Write(path, "World Cup 2026 — Tournament forecast", b.ToString(),
            $"{r.ParameterLabel} · seed {r.Seed} · {r.Iterations:N0} simulations");
    }

    // ---------------------------------------------------------------- detailed-MC stats / leaderboards

    public static void StatsToHtml(StatsReport r, string path)
    {
        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>TOURNAMENT STATS · {(r.Tournaments > 1 ? r.Tournaments.ToString("N0") + " simulated tournaments" : "single tournament")}</div><div class='vs'><div class='side'>Leaderboards &amp; records</div></div></div>");

        void Table(string title, string head, IEnumerable<string> rows)
        {
            b.Append($"<div class='card'><h2>{title}</h2><table class='lst'><tr class='th'>{head}</tr>");
            foreach (var row in rows) b.Append(row);
            b.Append("</table></div>");
        }

        int i;
        if (r.GoldenBoot.Count > 0)
        {
            i = 1;
            Table("🥇 Golden Boot", "<td>#</td><td>Player</td><td>Team</td><td class='r'>Goals</td><td class='r'>Assists</td><td class='r'>Mins</td>",
                r.GoldenBoot.Select(p => $"<tr><td class='dim'>{i++}</td><td>{E(p.Name)}</td><td class='dim'>{E(FlagName(p.TeamCode, p.TeamCode))}</td><td class='r'><b>{p.Goals}</b></td><td class='r dim'>{p.Assists}</td><td class='r dim'>{p.Minutes}</td></tr>"));
        }

        if (r.Mvp.Count > 0)
        {
            i = 1;
            Table("⭐ MVP / Golden Ball", "<td>#</td><td>Player</td><td>Team</td><td class='r'>Score</td><td class='r'>G</td><td class='r'>A</td><td class='r'>CS</td><td>Reached</td>",
                r.Mvp.Select(p => $"<tr><td class='dim'>{i++}</td><td>{E(p.Name)}</td><td class='dim'>{E(FlagName(p.TeamCode, p.TeamCode))}</td><td class='r'><b>{N1(p.Score)}</b></td><td class='r dim'>{p.Goals}</td><td class='r dim'>{p.Assists}</td><td class='r dim'>{p.CleanSheets}</td><td class='dim'>{E(p.FurthestStage)}</td></tr>"));
        }

        if (r.TopAssists.Count > 0)
        {
            i = 1;
            Table("🎯 Most assists", "<td>#</td><td>Player</td><td>Team</td><td class='r'>Assists</td><td class='r'>Goals</td>",
                r.TopAssists.Select(p => $"<tr><td class='dim'>{i++}</td><td>{E(p.Name)}</td><td class='dim'>{E(FlagName(p.TeamCode, p.TeamCode))}</td><td class='r'><b>{p.Assists}</b></td><td class='r dim'>{p.Goals}</td></tr>"));
        }

        if (r.GoldenGlove.Count > 0)
        {
            i = 1;
            Table("🧤 Golden Glove", "<td>#</td><td>Goalkeeper</td><td>Team</td><td class='r'>Clean sheets</td><td class='r'>Saves</td><td class='r'>Amazing</td><td class='r'>Conceded</td>",
                r.GoldenGlove.Select(p => $"<tr><td class='dim'>{i++}</td><td>{E(p.Name)}</td><td class='dim'>{E(FlagName(p.TeamCode, p.TeamCode))}</td><td class='r'><b>{p.CleanSheets}</b></td><td class='r dim'>{p.Saves}</td><td class='r'>{(p.AmazingSaves > 0 ? $"<span class='verg v-hi'>{p.AmazingSaves} 🔥</span>" : "0")}</td><td class='r dim'>{p.GoalsConceded}</td></tr>"));
        }

        if (r.PenaltyTakers.Count > 0)
        {
            i = 1;
            Table("⚽ Penalty takers", "<td>#</td><td>Player</td><td>Team</td><td class='r'>Scored</td><td class='r'>Missed</td><td class='r'>Record</td>",
                r.PenaltyTakers.Select(p => $"<tr><td class='dim'>{i++}</td><td>{E(p.Name)}</td><td class='dim'>{E(FlagName(p.TeamCode, p.TeamCode))}</td><td class='r'><b>{p.Scored}</b></td><td class='r'>{(p.Missed > 0 ? $"<span class='pill missed'>{p.Missed}</span>" : "0")}</td><td class='r dim'>{P((double)p.Scored / Math.Max(1, p.Scored + p.Missed))}</td></tr>"));
        }

        if (r.GoalsByType.Count > 0)
        {
            Table("Goals by type", "<td>Type</td><td class='r'>Goals</td><td class='r'>Share</td>",
                r.GoalsByType.Select(g => $"<tr><td>{E(g.Type)}</td><td class='r'>{g.Count}</td><td class='r dim'>{N1(g.Percent)}%</td></tr>"));
        }

        if (r.Teams.Count > 0)
        {
            Table("Team stats", "<td>Team</td><td class='r'>GF</td><td class='r'>GA</td><td class='r'>CS</td><td class='r'>Shots</td><td class='r'>SoT</td><td class='r'>Corners</td><td class='r'>Poss</td><td class='r'>Y</td><td class='r'>R</td>",
                r.Teams.Take(16).Select(t => $"<tr><td>{E(FlagName(t.TeamCode, t.Name))}</td><td class='r'>{t.GoalsFor}</td><td class='r dim'>{t.GoalsAgainst}</td><td class='r dim'>{t.CleanSheets}</td><td class='r dim'>{t.Shots}</td><td class='r dim'>{t.ShotsOnTarget}</td><td class='r dim'>{t.Corners}</td><td class='r dim'>{t.PossessionAvg:0}%</td><td class='r dim'>{t.Yellows}</td><td class='r dim'>{t.Reds}</td></tr>"));
        }

        if (r.MostYellows.Count > 0)
        {
            i = 1;
            Table("🟨 Most yellow cards", "<td>#</td><td>Player</td><td>Team</td><td class='r'>Yellows</td><td class='r'>Reds</td>",
                r.MostYellows.Select(p => $"<tr><td class='dim'>{i++}</td><td>{E(p.Name)}</td><td class='dim'>{E(FlagName(p.TeamCode, p.TeamCode))}</td><td class='r'><b>{p.Yellows}</b></td><td class='r dim'>{p.Reds}</td></tr>"));
        }

        if (r.MostReds.Count > 0)
        {
            i = 1;
            Table("🟥 Most red cards", "<td>#</td><td>Player</td><td>Team</td><td class='r'>Reds</td><td class='r'>Yellows</td>",
                r.MostReds.Select(p => $"<tr><td class='dim'>{i++}</td><td>{E(p.Name)}</td><td class='dim'>{E(FlagName(p.TeamCode, p.TeamCode))}</td><td class='r'><b>{p.Reds}</b></td><td class='r dim'>{p.Yellows}</td></tr>"));
        }

        if (r.CrazyStats.Count > 0)
        {
            Table("🤯 Records", "<td>Record</td><td>Detail</td>",
                r.CrazyStats.Select(rec => $"<tr><td><b>{E(rec.Category)}</b></td><td class='dim'>{E(rec.Description)}</td></tr>"));
        }

        if (r.TotalInjuries > 0)
        {
            Table("🩹 Injury list", "<td>Min</td><td>Player</td><td>Team</td><td>Diagnosis</td><td>Severity</td><td>Out for</td><td class='r'>Replaced</td>",
                r.Injuries.Select(inj => $"<tr><td class='dim'>{inj.Minute}'</td><td>🩹 {E(inj.PlayerName)}</td><td class='dim'>{E(FlagName(inj.TeamCode, inj.TeamCode))}</td><td>{E(string.IsNullOrEmpty(inj.Diagnosis) ? "—" : inj.Diagnosis)}</td><td class='dim'>{inj.Severity}</td><td class='dim'>{E(InjuryCatalog.RecoveryText(inj.RecoveryDays))}</td><td class='r'>{(inj.Replaced ? "<span class='dim'>subbed off</span>" : "<span class='pill missed'>played on</span>")}</td></tr>"));
        }

        if (r.Tournaments > 1)
        {
            void FreqTable(string title, IReadOnlyList<AwardFrequencyRow> rows)
            {
                if (rows.Count == 0) return;
                int n = 1;
                Table(title, "<td>#</td><td>Player</td><td>Team</td><td class='r'>Wins</td><td class='r'>Frequency</td><td class='r'>Avg tally</td>",
                    rows.Select(row => $"<tr><td class='dim'>{n++}</td><td>{E(row.Name)}</td><td class='dim'>{E(FlagName(row.TeamCode, row.TeamCode))}</td><td class='r'><b>{row.Wins}</b></td><td class='r dim'>{P(row.Frequency)}</td><td class='r dim'>{N1(row.AverageTally)}</td></tr>"));
            }

            FreqTable("🥇 Golden Boot — most often won", r.GoldenBootFrequency);
            FreqTable("⭐ MVP — most often won", r.MvpFrequency);
            FreqTable("🧤 Golden Glove — most often won", r.GoldenGloveFrequency);
        }

        Write(path, "World Cup 2026 — Tournament stats", b.ToString(), $"{r.Tournaments:N0} simulated tournament(s)");
    }

    // ---------------------------------------------------------------- whole-group outlook

    public static void GroupOutlookToHtml(GroupOutlook g, string path)
    {
        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>GROUP {g.Group} · FULL OUTLOOK</div>");
        b.Append("<div class='vs'><div class='side'>All four teams</div></div>");
        b.Append($"<div class='hero-sub'>{(g.GroupComplete ? "Group complete" : $"{g.Iterations:N0} simulations · who finishes where")}</div></div>");

        b.Append("<div class='card'><h2>Standings &amp; finishing probabilities</h2><table class='std wide'>");
        b.Append("<tr class='th'><td>#</td><td>Team</td><td class='r'>Pts</td><td class='r'>🥇 Win</td><td class='r'>🥈 2nd</td><td class='r'>✅ Adv</td><td class='r'>⚠️ 3rd</td><td class='r'>❌ Out</td><td>Status</td></tr>");
        foreach (var t in g.Teams)
        {
            var s = t.Standing;
            string cls = s.Rank <= 2 ? "adv" : "";
            string outCell = t.Eliminated > 0 ? $"<span class='bad'>{P(t.Eliminated)}</span>" : P(t.Eliminated);
            string statusColour =
                t.ClinchedWinGroup || t.ClinchedAdvance ? "var(--green)"
                : t.Status == "Eliminated" ? "var(--red)"
                : t.CannotAdvance ? "var(--gold)"
                : "var(--dim)";
            string statusCell = $"<td><span style='color:{statusColour}'>{E(t.Status)}</span></td>";
            b.Append($"<tr class='{cls}'><td class='dim'>{s.Rank}</td><td>{E(FlagName(s.Code, s.Name))}</td><td class='r'><b>{s.Points}</b></td><td class='r'><b>{P(t.WinGroup)}</b></td><td class='r dim'>{P(t.RunnerUp)}</td><td class='r'>{P(t.AdvanceDirect)}</td><td class='r dim'>{P(t.ThirdPlace)}</td><td class='r'>{outCell}</td>{statusCell}</tr>");
        }

        b.Append("</table><p class='note'>Win / 2nd / Adv = direct qualification · 3rd may advance as one of the eight best third-placed teams.</p></div>");

        if (!g.GroupComplete && g.RemainingFixtures.Count > 0)
        {
            b.Append("<div class='card'><h2>Remaining fixtures</h2><table class='lst'><tr class='th'><td>Fixture</td><td class='r'>Home</td><td class='r'>Draw</td><td class='r'>Away</td></tr>");
            foreach (var f in g.RemainingFixtures)
            {
                b.Append($"<tr><td>{E(FlagName(f.HomeCode, f.HomeName))} <span class='dim'>v</span> {E(FlagName(f.AwayCode, f.AwayName))}</td><td class='r'>{P(f.HomeWin)}</td><td class='r dim'>{P(f.Draw)}</td><td class='r'>{P(f.AwayWin)}</td></tr>");
            }

            b.Append("</table></div>");
        }

        Write(path, $"World Cup 2026 — Group {g.Group} outlook", b.ToString(),
            $"{g.ParameterLabel} · seed {g.Seed} · {g.Iterations:N0} sims");
    }

    // ---------------------------------------------------------------- group path to victory / defeat

    public static void GroupPathToHtml(GroupPathAnalysis a, string path)
    {
        var b = new StringBuilder();

        // Hero: team, group, and the three headline numbers.
        b.Append($"<div class='hero'><div class='hero-tag'>PATH TO VICTORY &amp; DEFEAT · GROUP {a.Group}</div>");
        b.Append($"<div class='vs'><div class='side win'>{E(FlagName(a.TeamCode, a.TeamName))}</div></div>");
        if (a.GroupComplete)
        {
            b.Append($"<div class='hero-sub'>Group complete — finished <b>{E(Ordinal(a.FinalRankIfComplete))}</b> ({E(TierWord(a.FinalRankIfComplete))})</div></div>");
        }
        else
        {
            b.Append($"<div class='hero-sub'>{a.Iterations:N0} simulations · {a.TotalCombinations} result combinations remain</div></div>");
            b.Append("<div class='card'><div class='chips'>");
            b.Append(Chip("🥇 Win the group", a.WinGroup));
            b.Append(Chip("🥈 Runner-up", a.RunnerUp));
            b.Append(Chip("✅ Advance (top 2)", a.AdvanceDirect));
            b.Append(Chip("⚠️ Finish 3rd", a.ThirdPlace));
            b.Append(Chip("❌ Eliminated (last)", a.Eliminated));
            b.Append("</div>");
            string clinch = a.ClinchedWinGroup ? "Already guaranteed top spot."
                : a.ClinchedAdvance ? "Already qualified for the knockouts."
                : a.CannotAdvance ? "Can no longer finish in the top two."
                : a.CannotWinGroup ? "Can no longer win the group."
                : "";
            if (clinch.Length > 0)
            {
                b.Append($"<p class='note'><b>{E(clinch)}</b></p>");
            }

            b.Append("</div>");
        }

        // Current standings.
        b.Append($"<div class='card'><h2>Group {a.Group} — current standings</h2><table class='std wide'>");
        b.Append("<tr class='th'><td>#</td><td>Team</td><td class='r'>P</td><td class='r'>W</td><td class='r'>D</td><td class='r'>L</td><td class='r'>GF</td><td class='r'>GA</td><td class='r'>GD</td><td class='r'>Pts</td></tr>");
        foreach (var s in a.Standings)
        {
            string cls = (s.Rank <= 2 ? "adv" : "") + (s.IsSelected ? " sel" : "");
            string gd = (s.GoalDifference >= 0 ? "+" : "") + s.GoalDifference;
            b.Append($"<tr class='{cls.Trim()}'><td class='dim'>{s.Rank}</td><td>{(s.IsSelected ? "➤ " : "")}{E(FlagName(s.Code, s.Name))}</td><td class='r dim'>{s.Played}</td><td class='r dim'>{s.Won}</td><td class='r dim'>{s.Drawn}</td><td class='r dim'>{s.Lost}</td><td class='r dim'>{s.GoalsFor}</td><td class='r dim'>{s.GoalsAgainst}</td><td class='r dim'>{gd}</td><td class='r'><b>{s.Points}</b></td></tr>");
        }

        b.Append("</table><p class='note'>Top two qualify directly · 3rd may advance as one of the eight best third-placed teams.</p></div>");

        if (a.GroupComplete)
        {
            Write(path, $"Group {a.Group} — {a.TeamName}", b.ToString(), $"{a.ParameterLabel} · group complete");
            return;
        }

        // Outlook bars.
        b.Append("<div class='card'><h2>Finishing outlook</h2><div class='wdl'>");
        b.Append(WdlBar("🥇 Win the group", a.WinGroup, "home"));
        b.Append(WdlBar("🥈 Runner-up", a.RunnerUp, "home"));
        b.Append(WdlBar("⚠️ Finish 3rd (best-third lottery)", a.ThirdPlace, "draw"));
        b.Append(WdlBar("❌ Eliminated (last)", a.Eliminated, "elim"));
        b.Append("</div></div>");

        // Remaining fixtures with odds.
        b.Append("<div class='card'><h2>Remaining group fixtures</h2><table class='lst'><tr class='th'><td>Fixture</td><td class='r'>Home</td><td class='r'>Draw</td><td class='r'>Away</td></tr>");
        foreach (var f in a.RemainingFixtures)
        {
            string marker = f.InvolvesSelected ? "<span class='you'>➤</span> " : "";
            b.Append($"<tr><td>{marker}{E(FlagName(f.HomeCode, f.HomeName))} <span class='dim'>v</span> {E(FlagName(f.AwayCode, f.AwayName))}</td><td class='r'>{P(f.HomeWin)}</td><td class='r dim'>{P(f.Draw)}</td><td class='r'>{P(f.AwayWin)}</td></tr>");
        }

        b.Append("</table></div>");

        // What your own result means.
        if (a.OwnResultBranches.Count > 0)
        {
            string h = a.OwnRemaining == 1 ? "What your last group game means" : "What your remaining games need to yield";
            b.Append($"<div class='card'><h2>{E(h)}</h2><table class='lst'><tr class='th'><td>Your result</td><td class='r'>Chance</td><td class='r'>→ Win group</td><td class='r'>→ Advance</td><td class='r'>→ Last</td><td>Verdict</td></tr>");
            foreach (var br in a.OwnResultBranches)
            {
                b.Append($"<tr><td><b>{E(br.Label)}</b></td><td class='r dim'>{P(br.Probability)}</td><td class='r'>{P(br.WinGroup)}</td><td class='r'>{P(br.Advance)}</td><td class='r'>{(br.Eliminated > 0 ? $"<span class='bad'>{P(br.Eliminated)}</span>" : P(br.Eliminated))}</td><td class='dim'>{E(br.Verdict)}</td></tr>");
            }

            b.Append("</table></div>");
        }

        // Path to victory / defeat, side by side.
        b.Append("<div class='grid2'>");
        b.Append(ScenarioCard("🏆 Path to victory", "win the group", a.VictoryScenarios, a.VictoryMass, "good"));
        b.Append(ScenarioCard("❌ Path to defeat", "finish last (eliminated)", a.DefeatScenarios, a.DefeatMass, "bad"));
        b.Append("</div>");

        Write(path, $"Group {a.Group} — {a.TeamName} path to victory &amp; defeat", b.ToString(),
            $"{a.ParameterLabel} · seed {a.Seed} · {a.Iterations:N0} simulations");
    }

    private static string ScenarioCard(
        string title, string goal, IReadOnlyList<GroupPathScenario> scenarios, double mass, string accent)
    {
        var b = new StringBuilder();
        b.Append($"<div class='card path {accent}'><h2>{E(title)}</h2>");
        if (scenarios.Count == 0)
        {
            b.Append($"<p class='note'>No combination of the remaining results can {E(goal)} — it is off the table.</p></div>");
            return b.ToString();
        }

        b.Append($"<p class='note'>Combinations that {E(goal)} — together ≈ <b>{P(mass)}</b> likely.</p>");
        b.Append("<table class='lst'><tr class='th'><td>If…</td><td class='r'>Finish</td><td class='r'>Likelihood</td></tr>");
        foreach (var s in scenarios.Take(12))
        {
            string combo = string.Join(" · ", s.Outcomes.Select(o => o.Description));
            string finish = s.BestRank == s.WorstRank
                ? Ordinal(s.BestRank)
                : $"{Ordinal(s.BestRank)}–{Ordinal(s.WorstRank)}*";
            b.Append($"<tr><td>{E(combo)}</td><td class='r'><span class='pill {accent}'>{finish}</span></td><td class='r'>{P(s.Probability)}</td></tr>");
        }

        b.Append("</table>");
        if (scenarios.Count > 12)
        {
            b.Append($"<p class='note'>Showing the 12 most likely of {scenarios.Count} combinations.</p>");
        }

        b.Append("<p class='note'>* place then settled on goal difference / head-to-head.</p></div>");
        return b.ToString();
    }

    private static string Ordinal(int rank) => rank switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{rank}th",
    };

    private static string TierWord(int rank) => rank switch
    {
        1 => "won the group",
        2 => "qualified as runner-up",
        3 => "into the best-third lottery",
        _ => "eliminated",
    };

    /// <summary>A team's "road to glory": reach/win odds per knockout round + likely opponents.</summary>
    public static void RoadToGloryToHtml(RoadToGloryReport r, string path)
    {
        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>ROAD TO GLORY · {r.Iterations:N0} simulations</div>");
        b.Append($"<div class='vs'><div class='side' style='flex:none'>{E(r.TeamName)}</div></div>");
        b.Append($"<div class='hero-sub'>Champions <b>{P(r.ChampionProbability)}</b> ({E(OddsConverter.All(r.ChampionProbability))}) · reach the final <b>{P(r.FinalProbability)}</b></div></div>");

        b.Append("<div class='card'><h2>Round by round</h2><table class='lst'>");
        b.Append("<tr class='th'><td>Round</td><td class='r'>Reach</td><td class='r'>Odds</td><td class='r'>Win round</td><td>Most likely opponents</td></tr>");
        foreach (var s in r.Stages)
        {
            string opps = s.LikelyOpponents.Count == 0 ? "<span class='dim'>—</span>"
                : string.Join(" · ", s.LikelyOpponents.Take(3).Select(o => $"{E(o.Name)} <span class='dim'>{P(o.Probability)}</span>"));
            b.Append($"<tr><td>{E(s.StageName)}</td><td class='r'>{P(s.ReachProbability)}</td><td class='r dim'>{E(OddsConverter.Decimal(s.ReachProbability))}</td><td class='r'>{P(s.WinProbability)}</td><td>{opps}</td></tr>");
        }

        b.Append("</table><p class='note'>Reach = chance of getting to that round; Win round = chance of winning it; opponent odds are conditional on getting there.</p></div>");
        Write(path, $"World Cup 2026 — {r.TeamName} road to glory", b.ToString(), $"{r.Iterations:N0} simulations");
    }

    /// <summary>The outright odds board: each team's title/final/group odds in %, decimal, fractional and American.</summary>
    public static void OddsBoardToHtml(TournamentMonteCarloReport r, string path)
    {
        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>OUTRIGHT ODDS · {r.Iterations:N0} tournaments</div>");
        if (r.MostLikelyChampion is { } fav)
        {
            b.Append($"<div class='hero-sub' style='margin-top:8px'>Favourites: <b>{E(fav.Name)}</b> at <b>{P(fav.Champion)}</b> ({E(OddsConverter.All(fav.Champion))})</div>");
        }

        b.Append("</div>");
        b.Append("<div class='card'><h2>To win the World Cup</h2><table class='lst'>");
        b.Append("<tr class='th'><td>#</td><td>Team</td><td>Grp</td><td class='r'>Title</td><td class='r'>Decimal</td><td class='r'>Fractional</td><td class='r'>US</td><td class='r'>Final</td><td class='r'>Win grp</td></tr>");
        int i = 1;
        foreach (var t in r.Teams.Where(x => x.Champion > 0).OrderByDescending(x => x.Champion).Take(32))
        {
            b.Append($"<tr><td class='dim'>{i++}</td><td>{E(t.Name)}</td><td class='dim'>{t.Group}</td><td class='r'><b>{P(t.Champion)}</b></td><td class='r dim'>{E(OddsConverter.Decimal(t.Champion))}</td><td class='r dim'>{E(OddsConverter.Fractional(t.Champion))}</td><td class='r dim'>{E(OddsConverter.American(t.Champion))}</td><td class='r'>{P(t.ReachedFinal)}</td><td class='r'>{P(t.TopGroup)}</td></tr>");
        }

        b.Append("</table><p class='note'>Fair odds (no bookmaker margin): decimal = 1/p, plus fractional and American moneyline.</p></div>");
        Write(path, "World Cup 2026 — Outright odds board", b.ToString(), $"{r.ParameterLabel} · seed {r.Seed} · {r.Iterations:N0} tournaments");
    }

    /// <summary>Model-accuracy backtest: calibration + every played match scored against the model's prediction.</summary>
    public static void BacktestToHtml(BacktestReport r, string path)
    {
        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>MODEL ACCURACY · {r.Matches} matches</div>");
        b.Append($"<div class='hero-sub' style='margin-top:8px'>Favourite called correctly <b>{P(r.FavouriteHitRate)}</b> · Brier <b>{r.BrierScore.ToString("0.000", Inv)}</b> · log-loss <b>{r.LogLoss.ToString("0.000", Inv)}</b> <span class='dim'>(lower is better)</span></div></div>");

        b.Append("<div class='card'><h2>Calibration — predicted vs observed</h2><table class='lst'>");
        b.Append("<tr class='th'><td>Predicted band</td><td class='r'>Cases</td><td class='r'>Avg predicted</td><td class='r'>Actually happened</td></tr>");
        foreach (var bin in r.Calibration)
        {
            b.Append($"<tr><td>{bin.LowEdge.ToString("0%", Inv)}–{bin.HighEdge.ToString("0%", Inv)}</td><td class='r dim'>{bin.Count}</td><td class='r'>{P(bin.PredictedAvg)}</td><td class='r'>{P(bin.ObservedFreq)}</td></tr>");
        }

        b.Append("</table></div>");

        b.Append("<div class='card'><h2>Every played match</h2><table class='lst'>");
        b.Append("<tr class='th'><td>Match</td><td class='r'>Score</td><td class='r'>P(home / draw / away)</td><td>Predicted</td><td>Actual</td><td class='r'>Hit</td></tr>");
        foreach (var row in r.Rows)
        {
            b.Append($"<tr><td>{E(row.HomeName)} <span class='dim'>v</span> {E(row.AwayName)}</td><td class='r'>{row.HomeGoals}–{row.AwayGoals}</td><td class='r dim'>{P(row.PHome)} / {P(row.PDraw)} / {P(row.PAway)}</td><td>{E(row.Predicted)}</td><td>{E(row.Actual)}</td><td class='r'>{(row.Hit ? "✓" : "·")}</td></tr>");
        }

        b.Append("</table><p class='note'>Brier and log-loss score the calibrated probabilities (not just the pick); a well-calibrated model's bands match the 'actually happened' column.</p></div>");
        Write(path, "World Cup 2026 — Model accuracy", b.ToString(), $"{r.Matches} matches scored");
    }

    /// <summary>Writes a linked tournament report bundle (index + bracket + stats) into <paramref name="dir"/>;
    /// returns the index path.</summary>
    public static string TournamentBundle(TournamentResult result, StatsReport stats, TournamentData data, string dir)
    {
        Directory.CreateDirectory(dir);
        TournamentToHtml(result, data, Path.Combine(dir, "bracket.html"));
        StatsToHtml(stats, Path.Combine(dir, "stats.html"));

        string champ = result.ChampionCode.Length > 0 ? data.Team(result.ChampionCode).Name : "—";
        string runnerUp = result.RunnerUpCode.Length > 0 ? data.Team(result.RunnerUpCode).Name : "—";
        var b = new StringBuilder();
        b.Append("<div class='hero'><div class='hero-tag'>WORLD CUP 2026 · TOURNAMENT REPORT</div>");
        b.Append($"<div class='vs'><div class='side' style='flex:none'>🏆 {E(champ)}</div></div>");
        b.Append($"<div class='hero-sub'>Champions · runners-up {E(runnerUp)}</div></div>");
        b.Append("<div class='card'><h2>Reports</h2><table class='lst'>");
        b.Append("<tr><td><a style='color:#3da5ff;font-weight:700' href='bracket.html'>Bracket &amp; results &rarr;</a></td><td class='dim'>group standings, knockout tree, best-third race, how far each team got</td></tr>");
        b.Append("<tr><td><a style='color:#3da5ff;font-weight:700' href='stats.html'>Statistics &rarr;</a></td><td class='dim'>golden boot, golden glove, MVP, discipline, injuries, awards</td></tr>");
        b.Append("</table></div>");

        string index = Path.Combine(dir, "index.html");
        Write(index, "World Cup 2026 — Tournament report", b.ToString(), $"champions {champ}");
        return index;
    }

    /// <summary>A goals-home × goals-away grid (0–5+) of scoreline probabilities, shaded by likelihood.</summary>
    private static string ScorelineHeatmap(MatchAggregateReport r)
    {
        const int max = 5;
        var grid = new double[max + 1, max + 1];
        double top = 0;
        foreach (var s in r.TopScorelines)
        {
            int h = Math.Min(s.HomeGoals, max), a = Math.Min(s.AwayGoals, max);
            grid[h, a] += s.Probability;
            if (grid[h, a] > top)
            {
                top = grid[h, a];
            }
        }

        if (top <= 0)
        {
            return string.Empty;
        }

        var b = new StringBuilder();
        b.Append("<div class='card'><h2>Scoreline heatmap</h2>");
        b.Append($"<p class='note'>{E(r.HomeName)} goals (rows) × {E(r.AwayName)} goals (columns) — brighter = more likely. Top row/column is 5+.</p>");
        b.Append("<table style='border-collapse:collapse;text-align:center;font-size:13px'>");
        b.Append("<tr><td style='color:var(--dim);padding:6px 10px'></td>");
        for (int a = 0; a <= max; a++)
        {
            b.Append($"<td style='color:var(--dim);padding:6px 10px;font-weight:700'>{a}{(a == max ? "+" : "")}</td>");
        }

        b.Append("</tr>");
        for (int h = 0; h <= max; h++)
        {
            b.Append($"<tr><td style='color:var(--dim);padding:6px 10px;font-weight:700'>{h}{(h == max ? "+" : "")}</td>");
            for (int a = 0; a <= max; a++)
            {
                double prob = grid[h, a];
                string alpha = (prob / top).ToString("0.00", Inv);
                string cell = prob > 0.0005 ? P(prob) : "";
                b.Append($"<td style='border:1px solid var(--line);padding:8px 11px;background:rgba(33,208,122,{alpha})'>{cell}</td>");
            }

            b.Append("</tr>");
        }

        b.Append("</table></div>");
        return b.ToString();
    }

    /// <summary>The qualification-scenarios grid: every remaining-results combination → who qualifies.</summary>
    public static void GroupPermutationsToHtml(GroupPermutations g, string path)
    {
        var b = new StringBuilder();
        b.Append($"<div class='hero'><div class='hero-tag'>GROUP {g.Group} · QUALIFICATION SCENARIOS</div>");
        b.Append($"<div class='hero-sub' style='margin-top:8px'>{g.TotalCombinations} combination(s) of {g.Fixtures.Count} remaining game(s)</div></div>");
        b.Append("<div class='card'><h2>Who qualifies under each result</h2><table class='lst'>");
        b.Append("<tr class='th'>");
        foreach (var f in g.Fixtures)
        {
            b.Append($"<td>{E(f.HomeCode)} v {E(f.AwayCode)}</td>");
        }

        b.Append("<td>1st</td><td>2nd</td><td class='dim'>3rd</td></tr>");
        foreach (var row in g.Rows)
        {
            b.Append(row.SelectedQualifies ? "<tr style='background:rgba(255,210,63,.12)'>" : "<tr>");
            for (int i = 0; i < g.Fixtures.Count; i++)
            {
                var f = g.Fixtures[i];
                string lbl = row.Outcomes[i] switch { 1 => f.HomeCode, -1 => f.AwayCode, _ => "draw" };
                string cls = row.Outcomes[i] == 0 ? " class='dim'" : string.Empty;
                b.Append($"<td{cls}>{E(lbl)}</td>");
            }

            b.Append($"<td class='gwin'><b>{E(row.FirstCode)}</b></td><td class='gwin'>{E(row.SecondCode)}</td><td class='dim'>{E(row.ThirdCode)}</td></tr>");
        }

        string hi = g.SelectedCode is not null ? $" Rows where <b>{E(g.SelectedCode)}</b> qualifies are highlighted." : string.Empty;
        b.Append($"<p class='note'>Hypothetical games use representative 1–0 / 0–0 / 0–1 margins, so goal-difference ties are deterministic; real scores can shuffle close ties.{hi}</p></div>");
        Write(path, $"World Cup 2026 — Group {g.Group} scenarios", b.ToString(), $"{g.TotalCombinations} combinations");
    }

    /// <summary>Writes a full detailed match report (box score, events, commentary) for every played-out
    /// scheduled game into <paramref name="dir"/>, plus a linking index; returns the index path.</summary>
    public static string ScheduledInstancesBundle(IReadOnlyList<MatchResult> results, string dir)
    {
        Directory.CreateDirectory(dir);

        var b = new StringBuilder();
        b.Append("<div class='hero'><div class='hero-tag'>SCHEDULED GAMES · PLAYED OUT</div>");
        b.Append($"<div class='hero-sub' style='margin-top:8px'>{results.Count} full match reports — click any game for the box score, every event, and the live commentary</div></div>");
        b.Append("<div class='card'><h2>Games</h2><table class='lst'><tr class='th'><td>Match</td><td class='r'>Score</td><td>Notes</td></tr>");
        foreach (var r in results)
        {
            string file = $"match_{r.HomeCode}_{r.AwayCode}.html";
            MatchResultToHtml(r, Path.Combine(dir, file)); // also writes the sibling commentary .txt

            string method = r.Method switch
            {
                MatchMethod.ExtraTime => " <span class='dim'>(a.e.t.)</span>",
                MatchMethod.Penalties => $" <span class='dim'>(pens {r.HomePens}–{r.AwayPens})</span>",
                _ => string.Empty,
            };

            var notes = new List<string>();
            int y = r.Cards.Count(c => !c.IsRed), rd = r.Cards.Count(c => c.IsRed);
            if (y > 0) notes.Add($"{y}🟨");
            if (rd > 0) notes.Add($"{rd}🟥");
            if (r.Penalties.Count > 0) notes.Add($"{r.Penalties.Count} pen");
            if (r.Miracle is not null) notes.Add("✨ miracle");
            if (r.Confrontations.Any(c => c.Level >= ConfrontationLevel.Scuffle)) notes.Add("🤬 flashpoint");
            if (r.Upset is { } u && u.MiracleRating >= 6.5) notes.Add($"shock {u.MiracleRating:0}/10");

            b.Append($"<tr><td><a style='color:#3da5ff;font-weight:700' href='{E(file)}'>{E(FlagName(r.HomeCode, r.HomeName))} v {E(FlagName(r.AwayCode, r.AwayName))} &rarr;</a></td>");
            b.Append($"<td class='r'><b>{r.HomeGoals}–{r.AwayGoals}</b>{method}</td><td class='dim'>{string.Join(" · ", notes)}</td></tr>");
        }

        b.Append("</table></div>");
        string index = Path.Combine(dir, "index.html");
        Write(index, "World Cup 2026 — Scheduled games played out", b.ToString(), $"{results.Count} games · one instance each");
        return index;
    }

    private static string FlagName(string code, string name) => Flags.Named(code, name);

    // ---------------------------------------------------------------- shared bits

    private static string WdlBar(string label, double p, string cls) =>
        $"<div class='wdl-row'><span class='wdl-l'>{E(label)}</span><div class='wdl-track'><div class='wdl-fill {cls}' style='width:{Px(p * 100)}%'></div></div><span class='wdl-p'>{P(p)}</span></div>";

    private static string Chip(string label, double p) =>
        $"<div class='chip'><div class='chip-v'>{P(p)}</div><div class='chip-l'>{E(label)}</div></div>";

    private static string Stat(string label, string value) =>
        $"<div class='chip'><div class='chip-v'>{value}</div><div class='chip-l'>{E(label)}</div></div>";

    private static string Cmp(string label, double home, double away, string? homeDisp = null, string? awayDisp = null)
    {
        double tot = home + away;
        double hs = tot > 0 ? home / tot * 100 : 50;
        return $"<div class='cmp'><span class='cv'>{homeDisp ?? N1(home)}</span><div class='cmp-mid'><div class='cmp-l'>{E(label)}</div><div class='cmp-track'><div class='cmp-h' style='width:{Px(hs)}%'></div><div class='cmp-a' style='width:{Px(100 - hs)}%'></div></div></div><span class='cv'>{awayDisp ?? N1(away)}</span></div>";
    }

    private static string MiniBar(double p) => $"<div class='mini'><div class='mini-f' style='width:{Px(Math.Min(100, p * 100 * 6))}%'></div></div>";

    private static string Vergazo(double v)
    {
        string c = v >= 8.5 ? "v-hi" : v >= 6.5 ? "v-mid" : v >= 4 ? "v-lo" : "v-min";
        return $"<span class='verg {c}'>{N1(v)}</span>";
    }

    private static (string Name, string Team, string Line)? PlayerOfMatch(MatchResult m)
    {
        var score = new Dictionary<string, (string Name, string Team, int G, int A, double Pts)>(StringComparer.Ordinal);
        void Add(string id, string nm, string tm, int g, int a, double pts)
        {
            var cur = score.TryGetValue(id, out var v) ? v : (Name: nm, Team: tm, G: 0, A: 0, Pts: 0.0);
            score[id] = (nm, tm, cur.G + g, cur.A + a, cur.Pts + pts);
        }

        foreach (var g in m.Goals)
        {
            if (g.IsOwnGoal) continue;
            Add(g.ScorerId, g.ScorerName, g.TeamCode, 1, 0, 2 + g.Vergazo * 0.1);
            if (g.AssistId is not null && g.AssistName is not null) Add(g.AssistId, g.AssistName, g.TeamCode, 0, 1, 1);
        }

        if (score.Count == 0) return null;
        var best = score.Values.OrderByDescending(x => x.Pts).ThenByDescending(x => x.G).First();
        string line = $"{best.G} goal{(best.G == 1 ? "" : "s")}" + (best.A > 0 ? $", {best.A} assist{(best.A == 1 ? "" : "s")}" : "");
        return (best.Name, best.Team, line);
    }

    private static (string Label, string Cls) MiracleClass(double r) =>
        r >= 9 ? ("MIRACLE", "m-hi") : r >= 7 ? ("SHOCK", "m-hi") : r >= 5 ? ("UPSET", "m-mid")
        : r >= 3 ? ("Mild surprise", "m-lo") : ("As expected", "m-min");

    private static string GoalTypeName(GoalType t) => t switch
    {
        GoalType.Penalty => "penalty",
        GoalType.LongRange => "long range",
        GoalType.BicycleKick => "bicycle kick",
        GoalType.FreeKick => "free kick",
        GoalType.Header => "header",
        GoalType.OwnGoal => "own goal",
        _ => "open play",
    };

    private static string P(double frac) => (frac * 100).ToString("0.0", Inv) + "%";
    private static string N1(double v) => v.ToString("0.0", Inv);
    private static string N2(double v) => v.ToString("0.00", Inv);
    private static string Px(double v) => v.ToString("0.##", Inv);

    private static string E(string s) => System.Net.WebUtility.HtmlEncode(s);

    private static void Write(string path, string title, string bodyInner, string footer)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html lang='en'><head><meta charset='utf-8'>");
        html.Append("<meta name='viewport' content='width=device-width,initial-scale=1'>");
        html.Append($"<title>{E(title)}</title><style>{Css}</style></head><body>");
        html.Append("<div class='wrap'>");
        html.Append($"<header class='page-h'><div class='logo'>⚽ FIFA World Cup 2026</div><div class='logo-sub'>Monte Carlo Simulator</div></header>");
        html.Append(bodyInner);
        html.Append($"<footer class='page-f'>{E(footer)} · 🔮 Predicted on {E(Ui.PredictedOnText())} · generated by WorldCupGameSimulator</footer>");
        html.Append("</div></body></html>");
        File.WriteAllText(path, html.ToString());
    }

    private const string Css = @"
:root{--bg:#0a0e1a;--card:#141a2e;--card2:#1b2440;--line:#2a3454;--txt:#e8ecf6;--dim:#8a94b0;--green:#21d07a;--blue:#3da5ff;--gold:#ffd23f;--red:#ff5470}
*{box-sizing:border-box;margin:0;padding:0}
body{background:radial-gradient(1200px 600px at 50% -10%,#16203c 0%,var(--bg) 55%);color:var(--txt);font:15px/1.5 system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;padding:28px 16px}
.wrap{max-width:1180px;margin:0 auto}
.page-h{display:flex;align-items:baseline;gap:12px;margin-bottom:22px}
.logo{font-weight:800;font-size:20px;letter-spacing:.3px}
.logo-sub{color:var(--dim);font-size:13px}
.card{background:linear-gradient(180deg,var(--card),var(--card2));border:1px solid var(--line);border-radius:16px;padding:20px 22px;margin-bottom:18px;box-shadow:0 10px 30px rgba(0,0,0,.35)}
.card h2{font-size:13px;letter-spacing:1.5px;text-transform:uppercase;color:var(--dim);margin-bottom:14px;font-weight:700}
.hero{background:linear-gradient(135deg,#21305c,#0f1730);border:1px solid var(--line);border-radius:20px;padding:30px;margin-bottom:18px;text-align:center}
.hero-tag{color:var(--gold);font-weight:700;letter-spacing:2px;font-size:12px;text-transform:uppercase}
.vs{display:flex;align-items:center;justify-content:center;gap:26px;margin:14px 0}
.vs .side{flex:1;font-size:26px;font-weight:800}
.vs .side:first-child{text-align:right}.vs .side:last-child{text-align:left}
.vs .side.win{color:var(--green)}
.vs .score{font-size:46px;font-weight:900;background:#0c1224;border:1px solid var(--line);border-radius:14px;padding:6px 20px;letter-spacing:2px}
.hero-sub{color:var(--dim);font-size:14px}
.hero-date{margin-top:12px;color:var(--dim);font-size:12.5px;letter-spacing:.3px}
.wdl-row{display:flex;align-items:center;gap:12px;margin:9px 0}
.wdl-l{width:170px;text-align:right;font-size:14px}
.wdl-track{flex:1;height:22px;background:#0c1224;border-radius:11px;overflow:hidden}
.wdl-fill{height:100%;border-radius:11px}
.wdl-fill.home{background:linear-gradient(90deg,#1a8f5a,var(--green))}
.wdl-fill.away{background:linear-gradient(90deg,#1f6fb8,var(--blue))}
.wdl-fill.draw{background:linear-gradient(90deg,#5a6480,#8a94b0)}
.wdl-p{width:64px;font-weight:700}
.chips{display:flex;flex-wrap:wrap;gap:12px}
.chip{background:#0c1224;border:1px solid var(--line);border-radius:12px;padding:12px 16px;min-width:140px;flex:1}
.chip-v{font-size:22px;font-weight:800;color:var(--gold)}
.chip-l{color:var(--dim);font-size:12px;letter-spacing:.5px;text-transform:uppercase}
.chip-s{color:var(--dim);font-size:12px;margin-top:2px}
.th td{color:var(--dim);font-size:11px;letter-spacing:.5px;text-transform:uppercase;font-weight:700}
.note{color:var(--dim);font-size:13px;margin-top:10px}
.og{background:rgba(255,84,112,.2);color:var(--red);font-size:10px;padding:1px 5px;border-radius:4px;font-weight:700}
.badge{font-size:11px;font-weight:800;padding:3px 9px;border-radius:6px;letter-spacing:.5px}
.badge.yellow{background:rgba(255,210,63,.18);color:var(--gold)}
.badge.red{background:rgba(255,84,112,.18);color:var(--red)}
.pill{font-size:12px;font-weight:700;padding:2px 9px;border-radius:99px}
.pill.scored{background:rgba(33,208,122,.18);color:var(--green)}
.pill.saved{background:rgba(255,210,63,.18);color:var(--gold)}
.pill.missed{background:rgba(255,84,112,.18);color:var(--red)}
.tl{position:relative;height:74px;margin:6px 4px}
.tl-axis{position:absolute;top:37px;left:0;right:0;height:2px;background:var(--line)}
.tl-half{position:absolute;top:28px;height:20px;width:1px;background:var(--line)}
.tl-end{position:absolute;right:0;top:50px;color:var(--dim);font-size:11px}
.tl-m{position:absolute;transform:translateX(-50%);font-size:17px;text-align:center;cursor:default}
.tl-m.home{top:0}.tl-m.away{top:44px}
.tl-min{display:block;font-size:10px;color:var(--dim)}
.tl-legend{color:var(--dim);font-size:11px;margin-top:12px}
.cmp-head{display:flex;justify-content:space-between;color:var(--dim);font-weight:700;font-size:13px;margin-bottom:6px}
.cmp{display:flex;align-items:center;gap:14px;margin:7px 0}
.cmp .cv{width:54px;font-weight:700;font-variant-numeric:tabular-nums}
.cmp .cv:first-child{text-align:right}
.cmp-mid{flex:1}
.cmp-l{text-align:center;color:var(--dim);font-size:12px;margin-bottom:3px}
.cmp-track{display:flex;height:9px;border-radius:5px;overflow:hidden;background:#0c1224}
.cmp-h{background:var(--green)}.cmp-a{background:var(--blue)}
.grid2{display:grid;grid-template-columns:1fr 1fr;gap:18px}
table.lst{width:100%;border-collapse:collapse}
table.lst td{padding:7px 6px;border-bottom:1px solid var(--line);font-variant-numeric:tabular-nums}
.lst tr:last-child td{border-bottom:none}
.r{text-align:right}.dim{color:var(--dim)}
.bar-cell{width:45%}
.mini{height:8px;background:#0c1224;border-radius:4px;overflow:hidden}
.mini-f{height:100%;background:linear-gradient(90deg,#1a8f5a,var(--green))}
.verg{font-weight:800;padding:2px 8px;border-radius:6px}
.v-hi{background:rgba(255,84,112,.15);color:var(--red)}.v-mid{background:rgba(255,210,63,.15);color:var(--gold)}
.v-lo{background:rgba(33,208,122,.12);color:var(--green)}.v-min{color:var(--dim)}
.miracle{display:flex;align-items:center;gap:18px}
.miracle .mr{font-weight:900;line-height:1}
.mr-val{font-size:42px}.mr-of{font-size:18px;color:var(--dim)}
.mr-label{font-weight:800;font-size:18px;letter-spacing:1px}
.miracle.m-hi .mr-val{color:var(--red)}.miracle.m-mid .mr-val{color:var(--gold)}.miracle.m-lo .mr-val{color:var(--green)}.miracle.m-min .mr-val{color:var(--dim)}
.champ{display:flex;align-items:center;gap:22px;background:linear-gradient(135deg,#3a2e07,#1a1505);border:1px solid #7a5e10;border-radius:20px;padding:24px 28px;margin-bottom:18px}
.trophy{font-size:54px}
.champ-tag{color:var(--gold);font-weight:800;letter-spacing:3px;font-size:13px}
.champ-name{font-size:34px;font-weight:900}
.bracket-card{width:100vw;max-width:none;margin-left:calc(50% - 50vw);border-radius:0}
.bracket-scroll{overflow-x:auto;padding:6px 2px 10px}
.bracket{position:relative}
.round-head{position:absolute;top:0;text-align:center;color:var(--dim);font-size:12px;font-weight:700;letter-spacing:1px;text-transform:uppercase}
.lines{position:absolute;top:24px;left:0;pointer-events:none}
.lines path{fill:none;stroke:var(--line);stroke-width:2}
.match{position:absolute;background:#0c1224;border:1px solid var(--line);border-radius:10px;overflow:hidden;z-index:2}
.match.standalone{position:relative;width:240px}
.team{display:flex;justify-content:space-between;align-items:center;padding:7px 11px;font-size:14px}
.team+.team{border-top:1px solid var(--line)}
.team.win{background:rgba(33,208,122,.10)}
.team.win .nm{color:var(--green);font-weight:700}
.team .sc{font-weight:800;font-variant-numeric:tabular-nums}
.team .pens{color:var(--gold);font-size:12px}
.groups{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:14px}
.grp-h{font-weight:700;margin-bottom:6px;color:var(--gold)}
table.std{width:100%;border-collapse:collapse;font-size:13px}
table.std td{padding:5px 6px;border-bottom:1px solid var(--line);font-variant-numeric:tabular-nums}
table.std tr.adv td:nth-child(2){font-weight:700;color:var(--green)}
table.std.wide tr.sel td{background:rgba(255,210,63,.08)}
table.std.wide tr.sel td:nth-child(2){color:var(--gold);font-weight:800}
.wdl-fill.elim{background:linear-gradient(90deg,#b83048,var(--red))}
.you{color:var(--gold);font-weight:800}
.bad{color:var(--red);font-weight:700}
.path.good{border-color:#1d6b46}.path.bad{border-color:#7a2740}
.pill.good{background:rgba(33,208,122,.18);color:var(--green)}
.pill.bad{background:rgba(255,84,112,.18);color:var(--red)}
.gwin{color:var(--green)}
.wdlsplit{display:flex;height:8px;border-radius:4px;overflow:hidden;background:#0c1224;margin-bottom:3px;min-width:170px}
.wdlsplit .seg{height:100%}
.seg.sh{background:var(--green)}.seg.sd{background:#5a6480}.seg.sa{background:var(--blue)}
.wdlnum{display:flex;justify-content:space-between;font-size:11px;gap:10px;font-variant-numeric:tabular-nums}
.wdlnum .nh{color:var(--green)}.wdlnum .nd{color:var(--dim)}.wdlnum .na{color:var(--blue)}
.comm{display:flex;flex-direction:column;gap:8px}
.cl{display:grid;grid-template-columns:42px 92px 1fr;gap:10px;align-items:baseline;padding:6px 8px;border-radius:8px;background:#0c1224}
.cl .cmin{color:var(--dim);font-size:12px;font-variant-numeric:tabular-nums;text-align:right}
.cl .cwho{font-size:11px;font-weight:700;letter-spacing:.5px;text-transform:uppercase}
.cl.pbp .cwho{color:var(--blue)}.cl.co .cwho{color:var(--gold)}
.cl.co{background:#141a2e;border-left:2px solid var(--gold)}
.cl.crd .cwho{color:var(--green)}.cl.crd{background:#101b14;border-left:2px solid var(--green)}.cl.crd .ctxt{color:#bdeccb}
.cl .ctxt{font-size:14px;line-height:1.45}
.page-f{color:var(--dim);font-size:12px;text-align:center;margin-top:10px}
@media(max-width:740px){.grid2{grid-template-columns:1fr}.vs .side{font-size:19px}.vs .score{font-size:34px}}
@media print{:root{--bg:#fff;--card:#fff;--card2:#fff;--txt:#111;--dim:#555;--line:#bbb}body{background:#fff;padding:0}.card,.hero,.champ,.match{background:#fff !important;box-shadow:none}.hero{border:1px solid #bbb}.bracket-scroll{overflow:visible}.lines path{stroke:#999}}
";
}
