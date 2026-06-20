namespace WorldCup.Engine.Simulation;

using WorldCup.Engine.Random;

/// <summary>
/// Play-by-play announcer phrase catalogue for goal events. Each pool is a flat
/// array of spoken commentary fragments containing NO player names, team names or
/// scores — the caller layers those in around the chosen fragment.
/// </summary>
public static partial class CommentaryPhrases
{
    // ---------------------------------------------------------------------
    // GoalShout pools — three excitement tiers.
    // ---------------------------------------------------------------------

    private static readonly string[] GoalShoutsScreamer =
    {
        "OHHH WHAT A GOAL!!!",
        "ARE YOU KIDDING ME?!",
        "WRITE THAT ONE INTO FOLKLORE!",
        "ABSOLUTELY UNBELIEVABLE!!!",
        "OH YOU BEAUTY!!!",
        "GET IN THERE!!!",
        "WHAT ON EARTH HAVE WE JUST SEEN?!",
        "THAT IS SENSATIONAL!!!",
        "I DON'T BELIEVE IT!!!",
        "STOP EVERYTHING — LOOK AT THAT!!!",
        "HE'S DONE IT AGAIN!!!",
        "A GOAL FOR THE AGES!!!",
        "OH MY WORD, WHAT A HIT!!!",
        "THAT'S GONE INTO THE TOP CORNER LIKE A MISSILE!!!",
        "PINCH YOURSELF, THAT WAS REAL!!!",
        "SCREAMER!!! ABSOLUTE SCREAMER!!!",
        "THE ROOF OF THE NET IS STILL SHAKING!!!",
        "WORLD CLASS! WORLD CLASS! WORLD CLASS!!!",
        "OH, THIS IS MAGNIFICENT!!!",
        "TELL ME THAT DIDN'T JUST HAPPEN!!!",
        "BREATHTAKING! SIMPLY BREATHTAKING!!!",
        "FRAME IT AND HANG IT ON THE WALL!!!",
        "GOAL OF THE TOURNAMENT, SURELY!!!",
        "HE'S RIPPED IT INTO THE ROOF OF THE NET!!!",
        "OH, THAT IS DELICIOUS!!!",
        "WHAT A MOMENT, WHAT A GOAL!!!",
        "THE CROWD ARE ON THEIR FEET — AND SO AM I!!!",
        "UTTERLY, UTTERLY SPECTACULAR!!!",
        "THAT'S THE GREATEST GOAL I'VE SEEN IN YEARS!!!",
        "POETRY! ABSOLUTE POETRY!!!",
        "HE HAS LOST HIS MIND AND SO HAVE I!!!",
        "OH, GIVE THAT MAN A STANDING OVATION!!!",
        "THUNDERBOLT!!! AN ABSOLUTE THUNDERBOLT!!!",
        "THAT WILL BE REPLAYED FOR A HUNDRED YEARS!!!",
        "MY GOODNESS, WHAT A FINISH!!!",
        "THE STADIUM HAS ERUPTED!!!",
        "JUST WHEN YOU THOUGHT YOU'D SEEN IT ALL!!!",
        "OH, HE'S A GENIUS, A PURE GENIUS!!!",
    };

    private static readonly string[] GoalShoutsExcited =
    {
        "That is a wonderful goal!",
        "He's buried it!",
        "What a strike!",
        "Oh, that's a lovely finish!",
        "Brilliantly taken!",
        "He's made no mistake there!",
        "A superb goal!",
        "That's a beauty!",
        "He's tucked that away in style!",
        "What composure, what a finish!",
        "Marvellous work, and it's in!",
        "He's lashed it home!",
        "A terrific finish!",
        "That's a goal of real quality!",
        "He's slotted it beautifully!",
        "Oh, that's well struck!",
        "He's picked his spot perfectly!",
        "A fine, fine goal!",
        "Clinically done!",
        "He's smashed it in!",
        "What a way to take it!",
        "That's a cracker!",
        "He's drilled it low and hard!",
        "Top-class finishing!",
        "He's curled it home gorgeously!",
        "A real collector's item!",
        "That's beautifully executed!",
        "He's rifled it past the keeper!",
        "What a delightful goal!",
        "He's finished with aplomb!",
        "Oh, that's classy!",
        "He's planted it in the corner!",
        "A goal to savour!",
        "He's made that look easy!",
        "Sumptuous, just sumptuous!",
        "He's thumped it into the net!",
        "That's a smashing goal!",
    };

    private static readonly string[] GoalShoutsRoutine =
    {
        "It's in!",
        "He scores!",
        "Tucked away.",
        "And that's a goal.",
        "He gets it.",
        "In the back of the net.",
        "That'll do the job.",
        "A goal, and a deserved one.",
        "He makes it count.",
        "Simple as you like.",
        "He puts it away.",
        "There it is.",
        "Job done.",
        "He finds the net.",
        "It nestles in the corner.",
        "He slots it home.",
        "That's gone in.",
        "He converts.",
        "And the keeper had no chance.",
        "It crosses the line.",
        "He buries the chance.",
        "Clean finish.",
        "He makes no mistake.",
        "Goal.",
        "He takes it well enough.",
        "That's a routine finish.",
        "Into the net it goes.",
        "He gets his reward.",
        "He sweeps it in.",
        "Calmly taken.",
        "A tidy finish.",
        "He pops it in.",
        "That'll count.",
        "He gets on the scoresheet.",
        "It finds its way home.",
        "He prods it over the line.",
        "A goal to show for it.",
    };

    // ---------------------------------------------------------------------
    // GoalDescriptor pools — routine + elaborate per GoalType (OwnGoal: single).
    // ---------------------------------------------------------------------

    private static readonly string[] DescOpenPlayRoutine =
    {
        "a tidy finish from inside the box",
        "a simple tap-in",
        "a composed side-footed effort",
        "a sharp finish at the near post",
        "a close-range conversion",
        "a routine strike from open play",
        "a low drive into the corner",
        "a first-time finish",
        "a poke home from a yard out",
        "a calm finish past the keeper",
        "a smart turn and shot",
        "a one-on-one finish",
        "a tucked-away chance",
        "a finish on the half-volley",
        "a guided effort into the net",
        "a prod home from the six-yard box",
        "a controlled strike from the edge of the area",
        "a clean strike through the keeper's legs",
        "a neat finish across goal",
        "a placed shot into the bottom corner",
        "a tap-in at the back post",
        "a scuffed but effective finish",
        "a quick snapshot from inside the box",
        "a measured finish under the keeper",
        "a poacher's strike",
        "a rebound bundled over the line",
        "a stooping effort from close in",
        "a swept finish into the side netting",
        "a toe-poke past the goalkeeper",
        "a simple slot into an open net",
        "a close-range header turned in",
        "a tidy right-footed finish",
        "a tidy left-footed finish",
        "a comfortable conversion from the spot of the action",
        "a clipped finish over the advancing keeper",
        "a finish squeezed in at the near post",
    };

    private static readonly string[] DescOpenPlayElaborate =
    {
        "a mazy run and finish",
        "a sublime piece of individual brilliance",
        "a jinking, weaving solo goal",
        "a breathtaking team move finished in style",
        "a dazzling dribble and clinical strike",
        "an exquisite first-time finish on the run",
        "a stunning curling effort into the top corner",
        "a glorious dink over the onrushing keeper",
        "a magnificent finish after a flowing move",
        "an audacious chipped finish",
        "a wonderfully disguised side-footed strike",
        "a thunderous half-volley into the roof of the net",
        "a sensational solo charge through the defence",
        "a beautifully weighted finish into the far corner",
        "a moment of sheer footballing genius",
        "a perfectly timed run and ruthless finish",
        "a swerving, dipping strike that left the keeper rooted",
        "an outrageous flick and finish",
        "a delightful one-two and clinical conversion",
        "a sumptuous curled effort beyond the dive",
        "a rasping drive arrowed into the corner",
        "a balletic turn and unstoppable shot",
        "a venomous strike on the swivel",
        "a gorgeous bending finish around the defender",
        "a slaloming run past three defenders and a finish",
        "a majestic lofted finish into the top bins",
        "a ferocious effort that nearly burst the net",
        "an instinctive, acrobatic finish",
        "a coolly dispatched finish after a mesmerising run",
        "a thunderbolt struck on the half-turn",
        "a piece of magic conjured from nothing",
        "a flowing, end-to-end move finished emphatically",
        "an extravagant back-heeled finish",
        "a scorching effort drilled into the bottom corner",
        "a beautifully crafted goal from a sweeping counter",
        "a stunning strike that arrowed past the helpless keeper",
    };

    private static readonly string[] DescHeaderRoutine =
    {
        "a header from close range",
        "a nodded finish at the near post",
        "a glancing header into the corner",
        "a downward header past the keeper",
        "a header from the six-yard box",
        "a firm header into the net",
        "a guided header inside the post",
        "a header from a corner",
        "a stooping header at the back post",
        "a header steered home",
        "a header nodded over the line",
        "a header from the centre of the box",
        "a header flicked goalwards",
        "a header met cleanly at the front post",
        "a header bundled in",
        "a routine header from a set-piece",
        "a header planted into the ground and up",
        "a header off the underside of the bar and in",
        "a header from a pinpoint cross",
        "a back-post header tucked away",
        "a header nodded across the keeper",
        "a header rising to meet the delivery",
        "a near-post header turned in",
        "a header looped over the goalkeeper",
        "a header dispatched from a free-kick delivery",
        "a header met under little pressure",
        "a header from a deep cross",
        "a header steered into the bottom corner",
        "a header glanced on and in",
        "a header buried from a yard out",
        "a header nodded down into the net",
        "a simple header at the far post",
        "a header from a whipped delivery",
        "a header thumped into the floor and up over the line",
        "a header directed firmly past the keeper",
        "a header met flush at the near upright",
    };

    private static readonly string[] DescHeaderElaborate =
    {
        "a towering header",
        "a thumping header that left the keeper helpless",
        "a soaring leap and powerful header",
        "an emphatic header crashed into the net",
        "a magnificent header rising above the defence",
        "a bullet header into the top corner",
        "a thunderous downward header",
        "a spectacular header from a towering jump",
        "a commanding header that flew past the keeper",
        "a header of immense power and precision",
        "a glorious looping header over the stranded keeper",
        "a header met with unstoppable force",
        "a header hung in the air before being dispatched",
        "a brave diving header at full stretch",
        "a header thundered in from a leap of remarkable height",
        "a header guided perfectly into the far corner with the flight defied",
        "a header that arrowed in off the underside of the crossbar",
        "a stooping diving header that defied the angle",
        "a header hammered home above a crowd of bodies",
        "a header met flush and rifled into the roof of the net",
        "a header of real authority into the bottom corner",
        "a header rising majestically to bury the cross",
        "a header met with venom at the back post",
        "a header that flew like a shot into the corner",
        "a header crashed in after climbing above two markers",
        "a header sent looping unstoppably beyond the keeper's reach",
        "a header dispatched with the timing of a born goalscorer",
        "a header thumped down and bouncing up into the net",
        "a header drilled in with astonishing power",
        "a header met at the perfect moment and buried",
        "a header that screamed into the top corner",
        "a header of breathtaking elevation and accuracy",
        "a header smashed home from an outrageous hang-time",
        "a header steered devastatingly across the keeper",
        "a header met with such force the net nearly tore",
        "a header of pure aerial dominance",
    };

    private static readonly string[] DescFreeKickRoutine =
    {
        "a well-struck free-kick",
        "a free-kick into the corner",
        "a low free-kick under the wall",
        "a free-kick guided past the wall",
        "a free-kick driven goalwards",
        "a free-kick clipped into the net",
        "a free-kick beyond the goalkeeper",
        "a free-kick squeezed inside the post",
        "a free-kick whipped towards goal",
        "a free-kick placed into the side netting",
        "a free-kick struck cleanly home",
        "a free-kick threaded past the wall",
        "a free-kick deflected in off the wall",
        "a free-kick swung into the danger area and in",
        "a free-kick rolled under the jumping wall",
        "a free-kick directed into the bottom corner",
        "a free-kick that beat the keeper at his near post",
        "a free-kick curled around the defenders",
        "a free-kick that found the gap in the wall",
        "a free-kick struck low and true",
        "a free-kick tucked inside the upright",
        "a free-kick drilled towards the bottom corner",
        "a free-kick floated in and converted",
        "a free-kick steered home from the edge of the box",
        "a free-kick that crept inside the post",
        "a free-kick fired through a gap",
        "a free-kick lifted over the wall and in",
        "a free-kick taken quickly and finished",
        "a free-kick stroked into the corner",
        "a free-kick that found the net via a touch",
        "a free-kick angled past the goalkeeper",
        "a free-kick swept towards goal and in",
        "a free-kick aimed at the near post and converted",
        "a free-kick that nestled inside the far post",
        "a free-kick punched home from range",
        "a free-kick threaded into the bottom corner",
    };

    private static readonly string[] DescFreeKickElaborate =
    {
        "an unstoppable free-kick",
        "a stunning free-kick into the top corner",
        "a magnificent free-kick over the wall",
        "a sensational curling free-kick",
        "a free-kick of pure perfection",
        "a wickedly dipping free-kick",
        "a free-kick that bent gloriously into the corner",
        "a free-kick struck with breathtaking precision",
        "a thunderous free-kick into the roof of the net",
        "a free-kick that swerved viciously beyond the wall",
        "a free-kick whipped unstoppably into the top bins",
        "a free-kick of the very highest class",
        "a free-kick that dipped and curled out of the keeper's reach",
        "a free-kick arrowed into the postage stamp",
        "a free-kick of astonishing technique and power",
        "a free-kick bent around the wall and into the corner",
        "a free-kick struck so sweetly it never looked like missing",
        "a free-kick that defied physics on its way in",
        "a free-kick smashed into the top corner with venom",
        "a free-kick curled exquisitely over the despairing wall",
        "a free-kick of jaw-dropping accuracy",
        "a free-kick that screamed into the upper ninety",
        "a free-kick lashed unstoppably home",
        "a free-kick that left the keeper grasping at air",
        "a free-kick of sublime curve and dip",
        "a free-kick rifled into the only gap available",
        "a free-kick struck with the outside of the boot and bent in",
        "a free-kick that flew like an arrow into the corner",
        "a free-kick of mesmerising trajectory",
        "a free-kick dispatched with ruthless brilliance",
        "a free-kick that whistled into the top corner",
        "a free-kick of outrageous quality from distance",
        "a free-kick bent and dipped beyond all reach",
        "a free-kick hammered unstoppably over the wall",
        "a free-kick that nestled in the top corner like a dream",
        "a free-kick of breathtaking technique into the far angle",
    };

    private static readonly string[] DescPenaltyRoutine =
    {
        "a clinical penalty",
        "a confident penalty into the corner",
        "a coolly taken spot-kick",
        "a penalty sent the wrong way",
        "a penalty drilled down the middle",
        "a penalty tucked into the corner",
        "a penalty struck firmly home",
        "a penalty placed beyond the keeper",
        "a penalty slotted into the bottom corner",
        "a penalty dispatched with ease",
        "a penalty rolled into the net",
        "a penalty hammered home from the spot",
        "a penalty sent into the opposite corner",
        "a penalty calmly converted",
        "a penalty driven into the roof of the net",
        "a penalty stroked into the corner",
        "a penalty that fooled the goalkeeper",
        "a penalty buried with conviction",
        "a penalty steered low and hard",
        "a penalty side-footed past the keeper",
        "a penalty smashed straight down the centre",
        "a penalty planted into the bottom corner",
        "a penalty taken with nerveless composure",
        "a penalty sent high into the net",
        "a penalty placed just inside the post",
        "a penalty thumped into the corner",
        "a penalty dispatched without fuss",
        "a penalty rifled past the dive",
        "a penalty rolled coolly into the side netting",
        "a penalty converted with assurance",
        "a penalty struck cleanly into the corner",
        "a penalty sent the keeper the wrong way entirely",
        "a penalty hit hard and true",
        "a penalty tucked away from twelve yards",
        "a penalty slammed into the top corner",
        "a penalty finished with total confidence",
    };

    private static readonly string[] DescPenaltyElaborate =
    {
        "an audacious Panenka down the middle",
        "an ice-cool penalty into the top corner",
        "a penalty of nerveless brilliance",
        "a penalty dispatched with breathtaking composure",
        "a cheeky chipped penalty straight down the middle",
        "a penalty struck with unanswerable power into the corner",
        "a penalty sent emphatically into the roof of the net",
        "a penalty of supreme confidence under enormous pressure",
        "a penalty that screamed into the top corner",
        "a penalty drilled unstoppably into the bottom corner",
        "a penalty taken with the swagger of a master",
        "a penalty placed with surgical precision",
        "a Panenka of outrageous audacity",
        "a penalty smashed into the very top of the net",
        "a penalty struck with such venom the keeper barely moved",
        "a penalty dispatched coolly despite the world watching",
        "a penalty buried with ruthless conviction in the moment of truth",
        "a penalty sent the keeper the wrong way with a glorious dummy",
        "a penalty thumped unerringly into the postage stamp",
        "a penalty of breathtaking nerve from twelve yards",
        "a penalty rolled with insolent calm down the middle",
        "a penalty crashed into the corner beyond any reach",
        "a penalty taken with the cold blood of a born finisher",
        "a penalty lashed unstoppably into the top corner",
        "a penalty of sublime execution under pressure",
        "a penalty chipped delicately as the keeper dived away",
        "a penalty hammered home with magnificent authority",
        "a penalty struck so true it never looked like missing",
        "a penalty of pure ice from the spot",
        "a penalty dispatched with devastating certainty",
        "a penalty that flew into the top corner like a rocket",
        "a penalty converted with astonishing nerve",
        "a penalty placed inch-perfect into the bottom corner",
        "a penalty smashed home in front of a roaring crowd",
        "a penalty of masterful coolness in the cauldron",
        "a penalty sent unstoppably into the side netting",
    };

    private static readonly string[] DescLongRangeRoutine =
    {
        "a strike from distance",
        "a long-range effort into the corner",
        "a shot from outside the box",
        "a drive from twenty-five yards",
        "an effort from range that beat the keeper",
        "a low strike from distance",
        "a shot from the edge of the area",
        "a long-range drive into the net",
        "a struck effort from outside the box",
        "a shot from distance that found the corner",
        "a drive from range past the goalkeeper",
        "an effort hit from outside the box",
        "a long shot tucked inside the post",
        "a strike from range into the bottom corner",
        "a shot from outside the area into the net",
        "a powerful effort from distance",
        "a drive from the edge of the box",
        "a shot from twenty-odd yards",
        "a long-range attempt that crept in",
        "an effort from deep that beat the keeper",
        "a shot from range into the side netting",
        "a drive that arrowed in from outside",
        "a strike from distance under the bar",
        "a long shot drilled into the corner",
        "a fizzing effort from range",
        "a low drive from outside the box",
        "a shot from distance that found a gap",
        "a struck effort from the edge of the D",
        "an effort lashed from range",
        "a shot from outside that nestled in the corner",
        "a long-range strike into the bottom corner",
        "a drive hit hard from distance",
        "an effort from range that flew past the keeper",
        "a shot from outside angled into the net",
        "a long-distance effort that found the target",
        "a strike from twenty-five yards into the corner",
    };

    private static readonly string[] DescLongRangeElaborate =
    {
        "a thunderous strike from distance",
        "an unstoppable rocket from thirty yards",
        "a screamer from outside the box",
        "a sensational long-range thunderbolt",
        "a stunning drive into the top corner from range",
        "a piledriver from distance into the roof of the net",
        "a breathtaking effort from fully thirty-five yards",
        "a missile launched from outside the area",
        "a dipping, swerving strike from long range",
        "a ferocious effort that flew in from distance",
        "a long-range bullet into the postage stamp",
        "a stunning curling effort from outside the box",
        "a thunderbolt that arrowed into the top corner",
        "a venomous drive from distance beyond all reach",
        "an outrageous strike from the halfway line",
        "a rasping long-range effort into the upper ninety",
        "a howitzer from twenty-five yards into the corner",
        "a magnificent strike that dipped under the bar from range",
        "a long-range effort of jaw-dropping power",
        "a swerving rocket that left the keeper rooted",
        "a sublime curling strike from well outside the box",
        "a thunderclap of a shot from distance",
        "a stunning effort that screamed into the top corner",
        "a long-range strike of breathtaking technique",
        "a vicious dipping drive from thirty yards",
        "a spectacular effort lashed in from range",
        "a long shot that flew unstoppably into the corner",
        "a wonderstrike from the edge of the centre circle",
        "a colossal hit that nearly tore the net from distance",
        "a long-range effort of astonishing accuracy and power",
        "a thumping drive that whistled into the top corner",
        "a glorious strike arrowed in from twenty-eight yards",
        "an absolute belter from outside the box",
        "a long-range missile into the very top of the net",
        "a scorching effort that bent in from distance",
        "a stunning thunderbolt that lit up the stadium from range",
    };

    private static readonly string[] DescBicycleKickRoutine =
    {
        "an overhead kick into the net",
        "a bicycle kick from close range",
        "a scissor-kick finish",
        "an overhead effort guided home",
        "a bicycle kick steered past the keeper",
        "an acrobatic overhead finish",
        "a scissor-kick into the corner",
        "an overhead kick tucked away",
        "a bicycle kick directed goalwards",
        "an overhead effort into the bottom corner",
        "a scissor-kick met cleanly and converted",
        "an overhead kick looped over the keeper",
        "a bicycle kick from inside the six-yard box",
        "an overhead finish at the near post",
        "a scissor-kick volleyed home",
        "an overhead kick prodded over the line",
        "a bicycle kick directed into the net",
        "an overhead effort flicked goalwards and in",
        "a scissor-kick guided into the corner",
        "an overhead kick met under pressure and converted",
        "a bicycle kick steered low into the net",
        "an overhead finish from a hanging cross",
        "a scissor-kick struck on the turn and in",
        "an overhead kick nudged past the keeper",
        "a bicycle kick connected and converted",
        "an overhead effort directed into the side netting",
        "a scissor-kick from the edge of the six-yard box",
        "an overhead kick met flush and finished",
        "a bicycle kick steered home from close in",
        "an overhead finish improvised in the box",
        "a scissor-kick volleyed into the corner",
        "an overhead kick directed firmly into the net",
        "a bicycle kick met cleanly and tucked away",
        "an overhead effort guided beyond the goalkeeper",
        "a scissor-kick connected sweetly and converted",
        "an overhead kick steered into the bottom corner",
    };

    private static readonly string[] DescBicycleKickElaborate =
    {
        "an outrageous overhead kick",
        "a spectacular bicycle kick into the top corner",
        "a jaw-dropping scissor-kick volley",
        "a sensational acrobatic overhead strike",
        "a breathtaking bicycle kick of pure athleticism",
        "an overhead kick of staggering audacity",
        "a stunning scissor-kick crashed into the net",
        "an overhead kick smashed unstoppably home",
        "a gravity-defying bicycle kick into the corner",
        "an overhead strike of impossible brilliance",
        "a scissor-kick volleyed thunderously into the roof of the net",
        "a bicycle kick that will be replayed forever",
        "an acrobatic overhead effort drilled into the top corner",
        "a spectacular airborne volley beyond the keeper",
        "an overhead kick of breathtaking improvisation",
        "a bicycle kick lashed into the corner from mid-air",
        "a scissor-kick of outrageous technique and timing",
        "an overhead strike that left the stadium gasping",
        "a soaring bicycle kick rifled into the net",
        "an acrobatic scissor-kick of the very highest order",
        "an overhead kick met perfectly and thundered home",
        "a bicycle kick of astonishing power and precision",
        "a spectacular overhead effort into the top corner",
        "a scissor-kick volleyed unstoppably beyond the dive",
        "an overhead kick struck with venom in mid-air",
        "a bicycle kick of sheer balletic genius",
        "an overhead strike that defied belief",
        "a scissor-kick smashed into the upper ninety",
        "a bicycle kick connected flush and rocketed in",
        "an overhead kick of jaw-dropping athleticism into the corner",
        "an acrobatic volley flung into the top corner",
        "a bicycle kick that lit up the tournament",
        "an overhead strike hammered home with breathtaking flair",
        "a scissor-kick of magnificent invention and power",
        "a bicycle kick arrowed into the net from an impossible angle",
        "an overhead kick of pure, unforgettable spectacle",
    };

    private static readonly string[] DescOwnGoalNeutral =
    {
        "an own goal under pressure",
        "an unfortunate own goal",
        "a deflection into his own net",
        "a misjudged clearance turned in",
        "an own goal off the defender",
        "a cruel own goal",
        "a deflected effort into his own goal",
        "an own goal from a defensive mix-up",
        "a clearance sliced into his own net",
        "an own goal off the boot of the defender",
        "an unlucky deflection past his own keeper",
        "an own goal under the high ball",
        "a touch into his own net at the near post",
        "an own goal from a mistimed clearance",
        "an own goal that wrong-footed the keeper",
        "a deflection that looped into his own net",
        "an own goal from a goalmouth scramble",
        "an own goal turned in at the back post",
        "an own goal off the defender's shin",
        "a clearance that cannoned into his own goal",
        "an own goal from a desperate intervention",
        "an own goal steered past his own keeper",
        "an own goal off a defender's outstretched leg",
        "an unfortunate deflection into the bottom corner of his own net",
        "an own goal under intense pressure in the box",
        "an own goal from a cross that struck the defender",
        "an own goal that crept in off the post",
        "an own goal from a botched clearance",
        "an own goal nudged in while attempting to clear",
        "an own goal off the heel of the defender",
        "an own goal from a deflection nobody could stop",
        "an own goal as the defender stretched to intervene",
        "an own goal turned past his own goalkeeper",
        "an own goal from a wicked deflection",
        "an own goal sliced into his own net under duress",
        "an own goal from a cruel ricochet",
    };

    // ---------------------------------------------------------------------
    // AssistConnector / SoloEffort / OwnGoal pools.
    // ---------------------------------------------------------------------

    private static readonly string[] AssistConnectors =
    {
        "teed up by",
        "set up by",
        "with the assist from",
        "laid on a plate by",
        "released by",
        "picked out by",
        "fed by",
        "found by",
        "supplied by",
        "served up by",
        "threaded through by",
        "slipped in by",
        "played in by",
        "created by",
        "engineered by",
        "set free by",
        "freed by",
        "carved open by",
        "with the pass from",
        "after a delightful ball from",
        "from the assist of",
        "courtesy of a fine pass from",
        "after being released by",
        "with the through-ball from",
        "off the boot of",
        "after a clever ball from",
        "delivered to him by",
        "presented with the chance by",
        "off the vision of",
        "with the cross from",
        "after a sublime pass from",
        "teed up beautifully by",
        "with the cutback from",
        "from a pinpoint delivery by",
        "with the killer pass from",
        "after the assist by",
    };

    private static readonly string[] SoloEfforts =
    {
        "all his own work",
        "a brilliant solo run",
        "he beat his man and finished",
        "a moment of individual magic",
        "a stunning solo effort",
        "entirely his own doing",
        "a piece of individual brilliance",
        "he did it all himself",
        "a mazy run finished off in style",
        "a dazzling solo goal",
        "he danced through and scored",
        "a wonderful individual effort",
        "he glided past them all and finished",
        "a sensational solo strike",
        "he took it from deep and finished alone",
        "a one-man masterclass",
        "he weaved through the defence and scored",
        "an unstoppable solo run",
        "he beat three men and slotted home",
        "a goal carved out single-handedly",
        "he carried it half the length and finished",
        "a breathtaking individual goal",
        "he jinked past the lot of them and scored",
        "no help needed there at all",
        "he ran from inside his own half and finished",
        "a virtuoso solo performance",
        "he shrugged them off and buried it",
        "a goal of pure individual genius",
        "he twisted and turned before finishing",
        "he left defenders trailing and scored",
        "a solo run for the ages",
        "he beat his marker and finished superbly",
        "he conjured it all by himself",
        "a slaloming run finished off alone",
        "he took on the defence and won",
        "a magnificent piece of solo skill",
    };

    private static readonly string[] OwnGoals =
    {
        "turns it into his own net",
        "an agonising own goal",
        "he's sliced it past his own keeper",
        "the cruellest of own goals",
        "he's turned it into his own goal",
        "a dreadful own goal",
        "he's deflected it past his own keeper",
        "he's bundled it into his own net",
        "an unfortunate own goal",
        "he's put it through his own goal",
        "he's diverted it into his own net",
        "a calamitous own goal",
        "he's steered it past his own goalkeeper",
        "the most unfortunate of own goals",
        "he's deflected it agonisingly into his own net",
        "he's prodded it into his own goal",
        "a heartbreaking own goal",
        "he's helped it into his own net",
        "he's nudged it past his own keeper",
        "a horrible own goal",
        "he's turned the cross into his own goal",
        "he's sliced it agonisingly into his own net",
        "an own goal he'll want to forget",
        "he's glanced it into his own goal",
        "he's poked it past his own goalkeeper",
        "a gut-wrenching own goal",
        "he's redirected it into his own net",
        "he's looped it over his own keeper",
        "the unkindest own goal you'll see",
        "he's deflected it cruelly into his own goal",
        "he's sent it past his own stranded keeper",
        "a wretched own goal",
        "he's turned it agonisingly into the corner of his own net",
        "he's stabbed it into his own goal under pressure",
        "an own goal of the cruellest kind",
        "he's diverted the ball beyond his own goalkeeper",
    };

    /// <summary>
    /// Returns an exclamatory goal shout whose excitement scales with
    /// <paramref name="vergazo"/>: 8.5+ yields a screamer, 6.5+ an excited
    /// shout, otherwise a routine one. Contains no names, teams or scores.
    /// </summary>
    public static string GoalShout(double vergazo, ref Xoshiro256 rng)
    {
        if (vergazo >= 8.5)
        {
            return GoalShoutsScreamer[rng.NextInt(GoalShoutsScreamer.Length)];
        }

        if (vergazo >= 6.5)
        {
            return GoalShoutsExcited[rng.NextInt(GoalShoutsExcited.Length)];
        }

        return GoalShoutsRoutine[rng.NextInt(GoalShoutsRoutine.Length)];
    }

    /// <summary>
    /// Returns a noun-phrase descriptor of the goal, chosen by
    /// <paramref name="type"/>. For most goal types an elaborate pool is used
    /// when <paramref name="vergazo"/> is at least 7.0, the strike travelled at
    /// least 25 metres, or at least three defenders were beaten; otherwise a
    /// routine pool is used. Own goals use a single neutral pool.
    /// </summary>
    public static string GoalDescriptor(GoalType type, double vergazo, double distanceMeters, int defendersBeaten, ref Xoshiro256 rng)
    {
        bool elaborate = vergazo >= 7.0 || distanceMeters >= 25.0 || defendersBeaten >= 3;

        switch (type)
        {
            case GoalType.OpenPlay:
                return elaborate
                    ? DescOpenPlayElaborate[rng.NextInt(DescOpenPlayElaborate.Length)]
                    : DescOpenPlayRoutine[rng.NextInt(DescOpenPlayRoutine.Length)];

            case GoalType.Header:
                return elaborate
                    ? DescHeaderElaborate[rng.NextInt(DescHeaderElaborate.Length)]
                    : DescHeaderRoutine[rng.NextInt(DescHeaderRoutine.Length)];

            case GoalType.FreeKick:
                return elaborate
                    ? DescFreeKickElaborate[rng.NextInt(DescFreeKickElaborate.Length)]
                    : DescFreeKickRoutine[rng.NextInt(DescFreeKickRoutine.Length)];

            case GoalType.Penalty:
                return elaborate
                    ? DescPenaltyElaborate[rng.NextInt(DescPenaltyElaborate.Length)]
                    : DescPenaltyRoutine[rng.NextInt(DescPenaltyRoutine.Length)];

            case GoalType.LongRange:
                return elaborate
                    ? DescLongRangeElaborate[rng.NextInt(DescLongRangeElaborate.Length)]
                    : DescLongRangeRoutine[rng.NextInt(DescLongRangeRoutine.Length)];

            case GoalType.BicycleKick:
                return elaborate
                    ? DescBicycleKickElaborate[rng.NextInt(DescBicycleKickElaborate.Length)]
                    : DescBicycleKickRoutine[rng.NextInt(DescBicycleKickRoutine.Length)];

            case GoalType.OwnGoal:
            default:
                return DescOwnGoalNeutral[rng.NextInt(DescOwnGoalNeutral.Length)];
        }
    }

    /// <summary>
    /// Returns a connector phrase introducing the provider of an assist
    /// (e.g. "teed up by"). Contains no names, teams or scores.
    /// </summary>
    public static string AssistConnector(ref Xoshiro256 rng)
    {
        return AssistConnectors[rng.NextInt(AssistConnectors.Length)];
    }

    /// <summary>
    /// Returns a phrase describing a goal as an individual solo effort
    /// (e.g. "a brilliant solo run"). Contains no names, teams or scores.
    /// </summary>
    public static string SoloEffort(ref Xoshiro256 rng)
    {
        return SoloEfforts[rng.NextInt(SoloEfforts.Length)];
    }

    /// <summary>
    /// Returns a phrase describing an own goal (e.g. "an agonising own goal").
    /// Contains no names, teams or scores.
    /// </summary>
    public static string OwnGoal(ref Xoshiro256 rng)
    {
        return OwnGoals[rng.NextInt(OwnGoals.Length)];
    }
}
