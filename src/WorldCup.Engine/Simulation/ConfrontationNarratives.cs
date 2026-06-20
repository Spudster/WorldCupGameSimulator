using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Provides randomly selected, name-free commentary fragments describing on-field
/// flashpoints between players and benches, escalating from harmless handbags through
/// full brawls to the referee restoring order. Each fragment is a self-contained
/// broadcast phrase containing no player names, team names, or scores so the generator
/// can supply that context around it.
/// </summary>
public static class ConfrontationNarratives
{
    /// <summary>
    /// Minor, almost comic pushing and chesting that fizzles out to nothing.
    /// </summary>
    private static readonly string[] HandbagsBank =
    {
        "a bit of handbags, nothing more",
        "chest-to-chest, foreheads almost touching, but it's all front",
        "a shove and a sneer — handbags at dawn",
        "all puffed-out chests and nobody actually swinging",
        "a half-hearted push in the back, and that's about the size of it",
        "plenty of posturing, not a lot of punching",
        "they bump chests, peacocking, and then think better of it",
        "a flick of the hand, a roll of the eyes — pure theatre",
        "a token little shove that wouldn't disturb a feather",
        "the two of them squaring up like it's the playground",
        "a bit of pushing and shoving that fizzles out almost instantly",
        "all mouth and no menace — classic handbags",
        "a gentle barge and a wagging finger, nothing in it really",
        "they grab each other's shirts and then sheepishly let go",
        "more huff and puff than genuine aggro",
        "a cartoonish little dust-up, both of them playing to the gallery",
        "a nudge, a nudge back, and it peters out",
        "shirt-tugging and tutting — it's all very polite, really",
        "a bit of argy-bargy, but you sense their hearts aren't in it",
        "they lean into one another, neither prepared to actually do anything",
        "a comedy shove and an even more comedic stagger back",
        "a flap of the arms, a bit of chuntering, and it's done",
        "the softest of pushes, met with the most theatrical of falls",
        "a quick chest-bump and they're already backing away",
        "minor handbags — the sort that's forgotten in seconds",
        "they jostle and grumble, but it never threatens to ignite",
        "a feeble little prod and an even feebler response",
        "all bristle and no bite, the pair of them",
        "a bit of finger-pointing and pantomime outrage",
        "they square up, realise the camera's on them, and ease off",
        "a playful-looking shove that's gone almost before it started",
        "hand on the chest, hand on the chest back — and that's your lot",
        "a little flurry of pushing that wouldn't trouble a turnstile",
        "they make a show of it, but there's nothing behind the bluster",
        "barely a ripple — handbags, and weak ones at that",
        "a token barge, a token glare, and on we go",
        "a fuss about nothing, the two of them quickly thinking better of it",
    };

    /// <summary>
    /// Two players squaring up nose to nose, with words exchanged and the temperature rising.
    /// </summary>
    private static readonly string[] FaceOffBank =
    {
        "nose to nose now, and the words are flying",
        "eyeball to eyeball, neither willing to back down",
        "a finger jabbed into the chest, and it's getting heated",
        "they're right up in each other's faces, foreheads almost meeting",
        "toe to toe, jaws jutting, and the language is colourful",
        "a real stand-off — chins up, stares locked",
        "they square up properly, chest puffed against chest",
        "you can see the words being spat between them",
        "neither one taking a backward step here",
        "snarling at each other from inches apart",
        "a proper face-off, the pair of them bristling",
        "they lock eyes and the temperature has rocketed",
        "an aggressive prod to the shoulder, and the staring contest begins",
        "they go nose to nose, daring the other to make the first move",
        "jabbing fingers and bared teeth — this is simmering",
        "a long, hostile stare and plenty being said",
        "they front up to one another, neither blinking",
        "right in his grill, mouthing off, refusing to give an inch",
        "the two of them squared up, chests heaving, words exchanged",
        "a venomous little exchange, faces just inches apart",
        "they're squaring up like a couple of boxers at the weigh-in",
        "shoulders back, chins forward, the stare-down is on",
        "a hostile face-off, jaws working overtime",
        "they get nose to nose, and you can lip-read most of it",
        "a finger in the face and a mouthful to go with it",
        "neither prepared to drop the gaze — this is brewing",
        "they front up, eyeballs out, the words getting nastier",
        "a real eyeball-to-eyeball confrontation now",
        "chest against chest, foreheads down, and the chuntering builds",
        "they square off, both jabbing, both refusing to step away",
        "a pointed finger, a curled lip, and the heat is rising fast",
        "the two of them locked in a furious stare, mouths going",
        "noses almost touching, voices rising — this could boil over",
        "they front each other up, neither giving the other the satisfaction",
        "a proper squaring-up, both of them spoiling for it",
        "an angry face-off, fingers prodding, words sharpening",
        "eye to eye, breath to breath, and it's turning ugly",
    };

    /// <summary>
    /// A genuine shoving match that drags more and more players into the middle.
    /// </summary>
    private static readonly string[] ShovingBank =
    {
        "and now there's a melee — players piling in from everywhere",
        "a proper shoving match, bodies flying into the mix",
        "it's kicked off — half a dozen players grappling",
        "shirts being grabbed, players shoved this way and that",
        "a real scuffle now, more and more of them wading in",
        "pushing and barging, the crowd around the ball swelling",
        "they're shoving each other hard, and others are joining in",
        "a scrum of bodies, everyone trying to get a piece of it",
        "players streaming in to shove and be shoved",
        "a heaving mass of players, plenty of pushing going on",
        "it's getting physical — shirts pulled, shoulders barged",
        "a proper grappling match breaking out in the middle",
        "shoves landing, players stumbling, a real tangle now",
        "bodies piling in, hands on chests, everyone having a go",
        "a knot of players shoving and jostling, tempers up",
        "the pushing has spread — six, seven players in the thick of it",
        "a genuine scuffle, players being barged off their feet",
        "they're wrestling and shoving, the whole thing snowballing",
        "a chaotic little maul, players shoving from all angles",
        "shirts stretched, shoulders flying — this is a proper shoving match",
        "more bodies pouring in, the shoving intensifying",
        "it's a tangle of limbs, everyone pushing somebody",
        "a real ruck of players, barging and grappling",
        "the scuffle's grown — players being dragged and shoved around",
        "hands shoving faces away, players piling into the middle",
        "a heaving scrum of shoving and shirt-pulling",
        "players being flung aside as more wade into the scuffle",
        "a genuine grapple now, bodies crashing together",
        "the shoving match is pulling in everyone within reach",
        "shoulders and chests barging, players tumbling into one another",
        "it's a free-for-all of pushing and pulling out there",
        "a swelling melee, players shoving with real intent",
        "they're grappling and barging, the whole pack of them",
        "a proper scrap of shoving, players stumbling over each other",
        "bodies hurled into the mix, the pushing getting fiercer",
        "a churning knot of players, all shoving, all jostling",
        "the scuffle's well and truly on — players grappling everywhere",
    };

    /// <summary>
    /// A serious, ugly brawl with many players involved that has badly boiled over.
    /// </summary>
    private static readonly string[] MassConfrontationBank =
    {
        "this has completely boiled over — a full-scale melee in the corner",
        "an ugly, ugly scene, players being dragged apart",
        "absolute chaos — this is the last thing the tournament needed",
        "it's a full-blown brawl now, bodies everywhere",
        "this is dreadful — a mass confrontation, players swarming in",
        "an enormous melee, and it's turned genuinely nasty",
        "players grabbing players, a horrible tangle of bodies",
        "this is a disgrace — a huge, ugly free-for-all",
        "a sprawling brawl, fists and shoves flying in the chaos",
        "the whole thing has erupted — players piling in from every direction",
        "an almighty melee, and tempers have completely snapped",
        "this is bedlam — a full-scale confrontation engulfing the pitch",
        "an ugly mass of bodies, players being hauled out by their shirts",
        "it's gone completely — a vicious, swirling brawl",
        "a horrible scene, players grappling and grabbing en masse",
        "this has boiled right over — a serious, ugly confrontation",
        "a full-on melee, and it's getting more violent by the second",
        "players everywhere, shoving, grabbing, dragging — total mayhem",
        "an enormous, ugly pile-up of players, and it's turning sour",
        "this is exactly what nobody wanted — a mass brawl in full flow",
        "a swirling sea of bodies, players hurling each other around",
        "it's anarchy out there — a huge confrontation, nobody backing off",
        "an ugly mob of players, fists clenched, shoves landing",
        "a vicious melee, and the stewards must be dreading this",
        "the whole squad of them piling in — this is a proper brawl",
        "players locked together in an ugly, heaving mass",
        "a serious confrontation now, players being prised apart one by one",
        "this is shameful — a full-scale brawl in front of a global audience",
        "an enormous flashpoint, players streaming in to join the fight",
        "it's boiled over completely — a churning, furious mob",
        "a horrible, ugly melee, and it shows no sign of calming",
        "players grabbing throats and shirts in a sprawling brawl",
        "this is as ugly as it gets — a mass of grappling bodies",
        "a vicious, swirling confrontation, players hauled out by teammates",
        "the whole pitch seems to have converged — an almighty brawl",
        "an ugly scrum of fury, players being dragged clear by the collar",
        "it's erupted into a full brawl — an awful look for the game",
    };

    /// <summary>
    /// The benches, substitutes, and staff spilling onto the pitch to join the trouble.
    /// </summary>
    private static readonly string[] BenchInvolvedBank =
    {
        "and now the benches have emptied — staff and subs sprinting on!",
        "the technical areas have cleared, coaches wading in to drag their players out",
        "substitutes pouring onto the pitch — this is bedlam on the touchline",
        "the benches are emptying, subs charging onto the field",
        "staff and substitutes streaming on — this is completely out of hand",
        "the dugouts have emptied, everyone sprinting into the fray",
        "subs in their bibs racing on to join the confrontation",
        "the whole bench is up and onto the pitch now",
        "coaching staff and subs flooding on — utter chaos",
        "the touchline has emptied, bodies pouring onto the field",
        "substitutes hurdling the advertising boards to get involved",
        "physios, subs, coaches — the lot of them are on the pitch",
        "both benches have cleared, and it's mayhem out there",
        "the reserves are charging on, fuelling the confrontation",
        "staff sprinting from the dugout to haul their players away",
        "the subs have piled on, and now it's truly enormous",
        "the technical area's deserted — everyone's in the middle of it",
        "bench players in bibs wading into the brawl",
        "the dugouts have emptied as one — players streaming on",
        "kit men and coaches dragged into the chaos by their own players",
        "the entire bench up and racing onto the field",
        "subs and staff flooding on, the trouble doubling in size",
        "the touchline's gone — everyone has spilled onto the pitch",
        "substitutes sprinting on, some to fight, some to drag others off",
        "the benches have well and truly emptied now",
        "coaching staff charging across the turf to break it up",
        "a wave of subs and staff pouring off both benches",
        "the dugout's cleared — bib-wearing subs piling into the scrum",
        "everyone from the bench is on, and it's turned into bedlam",
        "the touchline staff have abandoned their posts and run on",
        "subs streaming over the line, the confrontation swelling massively",
        "the whole matchday squad seems to be on the pitch now",
        "benches emptied, staff and players tangled together in the chaos",
        "substitutes vaulting the boards, racing to join the melee",
        "the technical areas have spilled onto the field — pure chaos",
        "every last sub and coach is out there now — extraordinary scenes",
        "the benches have erupted, bodies flooding onto the pitch",
    };

    /// <summary>
    /// The managers and coaching staff squaring up at one another on the touchline.
    /// </summary>
    private static readonly string[] TouchlineRowBank =
    {
        "the two dugouts going at it now — the managers nose to nose",
        "fourth official caught in the middle of a touchline row",
        "the managers squaring up in the technical area",
        "a furious touchline row — the two bosses jabbing fingers",
        "the coaching staff of both sides at each other on the sideline",
        "the managers eyeball to eyeball, the fourth official powerless",
        "a proper set-to in the technical areas, the bosses bristling",
        "the two benches trading words, the managers leading the charge",
        "the dugouts have squared up — coaches nose to nose",
        "an ugly touchline row, the fourth official pleading for calm",
        "the managers have to be kept apart by their own staff",
        "the two head coaches jabbing fingers across the white line",
        "a heated exchange in the technical area, the bosses going at it",
        "the assistants wading in as the managers square up",
        "the touchline's a battleground — coaching staff trading insults",
        "the fourth official stepping between two furious managers",
        "the two bosses chest to chest on the edge of the technical area",
        "a real touchline ruck, the dugouts spilling words at each other",
        "the managers nose to nose, the fourth official trying to part them",
        "coaching staff from both camps squaring up by the boards",
        "the two technical areas have merged into one furious row",
        "the bosses jabbing fingers, faces reddening on the sideline",
        "an almighty touchline argument, staff pulling their managers back",
        "the dugouts trading barbs, the fourth official caught in the crossfire",
        "the managers up in each other's faces by the halfway line",
        "a touchline confrontation — both benches on their feet, raging",
        "the head coaches squaring up, assistants hauling them apart",
        "a venomous touchline row, fingers pointed, voices raised",
        "the two managers refusing to back down on the sideline",
        "the fourth official sandwiched between two seething dugouts",
        "coaching staff bristling, the managers leading a touchline spat",
        "the technical areas at war — bosses nose to nose, staff intervening",
        "an ill-tempered touchline row, the managers jawing at each other",
        "the dugouts squaring up, the fourth official begging for order",
        "the bosses going toe to toe right on the touchline",
        "a furious sideline confrontation between the two coaching teams",
        "the managers practically nose to nose, neither willing to relent",
    };

    /// <summary>
    /// The referee stepping in to restore order and bring the flashpoint to an end.
    /// </summary>
    private static readonly string[] RefCalmsBank =
    {
        "the referee sprinting in, arms wide, pulling them apart",
        "the captains called over to calm their players down",
        "order slowly restored, the referee reaching for his pocket",
        "the referee wading into the middle, separating the worst of it",
        "the official blowing hard on his whistle, trying to take control",
        "the referee dragging players apart one by one",
        "the captains summoned, told to get their teammates under control",
        "the referee restoring calm, notebook already in hand",
        "the official ushering players back, gradually cooling it down",
        "the referee stepping between the main protagonists",
        "calm slowly returning as the referee asserts himself",
        "the official pulling shirts apart, demanding everyone backs off",
        "the referee shepherding players away from the flashpoint",
        "the captains doing the referee's work, pulling teammates clear",
        "the official restoring order, reaching for his cards",
        "the referee in the thick of it, arms spread, keeping them apart",
        "peace gradually breaking out as the referee takes charge",
        "the official marching the worst offenders away from each other",
        "the referee blowing repeatedly, hauling the sides apart",
        "order being restored, the referee noting the names down",
        "the captains called in, instructed to calm the situation",
        "the referee separating them, finger raised in warning",
        "the official ushering everyone back to a safe distance",
        "calm slowly settling as the referee reasserts his authority",
        "the referee pulling the last few apart, reaching for his pocket",
        "the official stepping in firmly, the temperature dropping",
        "the referee corralling the players, demanding they disperse",
        "order returning at last, the referee fishing out the cards",
        "the captains dragging their men away at the referee's request",
        "the official wading through the bodies, restoring some order",
        "the referee taking names, the worst of it now subsiding",
        "calm at last, the referee marching off to dish out cards",
        "the official separating the pair, both ushered firmly apart",
        "the referee getting a grip on it, players backing away",
        "order finally restored, the referee with his cards at the ready",
        "the captains pulling rank, helping the referee cool the flames",
        "the referee planting himself between them, arms outstretched",
    };

    /// <summary>
    /// The trigger that lit the fuse for the confrontation.
    /// </summary>
    private static readonly string[] SparkBank =
    {
        "a late, nasty challenge has sparked it",
        "a stamp, an elbow, and tempers detonate",
        "a clash of heads off the ball, and now it's all going off",
        "time-wasting at the corner flag was the trigger",
        "a studs-up lunge has lit the fuse",
        "an off-the-ball shove was all it took",
        "a flailing elbow has set the whole thing off",
        "a cynical trip from behind has ignited it",
        "a stamp on the ankle, and tempers boil over instantly",
        "a wild, two-footed challenge has detonated the lot",
        "a sly dig in the ribs was the spark",
        "an over-the-top tackle has kicked it all off",
        "a shirt-pull and a face-rub, and tempers flare",
        "a kick out in frustration has lit it up",
        "a scything challenge from behind sets it ablaze",
        "an elbow into the back of the head — that's the trigger",
        "some niggle, some words, and now it's erupted",
        "a stray boot to the shins, and it's all going off",
        "provocation in the celebration was the spark",
        "a nudge off the ball, then a retaliation, and away we go",
        "a reckless slide has tipped the whole thing over",
        "a tug on the shirt that snapped someone's patience",
        "a forearm to the chin has set tempers ablaze",
        "a deliberate barge into the keeper has lit the touchpaper",
        "a flying challenge well after the ball had gone — and it's off",
        "a bit of gamesmanship has been the final straw",
        "a knee into the back, and the powder keg goes up",
        "a sly stamp away from the ball was the catalyst",
        "a needless shove in the back has triggered it all",
        "a high boot near the face has lit the fuse",
        "some afters from a 50-50, and tempers detonate",
        "a trip and a stand-over have set the whole thing alight",
        "a clattering challenge late in the move has sparked the lot",
        "a finger in the face after the whistle — and it ignites",
        "a stray arm in an aerial duel has kicked it off",
        "a niggly foul and a refusal to apologise lit the fuse",
        "a raking of the studs down the achilles — and it explodes",
    };

    /// <summary>
    /// Returns a fragment describing minor, almost comic pushing and chesting that fizzles out.
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A randomly selected handbags fragment.</returns>
    public static string Handbags(ref Xoshiro256 rng) => HandbagsBank[rng.NextInt(HandbagsBank.Length)];

    /// <summary>
    /// Returns a fragment describing two players squaring up nose to nose with words exchanged.
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A randomly selected face-off fragment.</returns>
    public static string FaceOff(ref Xoshiro256 rng) => FaceOffBank[rng.NextInt(FaceOffBank.Length)];

    /// <summary>
    /// Returns a fragment describing a genuine shoving match that drags players in.
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A randomly selected shoving fragment.</returns>
    public static string Shoving(ref Xoshiro256 rng) => ShovingBank[rng.NextInt(ShovingBank.Length)];

    /// <summary>
    /// Returns a fragment describing a serious, ugly brawl with many players involved.
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A randomly selected mass-confrontation fragment.</returns>
    public static string MassConfrontation(ref Xoshiro256 rng) => MassConfrontationBank[rng.NextInt(MassConfrontationBank.Length)];

    /// <summary>
    /// Returns a fragment describing the benches and staff spilling onto the pitch.
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A randomly selected bench-involved fragment.</returns>
    public static string BenchInvolved(ref Xoshiro256 rng) => BenchInvolvedBank[rng.NextInt(BenchInvolvedBank.Length)];

    /// <summary>
    /// Returns a fragment describing managers and coaching staff squaring up on the touchline.
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A randomly selected touchline-row fragment.</returns>
    public static string TouchlineRow(ref Xoshiro256 rng) => TouchlineRowBank[rng.NextInt(TouchlineRowBank.Length)];

    /// <summary>
    /// Returns a fragment describing the referee stepping in to restore order.
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A randomly selected referee-calms fragment.</returns>
    public static string RefCalms(ref Xoshiro256 rng) => RefCalmsBank[rng.NextInt(RefCalmsBank.Length)];

    /// <summary>
    /// Returns a fragment describing the trigger that lit the fuse for the confrontation.
    /// </summary>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A randomly selected spark fragment.</returns>
    public static string Spark(ref Xoshiro256 rng) => SparkBank[rng.NextInt(SparkBank.Length)];
}
