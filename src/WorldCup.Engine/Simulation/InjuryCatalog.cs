using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// A catalogue of specific, realistic football injuries grouped by <see cref="InjurySeverity"/>.
/// Each diagnosis carries an affected body part and a plausible recovery-day range, from which a
/// concrete lay-off is sampled. Sampling is deterministic given the supplied RNG state.
/// </summary>
public static class InjuryCatalog
{
    /// <summary>
    /// A single catalogue entry: a body part, a precise diagnosis, and the inclusive range of
    /// realistic recovery times (in days) for that diagnosis.
    /// </summary>
    /// <param name="BodyPart">The affected body part (e.g. "hamstring", "ankle").</param>
    /// <param name="Diagnosis">A precise, human-readable description of the injury.</param>
    /// <param name="MinDays">The lower bound (inclusive) of the plausible recovery range, in days.</param>
    /// <param name="MaxDays">The upper bound (inclusive) of the plausible recovery range, in days.</param>
    private readonly record struct InjuryEntry(string BodyPart, string Diagnosis, int MinDays, int MaxDays);

    /// <summary>
    /// Knocks: shaken off / day-to-day. Many resolve with no time lost (0 days) and the player
    /// continues; the worst are a handful of days.
    /// </summary>
    private static readonly InjuryEntry[] Knocks =
    {
        new("thigh", "dead leg (thigh contusion)", 0, 3),
        new("ankle", "bang on the ankle", 0, 2),
        new("head", "cut requiring stitches", 0, 2),
        new("ribs", "bruised ribs", 1, 4),
        new("knee", "knock to the knee", 0, 3),
        new("foot", "stubbed/bruised foot", 0, 3),
        new("calf", "cramp", 0, 1),
        new("ankle", "mild rolled ankle", 1, 4),
        new("hip", "bruised hip", 0, 3),
        new("shoulder", "jarred shoulder", 0, 2),
        new("torso", "winded", 0, 0),
        new("nose", "bloodied nose", 0, 0),
    };

    /// <summary>
    /// Minor injuries: strains and sprains keeping a player out a few weeks, with sensible
    /// per-diagnosis ranges (roughly 7-35 days).
    /// </summary>
    private static readonly InjuryEntry[] Minor =
    {
        new("hamstring", "hamstring strain (grade 1)", 14, 21),
        new("ankle", "sprained ankle", 14, 28),
        new("groin", "groin strain", 12, 24),
        new("calf", "calf strain", 14, 25),
        new("knee", "MCL sprain (grade 1)", 18, 30),
        new("thigh", "thigh/quad strain", 16, 28),
        new("hip", "hip flexor strain", 12, 22),
        new("thigh", "deep dead leg (haematoma)", 10, 18),
        new("foot", "bruised metatarsal", 12, 22),
        new("back", "lower-back spasm", 7, 14),
        new("ankle", "turned ankle ligaments", 16, 26),
        new("knee", "knee bruise with effusion", 12, 20),
        new("hamstring", "hamstring tightness/low-grade strain", 9, 16),
        new("shoulder", "shoulder ligament sprain", 14, 24),
    };

    /// <summary>
    /// Major injuries: fractures, ruptures and tears measured in months and often season-threatening
    /// (roughly 42-300 days).
    /// </summary>
    private static readonly InjuryEntry[] Major =
    {
        new("knee", "ruptured ACL", 210, 270),
        new("ankle", "ruptured Achilles tendon", 180, 300),
        new("leg", "broken leg (fractured fibula)", 90, 150),
        new("ankle", "fractured ankle", 80, 140),
        new("hamstring", "grade 3 hamstring tear", 60, 110),
        new("shoulder", "dislocated shoulder", 45, 84),
        new("foot", "fractured metatarsal", 42, 84),
        new("knee", "torn meniscus", 56, 98),
        new("knee", "fractured patella", 84, 150),
        new("thigh", "torn quadriceps", 70, 120),
        new("shoulder", "broken collarbone", 42, 70),
        new("head", "concussion (return-to-play protocol)", 12, 28),
        new("face", "fractured cheekbone", 28, 49),
        new("ankle", "high ankle sprain (syndesmosis)", 45, 80),
    };

    /// <summary>
    /// Picks a specific, realistic football injury appropriate to <paramref name="severity"/>, sampling
    /// a recovery time (in days) uniformly within that diagnosis's plausible inclusive range.
    /// Deterministic given the RNG state.
    /// </summary>
    /// <param name="severity">The severity band to draw a diagnosis from.</param>
    /// <param name="rng">The RNG state, advanced by reference.</param>
    /// <returns>The affected body part, the precise diagnosis, and the sampled recovery time in days.</returns>
    public static (string BodyPart, string Diagnosis, int RecoveryDays) Diagnose(InjurySeverity severity, ref Xoshiro256 rng)
    {
        InjuryEntry[] table = severity switch
        {
            InjurySeverity.Knock => Knocks,
            InjurySeverity.Minor => Minor,
            InjurySeverity.Major => Major,
            _ => Knocks,
        };

        InjuryEntry entry = table[rng.NextInt(table.Length)];

        // NextInt's upper bound is exclusive; add one to make MaxDays inclusive.
        int recoveryDays = rng.NextInt(entry.MinDays, entry.MaxDays + 1);
        return (entry.BodyPart, entry.Diagnosis, recoveryDays);
    }

    /// <summary>
    /// Renders a lay-off length as a human-readable phrase: 0 days is "plays on"; 1-6 days reads in
    /// days; 7-27 days reads in (rounded) weeks; 28 days and above reads in (rounded) months.
    /// All units are correctly pluralised.
    /// </summary>
    /// <param name="days">The recovery time in days (negative values are treated as zero).</param>
    /// <returns>A short phrase describing the expected absence.</returns>
    public static string RecoveryText(int days)
    {
        if (days <= 0)
        {
            return "plays on";
        }

        if (days <= 6)
        {
            return $"~{days} {Plural(days, "day")}";
        }

        if (days <= 27)
        {
            int weeks = (int)Math.Round(days / 7.0, MidpointRounding.AwayFromZero);
            if (weeks < 1)
            {
                weeks = 1;
            }

            return $"~{weeks} {Plural(weeks, "week")}";
        }

        int months = (int)Math.Round(days / 30.0, MidpointRounding.AwayFromZero);
        if (months < 1)
        {
            months = 1;
        }

        return $"~{months} {Plural(months, "month")}";
    }

    /// <summary>Returns <paramref name="unit"/> pluralised with a trailing "s" when <paramref name="count"/> is not 1.</summary>
    /// <param name="count">The quantity that governs pluralisation.</param>
    /// <param name="unit">The singular unit word.</param>
    /// <returns>The singular or plural form of <paramref name="unit"/>.</returns>
    private static string Plural(int count, string unit) => count == 1 ? unit : unit + "s";
}
