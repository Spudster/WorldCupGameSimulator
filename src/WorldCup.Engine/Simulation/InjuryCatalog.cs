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
        new("head", "clash of heads (superficial laceration)", 0, 2),
        new("shin", "shin knock (contusion)", 0, 2),
        new("groin", "groin cramp", 0, 1),
        new("hamstring", "hamstring cramp", 0, 1),
        new("calf", "calf cramp", 0, 1),
        new("quad", "quad cramp", 0, 1),
        new("elbow", "grazed elbow", 0, 1),
        new("knee", "grazed knee", 0, 1),
        new("eye", "knock to the eye (minor swelling)", 0, 3),
        new("toe", "knocked toe", 0, 2),
        new("thigh", "dead leg (outer thigh)", 0, 3),
        new("hand", "bruised hand", 0, 2),
        new("back", "knock to the lower back", 0, 3),
        new("calf", "kick to the calf", 0, 2),
        new("shin", "shin contusion", 0, 3),
        new("head", "head knock (no concussion protocol)", 0, 1),
        new("arm", "bruised forearm", 0, 2),
        new("chest", "knock to the chest", 0, 2),
        new("shoulder", "bang on the shoulder", 0, 3),
        new("hip", "hip knock (contusion)", 0, 2),
        new("foot", "trodden-on foot", 0, 2),
        new("ankle", "trodden-on ankle", 0, 2),
        new("face", "facial cut (treated pitchside)", 0, 1),
        new("torso", "abdominal knock", 0, 2),
        new("neck", "stiff neck (minor strain)", 0, 3),
        new("knee", "knee bang with minor bruising", 0, 3),
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
        new("hamstring", "hamstring strain (grade 2)", 28, 42),
        new("groin", "adductor strain (grade 2)", 21, 35),
        new("calf", "calf strain (grade 2)", 21, 35),
        new("quad", "quadriceps strain (grade 1)", 14, 24),
        new("quad", "quadriceps strain (grade 2)", 21, 35),
        new("hip", "hip flexor tear (partial)", 18, 30),
        new("ankle", "lateral ankle ligament sprain", 14, 28),
        new("ankle", "medial ankle ligament sprain", 16, 30),
        new("knee", "patella tendon soreness", 14, 28),
        new("knee", "knee ligament sprain (grade 1)", 14, 21),
        new("wrist", "sprained wrist", 10, 21),
        new("back", "lumbar muscle strain", 10, 21),
        new("back", "back muscle spasm", 7, 18),
        new("foot", "plantar fascia irritation", 14, 28),
        new("foot", "bruised heel (heel contusion)", 10, 21),
        new("shin", "shin splints (stress reaction)", 14, 28),
        new("shoulder", "AC joint sprain", 14, 28),
        new("shoulder", "rotator cuff strain", 14, 28),
        new("neck", "neck muscle strain", 7, 18),
        new("groin", "inguinal strain", 14, 24),
        new("hip", "hip adductor strain", 12, 22),
        new("hamstring", "proximal hamstring tendinopathy (acute flare)", 14, 28),
        new("calf", "Achilles tendon irritation", 14, 28),
        new("knee", "medial knee bruising with swelling", 14, 24),
        new("head", "mild concussion (return-to-play protocol, stage 1-3)", 7, 21),
        new("rib", "intercostal muscle strain", 10, 21),
        new("thumb", "sprained thumb", 10, 18),
        new("knee", "iliotibial band syndrome (acute)", 14, 28),
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
        new("knee", "ruptured MCL (grade 3)", 84, 150),
        new("knee", "PCL rupture", 140, 210),
        new("knee", "combined ligament injury (ACL + MCL)", 240, 300),
        new("leg", "broken leg (fractured tibia)", 120, 180),
        new("leg", "stress fracture (tibia)", 60, 100),
        new("groin", "grade 3 adductor tear", 56, 100),
        new("hip", "hip flexor tear (complete)", 56, 98),
        new("hip", "hip labrum tear", 84, 150),
        new("hamstring", "proximal hamstring avulsion", 90, 150),
        new("calf", "grade 3 calf tear", 56, 98),
        new("foot", "Lisfranc joint injury", 84, 150),
        new("foot", "navicular stress fracture", 70, 120),
        new("foot", "Jones fracture (fifth metatarsal)", 56, 100),
        new("ankle", "bi-malleolar ankle fracture", 90, 150),
        new("shoulder", "rotator cuff tear (surgical)", 120, 180),
        new("shoulder", "shoulder dislocation with labrum damage", 84, 140),
        new("back", "herniated lumbar disc", 56, 120),
        new("back", "stress fracture (vertebral)", 84, 150),
        new("wrist", "fractured wrist (distal radius)", 42, 70),
        new("hand", "fractured hand (metacarpal)", 35, 56),
        new("knee", "patellar tendon rupture", 150, 240),
        new("quad", "complete quadriceps tendon rupture", 150, 240),
        new("head", "severe concussion (full return-to-play protocol)", 21, 42),
        new("face", "fractured nose (requiring surgery)", 28, 42),
        new("eye", "orbital fracture", 42, 70),
        new("rib", "fractured rib", 42, 70),
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
