using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// The crowd's voice: generic stadium reactions plus familiar nation-specific chants. These are spoken
/// as their own "Crowd" voice in the commentary transcript, so each fragment is the atmosphere itself,
/// not a description of it. Picked deterministically from the supplied RNG state.
/// </summary>
public static class CrowdChants
{
    private static string Pick(string[] bank, ref Xoshiro256 rng) => bank[rng.NextInt(bank.Length)];

    private static readonly string[] GenericChantBank =
    {
        "Olé, Olé, Olé, Olé — rolling around all four stands!",
        "The da-da-da-da-da-da of Seven Nation Army booming out!",
        "A wall of song, scarves held aloft and twirling!",
        "Drums pounding, the whole end bouncing as one!",
        "\"Stand up if you love your country!\" — and they're all on their feet!",
        "A deep, rhythmic clap building around the bowl!",
        "Flags waving, a sea of colour swaying side to side!",
        "Ninety thousand voices and not one of them sitting down!",
        "The famous \"Po-po-po-po\" ringing around the ground!",
        "A chant that starts in one corner and sweeps the whole stadium!",
        "Bouncing, bouncing — the upper tier visibly shaking!",
        "\"We love you, we love you, we love you!\" echoing down!",
    };

    private static readonly string[] GoalRoarBank =
    {
        "THE STADIUM ERUPTS! An absolute wall of noise!",
        "BEDLAM! They are going wild in every corner!",
        "A roar you can feel in your chest — deafening!",
        "Pandemonium! Strangers hugging strangers in the stands!",
        "The place goes up! Pure, unfiltered delirium!",
        "An eruption of noise — that one nearly took the roof off!",
        "Limbs everywhere! The whole end has fallen over itself!",
        "A guttural, primal roar shakes the upper tier!",
        "They have absolutely lost their minds — and who can blame them!",
        "The decibel meter is off the charts! Scenes of bedlam!",
    };

    private static readonly string[] NearMissBank =
    {
        "Ooooohhhh! A collective gasp around the ground!",
        "Aaaah! Eighty thousand heads in hands as one!",
        "A groan that rolls right around the stadium!",
        "So close! You can hear the sharp intake of breath!",
        "The whole crowd up — and then the deflated sigh!",
        "Inches! And the stands let out an agonised wail!",
    };

    private static readonly string[] TensionBank =
    {
        "You could cut the tension with a knife in here.",
        "A nervous, anxious hum around the stadium.",
        "Every touch greeted with a sharp cheer or a groan now.",
        "Fingernails being chewed all around the ground.",
        "An edgy, crackling atmosphere — nobody daring to breathe.",
    };

    private static readonly string[] BooBank =
    {
        "A chorus of boos rains down from every side!",
        "The crowd making their feelings about that decision very clear!",
        "Jeers and catcalls cascading down onto the pitch!",
        "Thunderous booing — they are absolutely furious!",
    };

    private static readonly string[] WhistlesBank =
    {
        "Shrill, ear-splitting whistles from every corner!",
        "A storm of whistling — they want the referee to end it!",
        "The crowd whistling their disapproval, loud and long!",
    };

    private static readonly string[] PreMatchBank =
    {
        "Flags, flares and a tifo covering an entire end!",
        "The anthem belted out by every single soul in here!",
        "A cauldron of noise even before a ball is kicked!",
        "Scarves held high, a mosaic shimmering across the stands!",
        "Goosebumps stuff — the hairs on the back of the neck standing up!",
    };

    private static readonly string[] LatePushBank =
    {
        "The crowd are trying to suck the ball into the net!",
        "Roaring them forward now — every touch cheered to the rafters!",
        "A relentless wall of noise driving them on!",
        "They can sense it — the whole ground is on its feet, urging them on!",
    };

    private static readonly string[] DisbeliefBank =
    {
        "Stunned, disbelieving silence — you could hear a pin drop.",
        "The home end has gone utterly quiet — shell-shocked.",
        "Silence, save for the small pocket of travelling fans going wild.",
        "An eerie hush falls over the stadium.",
    };

    private static readonly string[] OvationBank =
    {
        "A standing ovation rolls around the ground!",
        "Warm, generous applause from all four sides!",
        "Even the neutrals are on their feet to applaud that!",
    };

    /// <summary>A neutral chant / general atmosphere with no national flavour.</summary>
    public static string GenericChant(ref Xoshiro256 rng) => Pick(GenericChantBank, ref rng);

    /// <summary>The eruption when a goal goes in.</summary>
    public static string GoalRoar(ref Xoshiro256 rng) => Pick(GoalRoarBank, ref rng);

    /// <summary>The collective groan at a near miss.</summary>
    public static string NearMiss(ref Xoshiro256 rng) => Pick(NearMissBank, ref rng);

    /// <summary>A nervy, anxious atmosphere.</summary>
    public static string Tension(ref Xoshiro256 rng) => Pick(TensionBank, ref rng);

    /// <summary>Jeering a decision or the opponent.</summary>
    public static string Boo(ref Xoshiro256 rng) => Pick(BooBank, ref rng);

    /// <summary>Shrill whistles of disapproval.</summary>
    public static string Whistles(ref Xoshiro256 rng) => Pick(WhistlesBank, ref rng);

    /// <summary>The pre-match build-up: anthems, tifos, flags.</summary>
    public static string PreMatchAtmosphere(ref Xoshiro256 rng) => Pick(PreMatchBank, ref rng);

    /// <summary>The crowd roaring the team forward late on.</summary>
    public static string LatePush(ref Xoshiro256 rng) => Pick(LatePushBank, ref rng);

    /// <summary>Stunned silence / shock.</summary>
    public static string Disbelief(ref Xoshiro256 rng) => Pick(DisbeliefBank, ref rng);

    /// <summary>Applause / a standing ovation.</summary>
    public static string Ovation(ref Xoshiro256 rng) => Pick(OvationBank, ref rng);

    // Familiar, recognisable chants by nation (3-letter FIFA codes). Unknown codes fall back to a
    // generic chant, so the model degrades gracefully for every team.
    private static readonly Dictionary<string, string[]> Nation = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BRA"] = new[] { "Samba drums rolling — \"Brasil! Brasil!\"", "A yellow-and-green carnival in full swing!", "\"Eu sou brasileiro, com muito orgulho!\"", "Surdo drums and a thousand whistles — pure Brazil!" },
        ["ARG"] = new[] { "\"Vamos, vamos, Argentina!\"", "\"Muchachos, ahora nos volvimos a ilusionar!\" — the whole end bouncing!", "A sky-blue-and-white wall jumping in unison!", "\"Olé, olé, olé — Diego! Diego!\"" },
        ["MEX"] = new[] { "\"Cielito lindo — ay, ay, ay, ay — canta y no llores!\"", "\"¡Méxi-co! ¡Méxi-co!\"", "\"¡Sí se puede! ¡Sí se puede!\"", "A green tide and a deafening roar of sombreros and song!" },
        ["USA"] = new[] { "\"U-S-A! U-S-A!\"", "\"I believe that we will win!\" — over and over!", "Stars and stripes everywhere, a thunderous chant!", "\"Stand up for the red, white and blue!\"" },
        ["CAN"] = new[] { "\"We the North!\"", "\"Oh, Canada!\" ringing out in full voice!", "A wall of red maple leaves bouncing as one!" },
        ["ENG"] = new[] { "\"It's coming home, it's coming home!\"", "\"Sweet Caroline — bah, bah, bah!\" belted out!", "\"En-ger-land! En-ger-land!\"", "\"Three Lions on a shirt!\"" },
        ["FRA"] = new[] { "\"Allez les Bleus! Allez les Bleus!\"", "La Marseillaise thundering around the ground!", "\"Et un, et deux, et trois — zéro!\"" },
        ["GER"] = new[] { "\"Deutschland! Deutschland!\"", "\"Schland! Schland!\"", "A thunderous, drilled, rhythmic German roar!" },
        ["ESP"] = new[] { "\"¡A por ellos, oé!\"", "\"Yo soy español, español, español!\"", "\"¡España! ¡España!\"" },
        ["POR"] = new[] { "\"Portugal! Portugal!\"", "\"Força Portugal!\"", "A red-and-green roar lifting the roof!" },
        ["NED"] = new[] { "\"Hup Holland Hup!\"", "A sea of orange bouncing — \"Links, rechts!\"", "\"Hand in hand, kameraden!\"" },
        ["ITA"] = new[] { "\"Italia! Italia!\"", "\"Po-po-po-po-po-po!\" — the Seven Nation Army, Italian-style!", "A thunderclap of blue from the Curva!" },
        ["BEL"] = new[] { "\"Allez, allez, les Diables Rouges!\"", "A red wall in full voice!" },
        ["CRO"] = new[] { "A chequered red-and-white wall — \"U boj, u boj!\"", "\"Hrvatska! Hrvatska!\"" },
        ["URU"] = new[] { "\"Soy celeste!\"", "\"¡Uru-guay! ¡Uru-guay!\"" },
        ["COL"] = new[] { "\"¡Colombia! ¡Colombia!\"", "Salsa rhythms and a sea of yellow swaying!" },
        ["JPN"] = new[] { "The Samurai Blue ultras and taiko drums in perfect rhythm!", "\"Nippon! Nippon!\" — drilled and relentless!" },
        ["KOR"] = new[] { "\"Dae~han-min-guk!\" — clap, clap, clap-clap-clap!", "The Red Devils in full, organised voice!" },
        ["MAR"] = new[] { "A deafening, ceaseless Moroccan ultras roar — \"Sir! Sir!\"", "\"Dima Maghrib!\" booming around the ground!" },
        ["SEN"] = new[] { "The 12th Gaïndé — drums, horns and a wall of green!", "\"Sénégal! Sénégal!\"" },
        ["AUS"] = new[] { "\"Aussie! Aussie! Aussie! Oi! Oi! Oi!\"", "A gold-and-green roar — \"Socceroos!\"" },
        ["SCO"] = new[] { "The Tartan Army in full cry — \"Flower of Scotland!\"", "\"Yes Sir, I Can Boogie!\" belted out joyously!" },
        ["DEN"] = new[] { "A red-and-white Roligan roar!", "\"Vi er røde, vi er hvide!\"" },
        ["POL"] = new[] { "\"Polska, biało-czerwoni!\"", "A red-and-white wall booming out!" },
        ["SUI"] = new[] { "\"Hopp Schwiiz!\"", "Cowbells and a red wall in full voice!" },
        ["CIV"] = new[] { "An orange wall of Ivorian drums and song!", "\"Côte d'Ivoire! Côte d'Ivoire!\"" },
        ["NGA"] = new[] { "A green-and-white Super Eagles roar with drums!", "\"Naija! Naija!\"" },
        ["GHA"] = new[] { "Drums, horns and a Black Stars roar!", "\"Ghana! Ghana!\"" },
        ["ECU"] = new[] { "\"¡Ecua-dor! ¡Ecua-dor!\"", "A yellow tide in full song!" },
        ["SRB"] = new[] { "\"Srbija! Srbija!\"", "A thunderous red-and-white roar!" },
    };

    /// <summary>A familiar nation-specific chant for the given team code, or a generic chant if unknown.</summary>
    public static string NationChant(string teamCode, ref Xoshiro256 rng)
    {
        return Nation.TryGetValue(teamCode, out var bank) ? Pick(bank, ref rng) : GenericChant(ref rng);
    }
}
