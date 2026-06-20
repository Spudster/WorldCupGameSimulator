# ⚽ 2026 World Cup Monte Carlo Simulator

A polished **C# / .NET 8** console application that pulls the real 2026 FIFA World Cup
teams, groups and bracket and runs **Monte Carlo simulations** of individual matches and the
full 48-team tournament — up to **millions of times** — with robust, colourful
[Spectre.Console](https://spectreconsole.net/) output.

It runs at two fidelity levels:

| Mode | What it does | Used for |
|------|--------------|----------|
| **Fast** | Pure Poisson scorelines, parallelised, allocation-free per match | Title odds / advancement % over **1,000,000** tournaments |
| **Detailed** | Minute-by-minute event sim attributed to individual players (goals with scorer · assist · minute · distance · type, cards, penalties, injuries, full box scores) | Single matches, box scores, leaderboards, MVP, "crazy stats" |

> Throughput on a typical laptop: **~7M single-match sims/sec** and **~440K full
> tournaments/sec** (1,000,000 tournaments in ~2–3 s), with a live progress bar + ETA.

---

## Quick start

Just open and run — no setup needed:

```text
Windows:   double-click run.cmd   (or: .\run.ps1)
Any OS:    dotnet run --project src/WorldCup.Cli -c Release
```

On launch it loads the bundled real data, pulls the latest results/fixtures from football-data.org
(when a key is configured — see below), and shows the menu. It runs **fully offline out of the box**
using the bundled data, so no API key or network connection is required.

Other handy commands:

```bash
dotnet build                                   # build everything (warnings-as-errors)
dotnet test                                    # run the simulation-math test suite
dotnet run --project src/WorldCup.Cli -- --smoke   # non-interactive end-to-end demo
```

Requires the .NET 8 SDK (or newer). On first build, NuGet restores Spectre.Console.

---

## Main menu

```
1. Team vs Team                       – pick any two teams; single detailed match OR
                                        fast Monte-Carlo summary OR best-of-N event averages
2. Run current scheduled match        – finds the next real fixture, confirms, simulates it
3. Run scheduled games                – forecasts remaining fixtures (1M sims each) for a chosen day
                                        (today / a date) or all of them, in one scannable table
4. Full tournament — official groups  – the real 2026 draw + bracket, single playthrough
                                        OR Monte Carlo over N tournaments
5. Full tournament — current state     – locks already-played results, simulates the rest
6. Group path to victory & defeat      – pick a group + team; maps every way they can win
                                        the group or get knocked out, with probabilities
   (also: a Qualification scenarios grid — every remaining-results combination → who qualifies)
7. Road to glory                       – a team's campaign: reach/win odds per knockout round,
                                        likely opponents each round, and title odds
8. Tournament odds board               – outright odds (title, final, group) as %/decimal/
                                        fractional/American, plus optional Golden Boot/Glove/MVP odds
9. Model accuracy                      – backtests the model's pre-match predictions against the
                                        real results played so far (Brier, log-loss, calibration)
10. Compare two scenarios              – with vs without a player, or Current vs Starting params,
                                        diffed side by side from the same seed
11. Bracket challenge                  – pick champion / runner-up / dark horse; the model grades
                                        your picks and plays out one reality to score them
12. Live matchday dashboard            – today's games (score or forecast) + live qualification
                                        odds for the groups in action, with a refresh button
13. Run until a team wins              – keep simulating until a chosen team wins (cup/match)
14. Load a saved tournament            – re-open a saved snapshot (re-print, re-export) instantly
15. Parameters                         – view/edit/save/load/reset, calibrate, auto-tune
16. Exit
```

The simulator also models **real tournament discipline**: a player on two yellows (or shown a red)
**sits out his team's next game**, and a Minor/Major **injury rules him out for the rest** of the run —
so squad depth matters over a campaign. Single-tournament downloads now produce a **linked HTML bundle**
(index → bracket → statistics), and match forecasts include a **scoreline heatmap**.

### Run scheduled games

Forecasts the group fixtures that haven't been played yet — **all remaining at once, or just a single
day** (it offers "today", "tomorrow" and every date that still has fixtures, with a game count each),
each over a big Monte Carlo (1,000,000 sims by default), and prints **one scannable table** — group, matchday, the matchup (favourite
highlighted), the most-likely scoreline, the home/draw/away odds (heat-coloured) and the expected
goals — plus quick highlights (biggest favourite, closest match, most likely draw). The displayed
**score is the most likely scoreline for the forecast result**, so it agrees with the win odds — a
favourite's win is spread over many winning scorelines, so the single most-common *exact* score is
often a draw or 1–0 even when one side is a clear favourite (true in real football too). Offers to pull
the latest results first so the "remaining" set is current, and exports the whole slate to a styled
**HTML** page, **CSV** or **JSON**.

### Watching example matches

After any single-matchup Monte Carlo you can **watch example games** drawn from the distribution: pick
the **most likely scoreline**, the **most likely result for the favourite**, **any exact scoreline**
(each listed with its probability), or a **random game**. Every shown match then reports **how likely
it was** — the share of simulations that ended in that exact scoreline and that result — so you always
know whether you're looking at the textbook case or a one-in-fifty upset.

### Group path to victory & defeat

Pick a group, then choose **the whole group** or **a single team**.

**Whole group** runs one shared Monte-Carlo and prints a single overview table — every team's current
standing plus its probability of winning the group / finishing runner-up / advancing (top 2) / 3rd /
going out, with a one-word status (*In command*, *In the mix*, *Qualified ✓*, *Eliminated*, …). Because
it's one shared simulation the numbers are mutually consistent (the win-group shares sum to 100%,
direct-qualification to 200%), and it cheerfully shows when a strong side is favourite to win the
group even though a weaker side currently leads on points.

**Single team** lays out where the group can still go for one side:

- **Finishing outlook** — the probability of winning the group, finishing runner-up, dropping to
  3rd (the best-third lottery) or going out last, from a Monte-Carlo of the remaining fixtures run
  through the real tiebreakers.
- **Remaining fixtures** — the win/draw/win odds of every game left in the group.
- **What your own game(s) need to yield** — conditional on the team's own result (win/draw/lose, or
  points taken), the chance it then wins the group / advances / finishes last.
- **Path to victory & path to defeat** — the concrete *combinations* of the remaining results that
  win the group, and those that finish the team last, each with its likelihood. Where a place comes
  down to goal difference or head-to-head, the finish is shown as a range (e.g. `1st–2nd*`).
- It also flags what is already **mathematically settled** (clinched top spot / qualification, or
  no longer able to win the group / advance), and exports the whole thing as a **styled HTML page**.

Every report prints its **run configuration** (scenario, parameter set, seed, N) at the top so
results are reproducible. Long runs ask for confirmation and show a live progress bar with ETA
and throughput. All reports can be exported to **CSV and/or JSON**.

---

## The 2026 tournament (modelled exactly)

- **48 teams**, **12 groups (A–L) of 4**, using the **official final draw** (5 Dec 2025) with the
  March 2026 playoff winners resolved (Bosnia & Herzegovina, Sweden, Türkiye, Czechia via UEFA;
  DR Congo and Iraq via the intercontinental playoffs).
- **Group stage:** single round robin (3 matches each). Win 3, draw 1, loss 0.
- **Advancement:** top 2 of each group **plus the 8 best third-placed teams** = **32 teams**.
- **Group tiebreakers, in order:** (1) points, (2) goal difference, (3) goals scored,
  (4) head-to-head among the tied teams, (5) fair-play (fewer cards), (6) drawing of lots.
- **Knockout:** R32 → R16 → QF → SF → Final, following FIFA's **official bracket** (the fixed
  pre-defined pairings, match numbers 73–104). A third-place playoff is included and toggleable.
  Level knockout matches go to extra time, then a strength-weighted penalty shootout.

> **Third-place assignment:** the side a qualifying third-placed team faces depends on which eight
> of the twelve groups produce qualifiers. FIFA's full 495-row Annex C table is approximated by a
> deterministic bipartite matching that honours FIFA's published per-slot eligibility sets (and, as
> a fallback, the fundamental "no same-group rematch" rule). The result is reproducible — never
> random — and never pairs a team against a side from its own group. The winner/runner-up R32
> pairings and the entire R16→Final tree are exactly as published.

---

## Simulation model

For each match, each team's expected goals `λ` is derived log-linearly from the strength gap,
home advantage (applied only for host nations / scheduled home sides) and a global goal baseline:

```
λ_home = GoalBaseline · exp( StrengthSensitivity · (Sₕ − Sₐ)/100 + HomeAdvantage )
λ_away = GoalBaseline · exp( StrengthSensitivity · (Sₐ − Sₕ)/100 )
```

Scorelines are drawn from a **bivariate Poisson** (a shared component, `DrawCoupling`, tunes the
draw rate). Detailed mode then runs a minute-by-minute event model on top: goals attributed to a
scorer (weighted by `finishing` + position) and usually an assister (`creativity`); shot distance
from a right-skewed **log-normal** (median ≈ 11 m, so a 30 m+ screamer is genuinely rare); cards
from a discipline-weighted foul model (two yellows = red; a red reduces the team's effective
strength); probabilistic penalties, corners and a full box score. **Injuries are diagnosed
specifically** — a body part, an exact diagnosis (e.g. *ruptured ACL*, *hamstring strain*, *bang on
the ankle*) and an expected lay-off (*plays on* → *~3 weeks* → *~8 months*) drawn from a 40-entry
catalogue, with substitutions (or a man-down spell when subs are exhausted).

**Play-by-play announcer.** Every downloaded single match comes with a **commentary transcript**.
A deterministic two-voice engine (a play-by-play commentator + a colour analyst) walks the match
timeline and calls every moment in order — the intro and kickoff, each goal (*"OHH WHAT A GOAL! a
thunderbolt from distance…"*), penalties, cards *with the offence*, errors and keeper howlers, bad
calls, injuries *with the diagnosis and lay-off*, subs, great saves, half-time and full-time
summaries, and extra-time / shootout drama — weaving in the rich event data and the running score. No
network and no model calls: the same match always tells the same story. It's embedded as a **📻 Live
commentary** card in the styled HTML page and **saved alongside it as a `<match>_commentary.txt`
transcript** automatically whenever a game is downloaded; option 1 (single match) also offers to read
it out in the console.

**Momentum, morale & tempo.** Matches are not memoryless. A shared per-match **tempo** makes some
games open shootouts and others cagey grinds (so scores spread out instead of every game converging
on 1–0 / 1–1), and a live **momentum** swing responds to events: scoring (an early goal hits hardest)
lifts one side and deflates the other, winning a penalty emboldens — missing one is a reprieve that
swings it back — a red card or a bad call demoralises the wronged side, a defensive howler stings
extra, and **fresh substitutes get a burst**. The swing mean-reverts, so it only ever *redistributes*
scoring within a match — runs and capitulations happen without changing the goals/match calibration
(`MatchTempoVariance`, `MomentumStrength`, `MomentumDecayPerMinute`). The box score also reports
**possession, corners, throw-ins and goal kicks**, and every **card records the offence** it was shown
for (a late challenge, dissent, violent conduct, a second bookable, …).

**Mistakes & officiating.** Detailed mode also models the things that actually swing matches:
**defensive errors** and **goalkeeper howlers** that gift goals (a weaker keeper errs more often),
**unpunished mistakes** that only led to a scare, and **refereeing bad calls** — soft penalties,
penalties wrongly denied, harsh/mistaken cards, missed reds, and goals wrongly allowed or chalked
off (some VAR-checked). These are realistic, tunable hazards (`DefensiveErrorGoalShare`,
`GoalkeeperErrorGoalShare`, `UnpunishedErrorsPerMatch`, `WrongPenaltyShare`, `WrongCardShare`,
`RefereeMistakesPerMatch`). Each one is described **specifically** — the error narrates what actually
happened (*spilled a routine shot*, *a misplaced back-pass*, *flapped at a cross*) and the bad call
its detail (*a soft coming-together the referee bought*, *a stonewall trip ignored*). Crucially they
are **attribution-only** — an error *re-labels* a goal that was already scored and a bad call *tags*
a penalty/card that was already shown, rather than adding new ones — so the calibrated
goal/card/penalty/corner totals are untouched (the calibration test still lands every metric in
band). **Every single-game readout carries the same categories** — they appear in the box score
(per-team error and bad-call counts), dedicated error and refereeing-controversy tables (with the
specific narrative), the match timeline, the styled HTML page and the JSON/CSV exports, and as
per-game averages in the aggregate forecast.

A fast `xoshiro256**` RNG is used, **one instance per worker thread** (never shared), with
`Parallel.For` partitioning and stack-allocated, allocation-free group/bracket scratch buffers.

---

## Calibration

Default event rates are seeded from recent World Cup averages so output is believable out of the
box, then verified by a calibration step (Parameters → *Run calibration diagnostics* /
*Auto-tune*). Measured defaults (8,000-match batch) all land in band:

| Metric (per match) | Target | Source | Measured (default) |
|--------------------|-------:|--------|-------------------:|
| Goals              | 2.70   | 2.69 (2022), 2.64 (2018), 2.67 (2014) | ≈ 2.8 |
| Yellow cards       | 3.30   | 3.34 (2022), 3.42 (2018)              | ≈ 3.2 |
| Red cards          | 0.10   | 0.06 (2022, 4 in 64 games); historically higher | ≈ 0.10 |
| Penalties awarded  | 0.40   | 0.36 (2022), 0.45 (2018, VAR debut)   | ≈ 0.42 |
| Corners            | 10.0   | historical norm (recent WCs trend ~8.7) | ≈ 10.0 |

Because events interact (a red card suppresses the rest of a match; mismatches skew goal counts),
the realised numbers drift slightly from the inputs — that is why the verify step exists. Strength
creates the *spread* around these averages; the global rates set the overall *level*. **Auto-tune**
nudges the goal/card/penalty/corner knobs until each metric lands in its band.

---

## Parameters (current vs starting)

A `SimulationParameters` object holds every tunable knob: per-team strength and per-player attribute
overrides, home advantage, goal scaling, draw tendency, event rates, MVP weights and the RNG seed.

- **Starting** parameters are the pristine defaults (no overrides).
- **Current** parameters are an editable working copy that persists for the session and can be
  **saved to / loaded from** a JSON file.

When launching a run you choose which set to use. The **MVP / Golden Ball** score is transparent and
fully weighted by parameters:

```
raw = Goal·goals + Assist·assists + CleanSheet·cleanSheets + DefensiveAction·saves + Minutes·(mins/90)
mvp = raw · AdvancementMultiplier(team's furthest stage)
```

---

## Data layer

A single `ITeamDataProvider` interface backs two implementations:

- **Seed provider (default):** reads four bundled data files —
  - `data/seed_2026.json` — the official draw + strengths derived from the 11 Jun 2026 FIFA ranking,
  - `data/squads_2026.json` — **real ~18-man squads** for all 48 teams (real names + positions; a
    best-effort overall rating per player drives the five event attributes). **These curated rosters
    are the source of truth** — current and individually rated, so they take precedence over the live
    squad endpoint. If this file is absent for a team, that team falls back to a deterministic synthetic squad,
  - `data/schedule_2026.json` — the **official group-stage fixtures** (real pairings + kickoff dates).
    If absent/invalid, a generated round-robin is used,
  - `data/results_2026.json` — real results already played, used by current-state mode.
- **Built-in live refresh on startup:** every launch, the app pulls the latest group results and
  fixtures from football-data.org (`LiveResultsService`, via `IHttpClientFactory`), maps them to our
  team codes, updates the in-memory results/schedule, and caches the response. It reads the API key
  from the `FOOTBALL_DATA_API_KEY` environment variable (never hard-coded), uses a short timeout, and
  is fully defensive: with no key, offline, or on any error it falls back to the bundled real data and
  says so. "Current state" mode's refresh re-runs the same live pull. (football-data.org free-tier
  coverage of the 2026 World Cup is not guaranteed; the bundled data always works.)
- **Force-refresh before a sim:** the startup pull caches squads for 12 h and results for 10 min, so
  the **Run current scheduled match** flow offers to *re-pull the latest rosters, results and fixtures
  now, bypassing those caches* — handy when a roster or result has just changed. `Session.RefreshLatest()`
  forces the fetch; without a key it just re-reads the bundled data.
- **Rosters & the starting XI:** the projected line-up (and the simulated XI) is built from the
  **bundled researched squads** — real names, positions and a per-player rating — so the right
  first-choice players start. The live football-data.org squad endpoint is used **only to fill a team
  that has no bundled squad**: it carries names + positions but no ratings, and its national-team lists
  can be **stale/provisional** (it was, for example, still listing players who aren't in the actual
  matchday squad), so it never overwrites the curated rosters. To pin a specific keeper anyway, use
  **Parameters → "Set a team's starting goalkeeper"** — it stores a *preferred starter* the line-up
  projector honours in the **simulation**, not just the on-screen XI. (For outfield changes, use
  formation and player-availability overrides in Parameters.)

To enable the live startup pull, provide a football-data.org API key one of two ways (the
environment variable wins if both are set):

```bash
# 1) Environment variable
# PowerShell
$env:FOOTBALL_DATA_API_KEY = "your-key-here"; dotnet run --project src/WorldCup.Cli
# bash
FOOTBALL_DATA_API_KEY=your-key-here dotnet run --project src/WorldCup.Cli
```

```jsonc
// 2) A git-ignored config.local.json at the repo root (read on startup):
{ "footballDataApiKey": "your-key-here" }
```

`config.local.json` is in `.gitignore` and is not copied into the build output, so the key stays
local — keep it that way and rotate the key if it ever leaks.

To refresh real results for current-state mode, edit `data/results_2026.json` (matched to fixtures
by team pair, so home/away orientation there is irrelevant) or use the in-app refresh.

---

## Project structure

```
WorldCupGameSimulator.sln
├── src/
│   ├── WorldCup.Data        – domain models, seed/live providers, official bracket, squad generator
│   ├── WorldCup.Engine      – RNG, Poisson, parameters, match & tournament sims, Monte Carlo,
│   │                          stats/awards/records, calibration
│   ├── WorldCup.Reporting   – Spectre.Console formatters + CSV/JSON exporters
│   └── WorldCup.Cli         – menu, prompts, progress, parameters UI (no business logic)
├── tests/WorldCup.Tests     – xUnit: standings + tiebreakers, best-third selection, bracket mapping,
│                              Poisson sanity, current-state locking, stat accumulation, records,
│                              calibration
└── data/                    – seed_2026.json, squads_2026.json, schedule_2026.json, results_2026.json
                               (cache/ holds live-API responses — git-ignored)
```

Dependency direction: `Data ← Engine ← Reporting ← Cli`. Nullable reference types and
warnings-as-errors are on across the solution.

---

## Vergazo scale (goal spectacularity)

Every detailed-mode goal is rated **1–10 on the "vergazo" scale** — how great the goal is. The
factors are modelled on FIFA's **Puskás Award** criteria for the best goal of the year (technical
difficulty/beauty, long-range distance, acrobatic actions, solo runs, collective team moves,
audacity, match importance, and *not* luck). It blends, continuously:

- **technique/style** — bicycle kick > long-range > free kick > open play > header;
- **distance** — scales up to ~30 m (Ibrahimović's 30 m+ overhead volley is the archetype);
- **solo run** — defenders beaten in the build-up;
- **collective build-up** — a slick assist from a creative playmaker;
- **clutch timing** — late winners and extra-time goals (greater importance);
- **keeper beaten**, **finishing quality**, and a **flair/execution** roll.

Each style has a ceiling, so **only a top-class long-range bicycle kick can reach a perfect 10/10**;
the best non-bicycle goals top out around 9.5. Own goals are always **≤ 3** and penalties stay low
(they're "lucky"/undramatic by the Puskás criteria). The rating shows in every game's goals table
(with the defenders-beaten count) and as a per-game **"⚡ Goal of the match"** highlight; across a
tournament it surfaces as the **"Goal of the tournament (vergazo)"** record, and across a matchup
Monte Carlo as the **"Best goal seen"** — e.g. *10.0/10 bicycle kick, 33.1 m, 77'*.

## "Crazy stats" records

Records are stored as a collection of named trackers (`IRecordTracker`) so new ones are cheap to
add. The defaults: goal of the tournament (highest vergazo), longest goal (screamer of the
tournament), fastest / latest goal, biggest win, highest-scoring match, most goals by a player in a
match, fastest card, fastest red card, longest penalty shootout, longest unbeaten run, longest
winning streak, most comeback wins, and dirtiest / cleanest team. Across a detailed Monte Carlo run, awards are also reported as **how often
each player wins** the Golden Boot / MVP / Golden Glove, with average tallies.

---

## Notes & honest limitations

- Player squads are **real names** with **estimated** attribute ratings (the per-player overall is a
  best-effort estimate; identities and positions are real). Update `data/squads_2026.json` as final
  squads are announced.
- The third-place R32 assignment is a deterministic, eligibility-honouring approximation of FIFA's
  full Annex C table (see above).
- The live startup refresh targets football-data.org; whether its free tier exposes 2026 World Cup
  data is outside our control, so the app is offline-first and always works from the bundled files.
- Locked (real) results in current-state mode carry no event detail, so they don't contribute to
  detailed player leaderboards (only simulated matches do).

---

## Data sources & attribution

- Optional **live results and fixtures** come from **[football-data.org](https://www.football-data.org/)**
  (free tier); rosters always use the curated bundled squads. The bundled `data/*.json` files (official
  2026 draw, schedule, squads, results) mean the app runs **fully offline** with no key.
- Built with **[Spectre.Console](https://spectreconsole.net/)**.

## License

Released under the **MIT License** — see [`LICENSE`](LICENSE).
Copyright © 2026 Carlos R. Gomez Viramontes.
