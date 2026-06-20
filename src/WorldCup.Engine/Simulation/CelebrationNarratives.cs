using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Goal celebrations, chosen by the moment: a routine finish, a screamer, a last-gasp winner, a cool
/// penalty, a provocative dig at the crowd, a choreographed team routine — or, for an own goal, the
/// despair. Fragments name no players/teams; the generator supplies those around them.
/// </summary>
public static class CelebrationNarratives
{
    private static string Pick(string[] bank, ref Xoshiro256 rng) => bank[rng.NextInt(bank.Length)];

    private static readonly string[] StandardBank =
    {
        "wheels away, arms outstretched, and slides on his knees in front of the fans",
        "kisses the badge and points to the supporters",
        "races to the corner flag, fists pumping",
        "is instantly mobbed by his team-mates",
        "points to the sky in tribute",
        "beats his chest and lets out a roar",
        "blows a kiss to the camera and jogs back, all business",
        "leaps into the arms of the nearest team-mate",
        "stands tall, arms spread wide, soaking up the noise",
        "does his trademark little shimmy by the flag",
        "sprints to the touchline and cups a hand to his ear, grinning",
        "salutes the bench, who are up as one",
    };

    private static readonly string[] SpectacularBank =
    {
        "tears off down the touchline, arms wide like an aeroplane",
        "a full-length knee slide and a roar to the heavens — he knows what that was",
        "leaps clean over the advertising hoardings into the crowd",
        "a somersault, no less, then drinks in the adulation",
        "stands stock-still, arms aloft, just letting the moment wash over him",
        "screams into the night sky, veins bulging — pure ecstasy",
        "is buried under a pile of bodies before he can even celebrate",
        "rips his way through the team-mates and slides the length of the box",
    };

    private static readonly string[] LateWildBank =
    {
        "absolute bedlam — he's sprinting to the bench, the entire squad piling on top",
        "rips off down the touchline, completely lost in the moment, the staff chasing him",
        "wheels away in pure delirium, pointing at the badge and screaming",
        "is at the bottom of a heap of bodies by the corner flag, the whole squad on top",
        "vaults the boards and disappears into a sea of delirious supporters",
        "sinks to his knees, head back, roaring at the heavens — what a time to score",
    };

    private static readonly string[] PenaltyBank =
    {
        "the coolest of finishes — a measured, arms-out celebration and a knowing nod",
        "roars and points to the spot — ice in the veins",
        "puffs out his cheeks, relief as much as joy written all over his face",
        "calmly retrieves the ball, job done, and jogs back with a clenched fist",
        "wheels away, a single finger raised, the pressure released in an instant",
    };

    private static readonly string[] MutedBank =
    {
        "can't bear to look — head in his hands, utterly inconsolable",
        "stands frozen, hands on his head, as the other end erupts",
        "is consoled by a team-mate, distraught at what he's just done",
        "sinks to his haunches, staring at the turf, willing it not to have happened",
        "turns away, hands on hips, the picture of dejection",
    };

    private static readonly string[] ProvocativeBank =
    {
        "presses a finger firmly to his lips, silencing the home crowd",
        "cups both ears to the jeering supporters, lapping it up",
        "wheels away with a pointed, defiant stare at the opposition bench",
        "kisses his own badge right in front of the rival fans — that won't go down well",
        "holds his arms out wide and soaks up the boos with a grin",
    };

    private static readonly string[] TeamBank =
    {
        "and out comes the choreographed routine — the whole team rocking an imaginary baby",
        "a synchronised dance by the corner flag, rehearsed to perfection",
        "the bench empties to join a giant team huddle by the flag",
        "the squad line up for their now-famous celebration, arms linked",
        "a conga line breaks out by the corner — the celebration of the tournament so far",
    };

    /// <summary>Picks a celebration to fit the moment (own goal → despair; late, spectacular, penalty,
    /// provocative, choreographed, or a routine finish otherwise).</summary>
    public static string For(GoalType type, double vergazo, int minute, bool isOwnGoal, bool isPenalty, ref Xoshiro256 rng)
    {
        if (isOwnGoal)
        {
            return Pick(MutedBank, ref rng);
        }

        if (rng.NextDouble() < 0.06)
        {
            return Pick(ProvocativeBank, ref rng);
        }

        if (minute >= 80)
        {
            return Pick(LateWildBank, ref rng);
        }

        if (vergazo >= 8.0)
        {
            return Pick(SpectacularBank, ref rng);
        }

        if (isPenalty)
        {
            return Pick(PenaltyBank, ref rng);
        }

        return rng.NextDouble() < 0.18 ? Pick(TeamBank, ref rng) : Pick(StandardBank, ref rng);
    }

    /// <summary>True when a celebration was a provocative dig at the crowd (so they'll boo it).</summary>
    public static bool IsProvocative(string celebration) =>
        celebration.Contains("finger", StringComparison.OrdinalIgnoreCase)
        || celebration.Contains("cups both ears", StringComparison.OrdinalIgnoreCase)
        || celebration.Contains("defiant", StringComparison.OrdinalIgnoreCase)
        || celebration.Contains("rival fans", StringComparison.OrdinalIgnoreCase)
        || celebration.Contains("soaks up the boos", StringComparison.OrdinalIgnoreCase);
}
