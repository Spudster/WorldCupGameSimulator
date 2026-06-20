using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Produces referee-style offence narratives describing why a card was shown.
/// All phrases are action/offence descriptions only and contain no player names.
/// </summary>
public static class CardNarratives
{
    /// <summary>
    /// Catalogue of reasons a referee shows a YELLOW (cautionable) card.
    /// </summary>
    private static readonly string[] YellowReasons =
    {
        "a late challenge",
        "a reckless tackle",
        "a tactical foul to stop a break",
        "a cynical trip on the counter",
        "dissent towards the referee",
        "a deliberate handball",
        "persistent fouling",
        "time-wasting",
        "a dive in the box (simulation)",
        "kicking the ball away",
        "encroachment at a free-kick",
        "a shirt-pull to halt an attack",
        "a dangerously high boot",
        "an unsporting challenge from behind",
        "a wild swing that caught an opponent",
        "delaying the restart"
    };

    /// <summary>
    /// Catalogue of reasons a referee shows a STRAIGHT (direct) RED card.
    /// </summary>
    private static readonly string[] DirectRedReasons =
    {
        "violent conduct",
        "serious foul play — a studs-up lunge",
        "a two-footed challenge",
        "denying an obvious goalscoring opportunity (DOGSO)",
        "a deliberate handball on the goal line",
        "a stamp off the ball",
        "an elbow to an opponent's face",
        "spitting at an opponent",
        "a reckless tackle endangering an opponent",
        "a wild high-foot challenge",
        "abusive language towards an official",
        "a dangerous lunge with excessive force"
    };

    /// <summary>
    /// Catalogue of reasons a referee shows a SECOND-YELLOW red (a second bookable offence).
    /// </summary>
    private static readonly string[] SecondYellowReasons =
    {
        "a second bookable foul — a needless trip",
        "a second caution for another tactical foul",
        "a second yellow for persistent fouling",
        "a second booking for dissent",
        "a second yellow — a clumsy late challenge",
        "a second caution for handball",
        "a second yellow for kicking the ball away",
        "a soft second booking for a mistimed tackle",
        "a second caution — pulling back a runner",
        "a second yellow for diving",
        "a second booking for a reckless challenge",
        "a second caution for time-wasting"
    };

    /// <summary>
    /// The specific reason a YELLOW card was shown — a referee-style offence phrase, no player names.
    /// e.g. "a late challenge", "dissent", "a tactical foul to stop a break".
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A yellow-card offence phrase.</returns>
    public static string Yellow(ref Xoshiro256 rng)
    {
        return YellowReasons[rng.NextInt(YellowReasons.Length)];
    }

    /// <summary>
    /// The specific reason a STRAIGHT RED was shown — a serious offence phrase, no player names.
    /// e.g. "violent conduct", "serious foul play — a studs-up lunge".
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A straight-red offence phrase.</returns>
    public static string DirectRed(ref Xoshiro256 rng)
    {
        return DirectRedReasons[rng.NextInt(DirectRedReasons.Length)];
    }

    /// <summary>
    /// The reason for a SECOND-YELLOW red (a second bookable offence), no player names.
    /// e.g. "a second bookable foul — a needless trip".
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A second-yellow offence phrase.</returns>
    public static string SecondYellow(ref Xoshiro256 rng)
    {
        return SecondYellowReasons[rng.NextInt(SecondYellowReasons.Length)];
    }
}
