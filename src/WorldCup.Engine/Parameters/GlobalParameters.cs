namespace WorldCup.Engine.Parameters;

/// <summary>
/// Weights for the transparent MVP / Golden Ball composite score. The score for a player is:
/// <code>
/// raw = Goal*goals + Assist*assists + CleanSheet*cleanSheets + DefensiveAction*defActions
///       + Minutes*(minutes/90)
/// mvp = raw * AdvancementMultiplier(team's furthest stage)
/// </code>
/// All weights are tunable so the award can be re-calibrated.
/// </summary>
public sealed class MvpWeights
{
    public double Goal { get; set; } = 4.0;
    public double Assist { get; set; } = 2.5;
    public double CleanSheet { get; set; } = 1.5;
    public double DefensiveAction { get; set; } = 0.15;
    public double Minutes { get; set; } = 0.20;

    /// <summary>Multiplier applied by how far the player's team advanced (group → champion).</summary>
    public double GroupStageMultiplier { get; set; } = 1.0;
    public double RoundOf32Multiplier { get; set; } = 1.15;
    public double RoundOf16Multiplier { get; set; } = 1.3;
    public double QuarterFinalMultiplier { get; set; } = 1.5;
    public double SemiFinalMultiplier { get; set; } = 1.75;
    public double FinalistMultiplier { get; set; } = 2.0;
    public double ChampionMultiplier { get; set; } = 2.3;

    public MvpWeights Clone() => (MvpWeights)MemberwiseClone();
}

/// <summary>
/// Base per-match event rates for the detailed simulation. These set the overall <em>level</em>
/// of events; team strength creates the <em>spread</em> around them. Defaults are seeded from
/// recent World Cup averages (see README) and are refined by the calibration step.
/// </summary>
public sealed class EventRates
{
    /// <summary>Average yellow cards per match (recent WC ≈ 3.3).</summary>
    public double YellowCardsPerMatch { get; set; } = 3.3;

    /// <summary>Average <em>direct</em> red cards per match (excludes second-yellow reds). Real WCs are
    /// minority-direct: most dismissals are second yellows, so this is the smaller share of the ~0.1 total.</summary>
    public double DirectRedCardsPerMatch { get; set; } = 0.04;

    /// <summary>Average penalties awarded per match (VAR era ≈ 0.4).</summary>
    public double PenaltiesPerMatch { get; set; } = 0.40;

    /// <summary>Average corners per match, both teams combined (≈ 10).</summary>
    public double CornersPerMatch { get; set; } = 10.0;

    /// <summary>Average injuries per match, both teams combined (so ~0.4/match force a substitution).</summary>
    public double InjuriesPerMatch { get; set; } = 1.6;

    /// <summary>Average fouls per match, both teams combined (real WC ≈ 22–26; 2022 ≈ 24).</summary>
    public double FoulsPerMatch { get; set; } = 24.0;

    /// <summary>Average offsides per match, both teams combined.</summary>
    public double OffsidesPerMatch { get; set; } = 4.0;

    /// <summary>Average throw-ins per match, both teams combined (real ≈ 40).</summary>
    public double ThrowInsPerMatch { get; set; } = 40.0;

    /// <summary>Average goal kicks per match, both teams combined (real ≈ 14).</summary>
    public double GoalKicksPerMatch { get; set; } = 14.0;

    /// <summary>Probability an awarded penalty is converted (before keeper/strength effects).</summary>
    public double PenaltyConversionBase { get; set; } = 0.78;

    /// <summary>Average shots per goal (legacy; the box score now uses <see cref="ShotsBaselinePerTeam"/>).</summary>
    public double ShotsPerGoal { get; set; } = 9.0;

    /// <summary>Expected shots per team per match for an even contest (real elite ≈ 12); scaled by attack share.</summary>
    public double ShotsBaselinePerTeam { get; set; } = 12.0;

    /// <summary>Fraction of shots that are on target (real ≈ 0.33).</summary>
    public double ShotsOnTargetFraction { get; set; } = 0.33;

    // --- Mistakes & refereeing errors (detailed mode; all attribution-only, so they never change the
    //     calibrated goal/card/penalty totals — they re-label existing events or add score-neutral ones) ---

    /// <summary>Share of open-play goals that stem from a clear defensive error ("error leading to goal"). ≈ 0.09.</summary>
    public double DefensiveErrorGoalShare { get; set; } = 0.09;

    /// <summary>Share of open-play goals gifted by a goalkeeper error, for an average keeper; weaker keepers err more. ≈ 0.035.</summary>
    public double GoalkeeperErrorGoalShare { get; set; } = 0.035;

    /// <summary>Unpunished mistakes per team per match — an error that conceded a chance but not a goal. ≈ 0.55.</summary>
    public double UnpunishedErrorsPerMatch { get; set; } = 0.55;

    /// <summary>Share of awarded penalties that were the wrong call (a soft / incorrect decision). VAR-era ≈ 0.08.</summary>
    public double WrongPenaltyShare { get; set; } = 0.08;

    /// <summary>Share of cards shown that were harsh / incorrect (clear errors, VAR era). ≈ 0.05.</summary>
    public double WrongCardShare { get; set; } = 0.05;

    /// <summary>Clear refereeing mistakes per match with nothing else attached: a denied penalty, a missed
    /// red, or a goal wrongly chalked off. (Wrongly-allowed goals are tagged on the goals themselves.)
    /// VAR-era: clear, unreviewed injustices are ~once every 4–5 games ≈ 0.25.</summary>
    public double RefereeMistakesPerMatch { get; set; } = 0.25;

    public EventRates Clone() => (EventRates)MemberwiseClone();
}

/// <summary>
/// All global (not per-team/per-player) tunable knobs. Mutable so the CLI can edit a working
/// copy; cloned when forking "current" from "starting".
/// </summary>
public sealed class GlobalParameters
{
    // --- Match (goals) model ---

    /// <summary>Expected goals for an evenly matched team on a neutral ground (≈ 1.32 → ~2.7 total
    /// once the strength-spread convexity is averaged over fixtures).</summary>
    public double GoalBaseline { get; set; } = 1.32;

    /// <summary>How strongly a strength gap tilts expected goals (higher = bigger blowouts + sharper favourites).</summary>
    public double StrengthSensitivity { get; set; } = 1.82;

    /// <summary>
    /// How much the available starting XI's quality nudges effective team strength (0 = FIFA strength
    /// only). This is what makes injuries / suspensions / player edits actually move the win odds:
    /// the adjustment is the squad-rating delta vs. a team's full-strength XI, so a full team is
    /// unchanged (and calibration is preserved).
    /// </summary>
    public double SquadQualityWeight { get; set; } = 1.5;

    /// <summary>Home-advantage term added to the home team's log expected-goals.</summary>
    public double HomeAdvantage { get; set; } = 0.10;

    /// <summary>Bivariate-Poisson coupling that raises the draw rate (0 = independent goals).</summary>
    public double DrawCoupling { get; set; } = 0.10;

    /// <summary>
    /// Per-match "form / any-given-day" variance (log-normal sigma) applied to each team's expected
    /// goals. Higher = more upsets, shocks and surprise scorelines; 0 = a pure strength model. The
    /// multiplier is mean-centred (E[factor] = 1) so each team's <em>mean</em> goals — and the goals/match
    /// calibration — are unchanged; it widens the spread (draw rate, over/under, blowouts), which is
    /// where giant-killings come from.
    /// </summary>
    public double UpsetVariance { get; set; } = 0.33;

    /// <summary>
    /// Per-match shared "tempo" variance (log-normal sigma) applied to BOTH teams' expected goals at
    /// once — some games are open, end-to-end shootouts and others are cagey, low-block grinds. Mean-1,
    /// so it leaves goals/match unchanged but widens the <em>total</em>-goals spread (more 0–0s and more
    /// 4–3s) instead of every match converging on 1–0 / 1–1.
    /// </summary>
    public double MatchTempoVariance { get; set; } = 0.15;

    /// <summary>
    /// How strongly in-match momentum swings the live scoring rate. A goal (especially an early one),
    /// a won penalty, a red card, a refereeing call or a defensive howler lifts one side and deflates
    /// the other; fresh substitutes get a burst. The swing mean-reverts (see <see cref="MomentumDecayPerMinute"/>),
    /// so it only ever <em>redistributes</em> scoring within a match — it makes runs and capitulations
    /// happen without changing the long-run goals/match average. 0 = momentum off.
    /// </summary>
    public double MomentumStrength { get; set; } = 0.10;

    /// <summary>Per-minute decay of the momentum swing back toward neutral (0.90 ≈ a ~7-minute half-life).</summary>
    public double MomentumDecayPerMinute { get; set; } = 0.90;

    // --- Recent form (current-state forward predictions) ---
    // Carry a team's already-played results into its upcoming-game forecasts. Each played game is
    // scored by how its actual goal difference compared with what the strength model expected, and the
    // team's effective strength is nudged up (over-performed) or down (under-performed) for its next
    // games. This only applies when played results are supplied (via TeamFormDeltas), so pre-tournament
    // odds and the model calibration — which use no played results — are completely unaffected.

    /// <summary>
    /// How strongly recent form moves a team's effective strength in forward predictions. 0 = ignore
    /// form (pure pre-tournament ratings); 1 = full weight. Multiplies the per-team delta computed from
    /// played results, so e.g. Cape Verde holding Spain to a draw lifts them for Cape Verde v Uruguay.
    /// </summary>
    public double FormWeight { get; set; } = 1.0;

    /// <summary>Strength points (0–100 scale) awarded per goal of over-/under-performance versus the
    /// model's expected goal difference in a played game (before the cap and recency weighting).</summary>
    public double FormGoalDiffToStrength { get; set; } = 3.0;

    /// <summary>Maximum absolute strength adjustment (points) recent form can apply to a team, so a
    /// single freak result can't turn a minnow into a giant or a giant into a minnow.</summary>
    public double FormMaxDelta { get; set; } = 6.0;

    /// <summary>Recency weighting when a team has played several games: the most recent game has weight 1,
    /// the one before it this factor, the one before that this factor squared, and so on (0–1; lower =
    /// only the latest result really matters).</summary>
    public double FormRecencyDecay { get; set; } = 0.6;

    // --- Miracles (giant-killings) ---
    // A rare, realistic event where an underdog "catches fire" for the day: it claws back most of the
    // strength gap for this one match, which is what actually produces a genuine upset. The pre-match
    // odds (and the upset rating) still use the UN-boosted base strengths, so the rating measures the
    // true shock. Modelled in BOTH fast and detailed sims, so upsets ripple through every projection.

    /// <summary>Minimum strength gap (0–100 scale) for a miracle to be possible — there must be a clear underdog.</summary>
    public double MiracleMinGap { get; set; } = 8.0;

    /// <summary>Base per-match miracle chance once there is a clear underdog.</summary>
    public double MiracleBaseChance { get; set; } = 0.03;

    /// <summary>Extra miracle chance per strength-gap point above the minimum (capped by <see cref="MiracleMaxChance"/>).</summary>
    public double MiracleGapChance { get; set; } = 0.004;

    /// <summary>Maximum per-match miracle chance.</summary>
    public double MiracleMaxChance { get; set; } = 0.12;

    /// <summary>Fraction of the strength gap the underdog claws back when a miracle fires (0 = none, 1 = fully level).</summary>
    public double MiracleStrengthSwing { get; set; } = 0.70;

    // --- Knockout ---

    /// <summary>Fraction of regulation scoring rate applied across 30 minutes of extra time. Lower keeps
    /// more extra-time periods level, so more knockouts go to penalties (~17–20% of ties, as in real WCs).</summary>
    public double ExtraTimeGoalScale { get; set; } = 0.45;

    /// <summary>How much team strength tilts a penalty shootout (0 = coin flip).</summary>
    public double ShootoutStrengthWeight { get; set; } = 0.20;

    /// <summary>Base per-kick conversion probability in a shootout (real WC long-run ≈ 0.72).</summary>
    public double ShootoutKickConversion { get; set; } = 0.72;

    // --- Detailed-mode events & awards ---

    public EventRates Events { get; set; } = new();

    public MvpWeights Mvp { get; set; } = new();

    // --- Reproducibility ---

    /// <summary>Default team formation (e.g. "4-3-3", "4-4-2", "3-5-2"). Per-team overrides live on
    /// <see cref="SimulationParameters"/>.</summary>
    public string DefaultFormation { get; set; } = "4-3-3";

    /// <summary>Master RNG seed. The same seed + same parameters reproduce identical runs.</summary>
    public ulong Seed { get; set; } = 20260619;

    public GlobalParameters Clone()
    {
        var clone = (GlobalParameters)MemberwiseClone();
        clone.Events = Events.Clone();
        clone.Mvp = Mvp.Clone();
        return clone;
    }
}
