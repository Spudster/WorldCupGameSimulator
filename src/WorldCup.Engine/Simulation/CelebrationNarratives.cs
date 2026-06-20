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
        "drops to one knee, fist raised, letting the roar wash over him",
        "does a knee slide toward the bench, arms wide, and points both hands at the sky",
        "sprints to the corner, grabs the flag, and shakes it at the supporters",
        "plants his feet and lets out a raw, primal roar at the travelling fans",
        "wheels away with both thumbs up, grinning from ear to ear",
        "jogs calmly to the camera, taps his temple — he always believed",
        "holds his shirt crest outward with both hands, staring down the crowd",
        "clasps both hands together and bows toward the fans",
        "does a full aeroplane run the length of the touchline, turning back only when he runs out of room",
        "punches the air once, twice, three times, then grabs the nearest team-mate in a bear hug",
        "crouches low, arms out, then springs up with a roar as the team floods in",
        "points two fingers to the badge, then to his heart — this one means everything",
        "holds one finger aloft, spinning slowly, acknowledging every corner of the ground",
        "presses both palms flat together and bows his head briefly before the team buries him",
        "windmills both arms and sprints toward the dugout to celebrate with the staff",
        "cups his hands around his mouth and yells at the fans, who yell straight back",
        "performs a little run-up and slides perfectly on the turf, arms raking the air",
        "slaps the badge on his chest with an open palm — job done",
        "gallops the length of the box, hair streaming, before collapsing onto his knees in joy",
        "wraps both arms around himself in a self-hug, eyes closed, savouring it",
        "presses his forehead to the corner flag and lets out a long, satisfied exhale",
        "high-fives every single team-mate who runs over, then finally the goalkeeper",
        "turns to the sideline camera and mouths something unmistakable — sheer delight",
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
        "wheels away open-mouthed, as if he can't quite believe it himself",
        "scales the advertising hoardings, stands on top, and beats his chest to the crowd below",
        "sprints in a huge arc around the pitch, arms pumping like pistons, barely able to contain himself",
        "executes a flawless backflip, sticks the landing, and disappears under a wave of bodies",
        "drops to both knees right on the spot and spreads his arms wide — a cathedral moment",
        "rips his shirt off and whirls it above his head, the crowd delirious",
        "runs forty metres before anyone catches him, then the team piles on in a heap",
        "cups his face in his hands in disbelief, then erupts as the noise hits him",
        "vaults over a sprawling team-mate and nearly cartwheels in pure euphoria",
        "comes to a dead stop, head tilted back, arms hanging loose — completely overwhelmed",
        "slides on his knees the full width of the penalty area, eyes blazing, fists clenched",
        "points at the crossbar — still shaking — and then dissolves into a team embrace",
        "tears toward the corner flag at full pace and plants both palms on it, screaming",
        "darts straight to the bench, climbs into the arms of the manager, and the whole dugout erupts",
        "tears off his gloves and hurls them skyward — that finish needed no luck",
    };

    private static readonly string[] LateWildBank =
    {
        "absolute bedlam — he's sprinting to the bench, the entire squad piling on top",
        "rips off down the touchline, completely lost in the moment, the staff chasing him",
        "wheels away in pure delirium, pointing at the badge and screaming",
        "is at the bottom of a heap of bodies by the corner flag, the whole squad on top",
        "vaults the boards and disappears into a sea of delirious supporters",
        "sinks to his knees, head back, roaring at the heavens — what a time to score",
        "collapses onto the turf, overwhelmed, as his team-mates swarm from every direction",
        "sprints the entire length of the pitch before sinking to his knees at the far end in disbelief",
        "grabs the nearest team-mate, shakes him by the shoulders, and lets out a primal yell",
        "charges toward the dugout, fists hammering the air — the bench is already over the boards",
        "turns to the crowd with both arms raised, then crumbles as the weight of the moment hits him",
        "drops to the ground and punches the turf twice in pure relief",
        "tears off toward the corner, waves both arms at the travelling fans until his team buries him",
        "stands frozen for a fraction of a second — then the dam breaks completely",
        "lets out the longest, loudest roar of his career and the stadium shakes with him",
        "launches himself at the goalkeeper who has sprinted the full length to share the moment",
        "clutches his own face with both hands, then turns to the fans with a wild, disbelieving grin",
        "slumps to both knees and pounds the turf, tears already streaking his face",
        "rips his shirt over his head and runs blind until a team-mate tackles him to the floor in joy",
        "grabs the corner flag and drives it into the ground like a spear — pure electricity",
    };

    private static readonly string[] PenaltyBank =
    {
        "the coolest of finishes — a measured, arms-out celebration and a knowing nod",
        "roars and points to the spot — ice in the veins",
        "puffs out his cheeks, relief as much as joy written all over his face",
        "calmly retrieves the ball, job done, and jogs back with a clenched fist",
        "wheels away, a single finger raised, the pressure released in an instant",
        "turns away from goal and walks a slow, measured path back, letting the calm do the talking",
        "spreads his arms low, palms down, like he was never in doubt for a single moment",
        "closes his eyes for a beat, then opens them and raises both fists",
        "points back at the spot without even looking at the goalkeeper — sheer nerve",
        "shrugs his shoulders, tilts his head, and smiles — textbook",
        "places both hands behind his head, exhales hard, and finally lets himself smile",
        "gives a single pump of the fist and then holds both arms out, accepting the team's embrace",
        "taps his heart twice with a clenched fist, then points to the supporters",
        "jogs away with an almost eerie composure before a team-mate breaks his stride",
        "crouches to pick up the ball, then stands, holds it aloft, and waits for the applause",
        "winks at the camera, folds his arms, and waits calmly for the team to reach him",
    };

    private static readonly string[] MutedBank =
    {
        "can't bear to look — head in his hands, utterly inconsolable",
        "stands frozen, hands on his head, as the other end erupts",
        "is consoled by a team-mate, distraught at what he's just done",
        "sinks to his haunches, staring at the turf, willing it not to have happened",
        "turns away, hands on hips, the picture of dejection",
        "walks slowly back to his position, head bowed, not looking at anyone",
        "crouches down and stays there, arms wrapped around his knees, the noise washing over him",
        "covers his face with both hands and refuses to lift them, every second an agony",
        "drops to both knees and stares blankly at the goal he has just put the ball into",
        "waves a feeble apology at the goalkeeper, who can only shrug back",
        "stands perfectly still amid the chaos, face completely blank — the body's defence against despair",
        "presses his palms to his temples and closes his eyes as the opposition celebrate around him",
        "slumps against the post he deflected the ball into, unable to move",
        "trudges away to the centre circle alone, already willing the restart to come",
        "tugs his shirt collar up to hide his face as he walks, head down, back to position",
        "accepts the consoling arm of a team-mate in silence, unable to respond",
        "sinks to the floor and stays there until a team-mate crouches beside him",
    };

    private static readonly string[] ProvocativeBank =
    {
        "presses a finger firmly to his lips, silencing the home crowd",
        "cups both ears to the jeering supporters, lapping it up",
        "wheels away with a pointed, defiant stare at the opposition bench",
        "kisses his own badge right in front of the rival fans — that won't go down well",
        "holds his arms out wide and soaks up the boos with a grin",
        "strolls in front of the furious home end with his hands clasped behind his back, entirely unbothered",
        "plants himself on the touchline and glares back at the stands with an icy, unblinking calm",
        "slides on his knees directly toward the opposition fans and cups a hand to his ear",
        "turns his back on the howling crowd, arms folded, letting the jeers rain down on him",
        "points to the scoreboard and then points to the supporters — you can't argue with that",
        "jogs slowly along the touchline in front of the furious home fans, nodding at each section",
        "makes a gesture as if conducting a silent orchestra, quieting the boos around him",
        "taps the badge twice in front of the crowd, then turns and walks away without a glance back",
        "stands right on the edge of the box facing the opposition faithful, spreads his arms, and waits",
        "blows a slow, theatrical kiss to the supporters who were booing him all night",
        "turns and flicks his eyes toward the opposition bench with a thin, satisfied smile",
    };

    private static readonly string[] TeamBank =
    {
        "and out comes the choreographed routine — the whole team rocking an imaginary baby",
        "a synchronised dance by the corner flag, rehearsed to perfection",
        "the bench empties to join a giant team huddle by the flag",
        "the squad line up for their now-famous celebration, arms linked",
        "a conga line breaks out by the corner — the celebration of the tournament so far",
        "the whole team forms a train and snakes along the touchline to the fans",
        "a perfectly rehearsed handshake chain unravels down the line of players, flawlessly executed",
        "the squad drops to the turf in unison and performs the worm — the crowd are in raptures",
        "they've practiced this all week: a rippling wave of knee slides along the penalty box",
        "each player takes a bow in turn — a theatrical, well-drilled curtain call",
        "the team forms a tight circle, chanting together, then explodes outward in every direction",
        "every man sprints to the far corner and they pile into a single joyous heap",
        "the bench and the pitch squad link arms and face the supporters — this one was for them",
        "a synchronised point to the sky, held for three beats, then bedlam",
        "the whole team drops to its knees simultaneously — this goal means everything to all of them",
        "they form a human pyramid and somehow it holds just long enough for the cameras to capture it",
        "the squad does the rowing boat in a tight circle, oars and all — a well-kept secret until now",
        "each player mirrors the scorer's run in turn, a game of follow-the-leader built in the training ground",
        "they form a line and shuffle sideways in front of the supporters like a chorus line — perfect timing",
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
