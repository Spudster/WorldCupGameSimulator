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
        "delaying the restart",
        "a cynical shirt-tug on the halfway line",
        "a professional foul to deny a clear run",
        "a studs-showing challenge",
        "a needless late lunge",
        "disputing the referee\'s decision at length",
        "surrounding the referee with team-mates",
        "feigning injury (simulation)",
        "going down too easily in the area",
        "a blatant dive to win a free-kick",
        "a deliberate handball to stop a cross",
        "handling the ball to break up play",
        "cynical time-wasting near the corner flag",
        "prolonged celebrations designed to waste time",
        "holding the ball after the whistle",
        "a persistent infringement — a sixth foul of the match",
        "repeatedly fouling the same opponent",
        "niggling fouls that have mounted up",
        "a cynical trip to break up a counter-attack",
        "an arm across the throat of an opponent",
        "a challenge that endangered an opponent",
        "a crude tackle from the side",
        "a scything challenge that made no attempt to play the ball",
        "a high foot that caught an opponent on the knee",
        "a clip on the heel from behind",
        "a trip at the edge of the area to prevent a shot",
        "encroachment at a penalty kick",
        "an unsporting gesture towards the crowd",
        "removing the shirt during celebrations",
        "delaying a goal kick to run down the clock",
        "deliberate obstruction off the ball",
        "impeding the goalkeeper from releasing the ball",
        "a cynical block on a throw-in",
        "a deliberate foul to give teammates time to recover",
        "an over-the-top challenge in midfield"
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
        "a dangerous lunge with excessive force",
        "a headbutt off the ball",
        "a violent shove from behind near the dugout",
        "a karate-kick challenge with excessive force",
        "an off-the-ball assault away from play",
        "serious foul play — a cynical studs-first lunge",
        "a two-footed scissor-leg challenge",
        "denying a goal with a deliberate handball — last defender",
        "tripping the last man with a clear run on goal (DOGSO)",
        "pulling back the striker one-on-one with the keeper — DOGSO",
        "a crude take-out of an onrushing attacker to deny a goal (DOGSO)",
        "a deliberate stamp on an opponent's ankle after the whistle",
        "a retaliatory kick off the ball",
        "raising hands to an opponent in an aggressive manner",
        "an aggressive forearm into the back of an opponent's head",
        "a vicious elbow in a challenge for a header",
        "biting an opponent",
        "threatening an official with gestures",
        "abusive and offensive language directed at a match official",
        "a dangerous tackle that buckled an opponent's knee",
        "a flying two-footed lunge off the ground",
        "an over-the-top challenge with studs raised into the shin",
        "a cynical hack-down of the last defender (DOGSO)",
        "violent conduct in a mass confrontation",
        "a deliberate elbow away from the ball",
        "a stamp on a grounded opponent",
        "a reckless, high-velocity challenge with no attempt to play the ball",
        "a goalkeeper's foul on an attacker with a clear path to goal (DOGSO)"
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
        "a second caution for time-wasting",
        "a second yellow — a cynical shirt-pull on the break",
        "a second booking — dissent after a contentious decision",
        "a second caution for a professional foul to stop a counter",
        "a second yellow — another deliberate handball",
        "a second booking for simulation in the area",
        "a second caution — a trip to halt a promising attack",
        "a second yellow for encroachment at a set piece",
        "a second booking — persistently fouling the same opponent",
        "a second caution for a high boot on an opponent",
        "a second yellow — a dangerous tackle from behind",
        "a second booking for unsporting conduct",
        "a second caution — an unnecessary late challenge in midfield",
        "a second yellow for deliberate obstruction",
        "a second booking for arguing with the fourth official",
        "a second caution — a scything foul on the wing",
        "a second yellow for delaying the restart once too often"
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
