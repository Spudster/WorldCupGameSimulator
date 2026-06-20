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
        "A thunderous call-and-response roar from both ends of the ground!",
        "The lower tier doing the wave and the upper tier catching it!",
        "Handclaps synced to a beat — the whole bowl locked together!",
        "A sea of phone torches twinkling in the dark of the stands!",
        "Thousands of scarves raised high, held there, not moving an inch!",
        "\"Oooh-aaah!\" — a rhythmic surge rolling around the terraces!",
        "The noise is simply relentless — this crowd refuses to sit down!",
        "Every single section of the stadium in full voice — extraordinary!",
        "A tribal drumbeat underscoring an endless wall of chant!",
        "They are singing their hearts out — every word, every line!",
        "\"We're not going home!\" — and they mean every syllable of it!",
        "The whole stadium bouncing in unison — breathtaking to witness!",
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
        "PANDEMONIUM! The net barely stopped shaking before they did!",
        "An explosion of joy — flags, scarves, arms all flying at once!",
        "The whole stadium surges forward — an ocean of pure ecstasy!",
        "That roar will echo around the world tonight!",
        "Delirium! Total, absolute, glorious delirium in the stands!",
        "Tears, embraces, screams — they will remember this for a lifetime!",
        "The noise is PHYSICAL — you can feel it pressing against you!",
        "Every single soul in this ground on their feet at once!",
        "Cue the celebrations — this end has completely lost it!",
        "A wave of pure disbelieving joy crashes around the bowl!",
    };

    private static readonly string[] NearMissBank =
    {
        "Ooooohhhh! A collective gasp around the ground!",
        "Aaaah! Eighty thousand heads in hands as one!",
        "A groan that rolls right around the stadium!",
        "So close! You can hear the sharp intake of breath!",
        "The whole crowd up — and then the deflated sigh!",
        "Inches! And the stands let out an agonised wail!",
        "Oh! Oh! That was SO close — the crowd can barely watch!",
        "A groan of pure anguish from thousands of lungs at once!",
        "The crowd rises, the crowd falls — a collective heartbreak!",
        "Hands on heads, eyes closed — that should have been one!",
        "A wall of despair — they were all off their seats for that!",
        "The whole stadium gasps and then holds its breath!",
        "So, so close — and you can hear the frustration pouring down!",
        "Ohhhhhh! A moan that fills every cubic metre of this place!",
    };

    private static readonly string[] TensionBank =
    {
        "You could cut the tension with a knife in here.",
        "A nervous, anxious hum around the stadium.",
        "Every touch greeted with a sharp cheer or a groan now.",
        "Fingernails being chewed all around the ground.",
        "An edgy, crackling atmosphere — nobody daring to breathe.",
        "The crowd barely making a sound — holding their breath collectively.",
        "A low, worried murmur rippling through every section of the stand.",
        "Twitchy, nervous energy — even the stewards look on edge.",
        "You can feel the anxiety in here — it's almost tangible.",
        "Hushed, clipped, clapping — urging rather than roaring.",
        "The stadium is wound like a coil, ready to explode either way.",
        "Every clearance cheered as if it were a goal — nerves shot.",
        "A tension that only football at its highest level can produce.",
        "Even the commentators have lowered their voices instinctively.",
    };

    private static readonly string[] BooBank =
    {
        "A chorus of boos rains down from every side!",
        "The crowd making their feelings about that decision very clear!",
        "Jeers and catcalls cascading down onto the pitch!",
        "Thunderous booing — they are absolutely furious!",
        "A wave of disapproval rolls around the entire stadium!",
        "They are NOT happy — and they want everyone to know it!",
        "Boos ringing off every surface — deafening disapproval!",
        "The crowd letting that decision live rent-free in their minds — loudly!",
        "An angry roar of disagreement from the terraces!",
        "Every section of the ground joins the chorus of displeasure!",
    };

    private static readonly string[] WhistlesBank =
    {
        "Shrill, ear-splitting whistles from every corner!",
        "A storm of whistling — they want the referee to end it!",
        "The crowd whistling their disapproval, loud and long!",
        "A piercing, relentless chorus of whistles fills the air!",
        "Eighty thousand people blowing imaginary whistles in unison!",
        "Sharp, indignant whistling — they cannot believe what they've seen!",
        "The referee is getting an absolute earful from the stands!",
        "Whistles and jeers — a complete breakdown in patience out here!",
    };

    private static readonly string[] PreMatchBank =
    {
        "Flags, flares and a tifo covering an entire end!",
        "The anthem belted out by every single soul in here!",
        "A cauldron of noise even before a ball is kicked!",
        "Scarves held high, a mosaic shimmering across the stands!",
        "Goosebumps stuff — the hairs on the back of the neck standing up!",
        "A breathtaking tifo unfurls — the whole stadium gasps!",
        "The roar as the teams emerge from the tunnel is simply enormous!",
        "Drums, horns, flags — a carnival atmosphere before kick-off!",
        "Both sets of fans trying to out-sing one another — spectacular!",
        "The anthems finishing and the crowd simply taking over!",
        "A cauldron already at boiling point and the game hasn't even started!",
        "Flares painting the sky — a stadium on fire with anticipation!",
        "Every seat filled, every voice ready — this place is absolutely buzzing!",
    };

    private static readonly string[] LatePushBank =
    {
        "The crowd are trying to suck the ball into the net!",
        "Roaring them forward now — every touch cheered to the rafters!",
        "A relentless wall of noise driving them on!",
        "They can sense it — the whole ground is on its feet, urging them on!",
        "The noise is getting louder — the crowd demanding one last effort!",
        "A desperate, imploring roar — \"Don't give up!\"",
        "Clapping, stamping, singing — everything thrown at this late push!",
        "The stadium is shaking them forward with sheer force of will!",
        "One more! One more! The crowd insisting there is still time!",
        "A wall of sound that a goalkeeper can feel in their gloves!",
        "Every corner, every set piece met with an enormous roar of hope!",
        "They refuse to accept it's over — and the noise reflects that completely!",
    };

    private static readonly string[] DisbeliefBank =
    {
        "Stunned, disbelieving silence — you could hear a pin drop.",
        "The home end has gone utterly quiet — shell-shocked.",
        "Silence, save for the small pocket of travelling fans going wild.",
        "An eerie hush falls over the stadium.",
        "Thousands of open mouths and no sound coming out.",
        "The silence is almost louder than any chant could be.",
        "People just staring, unable to process what they've just seen.",
        "A moment of collective shock — the whole stadium frozen.",
        "Not a sound — just a stunned, bewildered, disbelieving stillness.",
        "The crowd robbed of voice, robbed of movement, robbed of breath.",
    };

    private static readonly string[] OvationBank =
    {
        "A standing ovation rolls around the ground!",
        "Warm, generous applause from all four sides!",
        "Even the neutrals are on their feet to applaud that!",
        "Sustained, respectful applause — they recognise greatness here!",
        "The whole stadium on their feet, clapping as one!",
        "A magnificent reception from a magnificent crowd!",
        "Applause that lasts and lasts — they don't want it to stop!",
        "Both sets of supporters joining in the appreciation — wonderful!",
        "A rapturous ovation — this crowd knows a special moment when they see one!",
    };

    private static readonly string[] OlePassingSpellBank =
    {
        "Olé! Olé! Olé! — every pass greeted with a roar!",
        "The crowd playing along — an Olé for every touch!",
        "A symphony of Olés ringing out with each slick one-touch pass!",
        "They are loving this — the Olés are getting louder with every exchange!",
        "Olé! Olé! Olé! The crowd making the opposition's day feel very long!",
        "Pass after pass, Olé after Olé — the stadium is purring!",
        "The crowd narrating every touch with a gleeful, rolling Olé!",
        "Even the most neutral observer is smiling — this is exhibition football!",
    };

    private static readonly string[] SaveOvationBank =
    {
        "An enormous roar for that save — the goalkeeper's name booming out!",
        "WHAT A SAVE! The crowd on their feet to acclaim it!",
        "They cheered that save almost as loud as a goal!",
        "The goalkeeper's name chanted around every corner of the stadium!",
        "A save that lifts the entire stadium off its feet!",
        "Applause and disbelief — how on earth did they keep that out?!",
        "The crowd roaring their appreciation — a world-class stop!",
        "A collective \"ohhhh\" of astonishment turning instantly into a cheer!",
    };

    private static readonly string[] TauntingOppositionBank =
    {
        "The home end turning to taunt the travelling support — all in good fun!",
        "A joyful, sing-song chorus directed at the visiting end!",
        "The winning side's fans serenading the opposition with a cheery wave!",
        "Good-natured banter rolling across the stadium divide!",
        "\"Can you hear us? Can you hear us?\" — playfully aimed at the quieter end!",
        "The winning support in full voice, making sure the visitors can hear it!",
        "A cheerful, cheeky song aimed across the halfway line!",
    };

    private static readonly string[] DefendingLeadBank =
    {
        "A nervy but defiant chant — they want this result!",
        "The home fans urging their defence to hold firm!",
        "Every clearance met with enormous, grateful relief!",
        "Clapping them home — \"Keep it! Keep it!\"",
        "A tense, determined roar supporting every tackle, every block!",
        "The crowd throwing an imaginary arm around their defenders!",
        "Agonising, desperate support — they can see the finish line!",
        "\"Hold on! Hold on!\" — the whole stadium willing them through!",
    };

    // Familiar, recognisable chants by nation (3-letter FIFA codes). Unknown codes fall back to a
    // generic chant, so the model degrades gracefully for every team.
    private static readonly Dictionary<string, string[]> Nation = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BRA"] = new[] { "Samba drums rolling — \"Brasil! Brasil!\"", "A yellow-and-green carnival in full swing!", "\"Eu sou brasileiro, com muito orgulho!\"", "Surdo drums and a thousand whistles — pure Brazil!", "\"Olé, olé, olé, olé — Brasi-il, Brasi-il!\"", "The samba beat is relentless — this is a carnival, not a match!", "\"Canarinho! Canarinho!\" — the whole stadium bouncing!" },
        ["ARG"] = new[] { "\"Vamos, vamos, Argentina!\"", "\"Muchachos, ahora nos volvimos a ilusionar!\" — the whole end bouncing!", "A sky-blue-and-white wall jumping in unison!", "\"Olé, olé, olé — Diego! Diego!\"", "\"Argentina, Argentina!\" rolling around the bowl!", "\"Vamos, vamos, la scaloneta!\" — the new anthem ringing out!", "The Albiceleste faithful in rapturous full voice!" },
        ["MEX"] = new[] { "\"Cielito lindo — ay, ay, ay, ay — canta y no llores!\"", "\"¡Méxi-co! ¡Méxi-co!\"", "\"¡Sí se puede! ¡Sí se puede!\"", "A green tide and a deafening roar of sombreros and song!", "\"Ánimo, ánimo, México!\" sweeping the stands!", "Mariachi horns and a relentless drumbeat from the green end!", "\"México lindo y querido!\" echoing off every surface!" },
        ["USA"] = new[] { "\"U-S-A! U-S-A!\"", "\"I believe that we will win!\" — over and over!", "Stars and stripes everywhere, a thunderous chant!", "\"Stand up for the red, white and blue!\"", "\"U-S-A!\" bouncing in every corner of the stadium!", "\"Born in the USA!\" rolling out from the packed American end!", "\"Let's go USA!\" — clap, clap, clap-clap-clap!" },
        ["CAN"] = new[] { "\"We the North!\"", "\"Oh, Canada!\" ringing out in full voice!", "A wall of red maple leaves bouncing as one!", "\"Ca-na-da! Ca-na-da!\" — rising to a crescendo!", "Red-clad fans in fine voice — \"Go Canada Go!\"", "\"Oh, Canada, our home and native land!\" — every word belted out!" },
        ["ENG"] = new[] { "\"It's coming home, it's coming home!\"", "\"Sweet Caroline — bah, bah, bah!\" belted out!", "\"En-ger-land! En-ger-land!\"", "\"Three Lions on a shirt!\"", "\"God Save the King!\" given full throat by the English end!", "\"Football's coming home!\" — joyful, defiant, endless!", "\"Vindaloo! Na-na-na-na-na-na-na!\" in brilliant, daft voice!" },
        ["FRA"] = new[] { "\"Allez les Bleus! Allez les Bleus!\"", "La Marseillaise thundering around the ground!", "\"Et un, et deux, et trois — zéro!\"", "\"Aux armes!\" — the French faithful in passionate full voice!", "\"Allez!\" clapped out in steady, relentless rhythm!", "\"On va gagner! On va gagner!\" ringing around the bowl!" },
        ["GER"] = new[] { "\"Deutschland! Deutschland!\"", "\"Schland! Schland!\"", "A thunderous, drilled, rhythmic German roar!", "\"Auf geht's Deutschland, schieß ein Tor!\"", "\"Deutschland über alles\" — the anthem full-throated and proud!", "A precise, powerful drumbeat keeping thousands in perfect step!" },
        ["ESP"] = new[] { "\"¡A por ellos, oé!\"", "\"Yo soy español, español, español!\"", "\"¡España! ¡España!\"", "\"¡Campeones, campeones, oé, oé, oé!\"", "\"¡Vamos España!\" — a red-and-yellow wall at full volume!", "Flamenco claps and a passionate Spanish roar!" },
        ["POR"] = new[] { "\"Portugal! Portugal!\"", "\"Força Portugal!\"", "A red-and-green roar lifting the roof!", "\"Heeeei! Portugal!\" — the whole Lusitanian end in unison!", "\"Vamos, Portugal, vamos!\" — building and building!", "\"Força, força, força!\" beating out in a drilled clap!" },
        ["NED"] = new[] { "\"Hup Holland Hup!\"", "A sea of orange bouncing — \"Links, rechts!\"", "\"Hand in hand, kameraden!\"", "\"Wij houden van Oranje!\" filling the bowl!", "The Dutch end a wall of blazing orange, bouncing as one!", "\"Hol-land! Hol-land!\" — steady, loud, unstoppable!" },
        ["ITA"] = new[] { "\"Italia! Italia!\"", "\"Po-po-po-po-po-po!\" — the Seven Nation Army, Italian-style!", "A thunderclap of blue from the Curva!", "\"Azzurri! Azzurri!\" ringing out from the passionate Italian end!", "\"Campioni del mondo!\" — sung with total, unironic joy!", "Operatic harmonies floating above the Azzurri support!" },
        ["BEL"] = new[] { "\"Allez, allez, les Diables Rouges!\"", "A red wall in full voice!", "\"Belgique! Belgique!\"", "\"Allez la Belgique!\" — Dutch, French and German voices as one!", "\"Les Diables!\" chanted in rhythmic, proud unison!" },
        ["CRO"] = new[] { "A chequered red-and-white wall — \"U boj, u boj!\"", "\"Hrvatska! Hrvatska!\"", "\"Lijepa naša domovino!\" — the Croatian anthem belted out!", "The chequered flags creating a dazzling mosaic in full voice!", "\"Vatreni! Vatreni!\" — the Blazers getting their full-throated send-off!" },
        ["URU"] = new[] { "\"Soy celeste!\"", "\"¡Uru-guay! ¡Uru-guay!\"", "\"Garra Charrúa! Garra Charrúa!\"", "\"La celeste olímpica!\" — Uruguay's pride on full display!", "Light-blue scarves held aloft — \"¡Uruguay campeón!\"" },
        ["COL"] = new[] { "\"¡Colombia! ¡Colombia!\"", "Salsa rhythms and a sea of yellow swaying!", "\"¡Vamos, Colombia, vamos!\"", "\"Colombia es pasión!\" — the whole yellow-and-red end singing!", "Cumbia drumbeats pulsing through the Colombian support!" },
        ["JPN"] = new[] { "The Samurai Blue ultras and taiko drums in perfect rhythm!", "\"Nippon! Nippon!\" — drilled and relentless!", "A perfectly choreographed blue-and-white flag display in perfect silence, then — BOOM!", "\"Ganbare Nippon!\" — the Japanese fans as precise as a Swiss watch!", "Taiko drums counting out the beat — the whole end locked in perfect unison!", "\"Samurai Blue!\" chanted in crisp, rhythmic cadence!" },
        ["KOR"] = new[] { "\"Dae~han-min-guk!\" — clap, clap, clap-clap-clap!", "The Red Devils in full, organised voice!", "\"Oh, pil-seung Korea!\" — a red tsunami!", "A wall of red-devil outfits and the noise to match!", "\"Korea! Korea!\" — precise, passionate, impossibly loud!", "The clap-and-chant sequence rolling around the entire stadium!" },
        ["MAR"] = new[] { "A deafening, ceaseless Moroccan ultras roar — \"Sir! Sir!\"", "\"Dima Maghrib!\" booming around the ground!", "Davul drums and Berber rhythms — the Atlas Lions in full cry!", "\"Al-Maghrib! Al-Maghrib!\" — a green wave that never stops!", "\"Yallah, Yallah, Maghrib!\" — the whole end bouncing as one!", "A riot of green flags and an ear-splitting roar — Morocco in the house!" },
        ["SEN"] = new[] { "The 12th Gaïndé — drums, horns and a wall of green!", "\"Sénégal! Sénégal!\"", "\"Allez les Lions!\" — the Senegalese fans in magnificent voice!", "Sabar drums keeping a relentless rhythm in the Senegal end!", "\"Lions de la Téranga!\" — rolling around the stadium in delight!" },
        ["AUS"] = new[] { "\"Aussie! Aussie! Aussie! Oi! Oi! Oi!\"", "A gold-and-green roar — \"Socceroos!\"", "\"C'mon Aussie, c'mon!\" — the whole green-and-gold end singing!", "\"Advance Australia Fair!\" — every word known, every word sung!", "\"Aussie! Aussie! Aussie!\" — call and response bouncing around!" },
        ["SCO"] = new[] { "The Tartan Army in full cry — \"Flower of Scotland!\"", "\"Yes Sir, I Can Boogie!\" belted out joyously!", "\"Sce-a-tland! Sce-a-tland!\" — the kilts and drums in full swing!", "\"We'll support you evermore!\" — the Tartan Army never silent!", "\"Scotland the Brave\" drifting hauntingly across the bowl!" },
        ["DEN"] = new[] { "A red-and-white Roligan roar!", "\"Vi er røde, vi er hvide!\"", "\"Dan-mark! Dan-mark!\" — steady, cheerful, relentless!", "The Roligans are perfectly happy whether winning or not — and loudly so!", "\"Der er et yndigt land!\" — the Danish faithful in full throat!" },
        ["POL"] = new[] { "\"Polska, biało-czerwoni!\"", "A red-and-white wall booming out!", "\"Polska, goooo!\" — rolling around the bowl!", "\"Mazurek Dąbrowskiego!\" — the Polish anthem belted to the skies!", "\"Biało-czerwoni!\" chanted with pride and passion!" },
        ["SUI"] = new[] { "\"Hopp Schwiiz!\"", "Cowbells and a red wall in full voice!", "\"Allez la Suisse!\" — the Swiss support in three languages and one giant noise!", "\"Schweiz! Schweiz!\" ringing alongside the cowbells!", "A cheerful alpine roar — cowbells clanging in joyful chaos!" },
        ["CIV"] = new[] { "An orange wall of Ivorian drums and song!", "\"Côte d'Ivoire! Côte d'Ivoire!\"", "\"Éléphants! Éléphants!\" — the Ivory Coast end rocking!", "\"Allez les Éléphants!\" sung with irresistible West African rhythm!", "Djembe drums and horns — the Ivorian fans putting on a show!" },
        ["NGA"] = new[] { "A green-and-white Super Eagles roar with drums!", "\"Naija! Naija!\"", "\"Super Eagles! Super Eagles!\" — soaring around the bowl!", "\"Arise, O Compatriots!\" — the Nigerian anthem given full voice!", "Afrobeats rhythms drifting out of the packed Nigerian end!" },
        ["GHA"] = new[] { "Drums, horns and a Black Stars roar!", "\"Ghana! Ghana!\"", "\"Black Stars! Black Stars!\" — the whole end jumping!", "\"God Bless Our Homeland Ghana!\" — the anthem in proud full voice!", "High-life rhythms and a yellow-and-red-and-green sea of flags!" },
        ["ECU"] = new[] { "\"¡Ecua-dor! ¡Ecua-dor!\"", "A yellow tide in full song!", "\"¡Vamos, La Tri! ¡Vamos!\"", "\"¡Mi lindo Ecuador!\" rolling around the stands!", "Tricolour flags waving and a passionate Ecuadorian roar!" },
        ["SRB"] = new[] { "\"Srbija! Srbija!\"", "A thunderous red-and-white roar!", "\"\"Orlovi! Orlovi!\" — the Eagles getting a full-throated send-off!", "\"Beli Orlovi!\" chanted with Serbian pride and fervour!" },
        ["CMR"] = new[] { "\"Indomptables! Indomptables!\" — the Indomitable Lions in full voice!", "\"Cameroun! Cameroun!\" — a green-and-red roar!", "Makossa rhythms and the drumbeat of a Cameroonian carnival!", "\"Allez les Lions Indomptables!\" ringing around the ground!" },
        ["TUN"] = new[] { "\"Tûnis! Tûnis!\" — a red-and-white Tunisian roar!", "\"Allez les Aigles de Carthage!\" — the Eagles of Carthage calling!", "\"Tûnis, Tûnis — watan!\" bouncing around the stands!" },
        ["EGY"] = new[] { "\"Masr! Masr!\" — Egypt in passionate full voice!", "\"Hya! Hya! Hya! Yalla!\" — the Pharaohs' fans in full cry!", "\"Bilady, Bilady!\" — the Egyptian anthem at incredible volume!", "A wall of red and white — the Pharaohs faithful never quiet!" },
        ["ALG"] = new[] { "\"Jazair! Jazair!\" — Algeria's fans filling every corner!", "\"Les Fennecs! Les Fennecs!\" — the Desert Foxes cheered on!", "\"One, Two, Three — Vive l'Algérie!\"", "Green-and-white flags and a roar from the passionate Algerian end!" },
        ["SAU"] = new[] { "\"Al-Akhdhar! Al-Akhdhar!\" — the Saudis in green full voice!", "\"Saudi Arabia! Saudi Arabia!\" — chanted with national pride!", "\"Yallah Akhdhar!\" — the Green Falcons urged on!" },
        ["IRN"] = new[] { "\"Iran! Iran!\" — a deafening red-white-and-green roar!", "\"Melli! Melli!\" — the whole Iranian end jumping!", "\"Yallah, Yallah, Iran!\" — bouncing in full voice!" },
        ["IRQ"] = new[] { "\"Iraq! Iraq!\" — the Lions of Mesopotamia cheered on!", "\"Yallah, Yallah Asood!\" — passionate and relentless!" },
        ["QAT"] = new[] { "\"Qatar! Qatar!\" — the maroon wall in full voice!", "\"Yallah, Yallah, Al-Annabi!\" — the Burgundy Hearts singing!", "A sea of maroon and white — \"Fawz Al-Annabi!\"" },
        ["PAN"] = new[] { "\"Pa-na-má! Pa-na-má!\" — the whole Panamanian end bouncing!", "\"Arriba Panamá!\" — a red, white and blue roar!", "\"Los Canaleros!\" chanted with enormous Caribbean flair!" },
        ["JAM"] = new[] { "Reggae rhythms and a \"Reggae Boyz!\" chant rolling around!", "\"Jamaica! Jamaica!\" — gold, black and green in full voice!", "\"One Love, Reggae Boyz!\" — a joyful, irresistible roar!" },
        ["TRI"] = new[] { "\"Trinidad and Tobago! Trinidad and Tobago!\"", "\"Soca Warriors! Soca Warriors!\" — a carnival in the stands!", "Soca beats and a joyful red-white-and-black roar!" },
        ["CRC"] = new[] { "\"¡Ticos! ¡Ticos!\" — Costa Rica cheered to the heavens!", "\"¡La Sele! ¡La Sele!\" — the whole red end bouncing!", "\"¡Costa Rica! ¡Costa Rica!\" — pure, joyful Central American passion!" },
        ["HON"] = new[] { "\"¡Honduras! ¡Honduras!\" — blue-and-white bouncing!", "\"¡Los Catrachos! ¡Los Catrachos!\" — filling the stadium!" },
        ["SLV"] = new[] { "\"¡El Salvador! ¡El Salvador!\"", "\"¡Cuzcatlán! ¡Cuzcatlán!\" — the whole Salvadorean end singing!" },
        ["BOL"] = new[] { "\"¡Bo-li-via! ¡Bo-li-via!\" — green-and-red waving!", "\"¡La Verde! ¡La Verde!\" — the passionate Bolivian roar!" },
        ["PER"] = new[] { "\"¡Arriba Perú! ¡Arriba Perú!\"", "\"Contigo Perú!\" — the whole Blanquirroja end in magnificent voice!", "\"¡Pe-rú! ¡Pe-rú!\" — a white-and-red roar that doesn't stop!" },
        ["CHL"] = new[] { "\"¡Chi, Chi, Chi! ¡Le, Le, Le! ¡Viva Chile!\"", "\"¡La Roja! ¡La Roja!\" — the red wall bouncing!", "\"¡Fuerza Chile!\" chanted with incredible fervour!" },
        ["PAR"] = new[] { "\"¡Paraguay! ¡Paraguay!\" — red-and-white flags twirling!", "\"¡Albirroja! ¡Albirroja!\" — the whole Paraguayan end in song!" },
        ["VEN"] = new[] { "\"¡Venezuela! ¡Venezuela!\" — the Vinotinto faithful in full cry!", "\"¡Vinotinto! ¡Vinotinto!\" — singing with fierce pride!" },
        ["SWE"] = new[] { "\"Sverige! Sverige!\" — yellow-and-blue bouncing in the stands!", "\"Du gamla, du fria\" — the Swedish anthem sung with feeling!", "\"Heja Sverige! Heja!\" — the whole Swedish end roaring!" },
        ["NOR"] = new[] { "\"Nor-ge! Nor-ge!\" — the red-and-blue end in full voice!", "\"Norway! Norway!\" — a Scandinavian roar rolling around the bowl!", "\"Ja, vi elsker dette landet!\" — the Norwegian anthem full-throated!" },
        ["FIN"] = new[] { "\"Suomi! Suomi!\" — the Huuhkajat faithful cheering!", "\"Finland! Finland!\" — a blue-and-white roar!", "\"Hei, hei, Suomi peli!\" — bouncing in the stands!" },
        ["AUT"] = new[] { "\"Öster-reich! Öster-reich!\" — red-and-white bouncing!", "\"Wir sind Österreich!\" — the Austrian end in full voice!", "\"Allez, allez, Team Austria!\" ringing around the ground!" },
        ["CZE"] = new[] { "\"Česko! Česko!\" — a red-white-and-blue roar!", "\"Pojď, pojď, reprezentace!\" — the whole Czech end urging them on!", "\"Hej, Slavané!\" — the Czech faithful in brilliant voice!" },
        ["SVK"] = new[] { "\"Slovensko! Slovensko!\" — the Falcons cheered on!", "\"Hej, Slováci!\" — a passionate and proud chant!" },
        ["HUN"] = new[] { "\"Hajrá, Magyarország! Hajrá!\"", "\"Ma-gyar-or-szág!\" — red-white-and-green bouncing!", "\"Nem, nem, soha!\" — defiant and full-throated!" },
        ["ROU"] = new[] { "\"Ro-mâ-nia! Ro-mâ-nia!\" — a yellow-blue-and-red roar!", "\"Hai, România!\" — the whole Romanian end swaying!", "\"Tricolorul!\" chanted with enormous Romanian pride!" },
        ["BUL"] = new[] { "\"Бъл-га-рия! Бъл-га-рия!\" — white-and-green bouncing!", "\"Haide, Balgaria!\" — the passionate Bulgarian roar!" },
        ["GRE"] = new[] { "\"Hellas! Hellas!\" — the whole blue-and-white end bouncing!", "\"Ell-á-da! Ell-á-da!\" — a thunderous Greek roar!", "\"Ymno eis tin Eleftherian!\" — the anthem in proud, passionate voice!" },
        ["TUR"] = new[] { "\"Türkiye! Türkiye!\" — the red crescent end in full voice!", "\"Ay yıldızlı al bayrak!\" — the Turkish faithful in patriotic song!", "\"Hadi! Hadi! Türkiye!\" — relentless, passionate noise!" },
        ["UKR"] = new[] { "\"Ukraina! Ukraina!\" — blue-and-yellow bouncing with emotion!", "\"Slava Ukraini!\" — the crowd roaring with pride and heart!", "A blue-and-yellow wall singing with incredible fervour!" },
        ["SCT"] = new[] { "\"Scots, wha hae!\" — the Tartan Army in full historic cry!" },
        ["ALB"] = new[] { "\"Shqipëri! Shqipëri!\" — the red-and-black eagle end roaring!", "\"Kuq e zi!\" — red and black flags filling the air!" },
        ["SLO"] = new[] { "\"Slo-ve-ni-ja! Slo-ve-ni-ja!\" — the Dragons cheered on!", "\"Slovenija, od zdaj naprej!\" — passionate and proud!" },
        ["MKD"] = new[] { "\"Ma-ke-do-ni-ja!\" — the whole end bouncing with pride!", "\"Boj se boj!\" — defiant North Macedonian passion!" },
        ["ISR"] = new[] { "\"Israel! Israel!\" — blue-and-white flags all around!", "\"Hatikvah!\" — the Israeli anthem sung with deep emotion!" },
        ["CHN"] = new[] { "\"Zhōngguó! Zhōngguó!\" — a red wall at full volume!", "\"Jiā yóu!\" — the whole Chinese end urging them forward!", "\"Qǐlái!\" — the Chinese faithful in passionate full voice!" },
        ["THA"] = new[] { "\"Tai-land! Tai-land!\" — a sea of gold and red!", "\"Go Thailand! Go Thailand!\" — the whole end in fine voice!" },
        ["VIE"] = new[] { "\"Việt Nam! Việt Nam!\" — the golden stars bouncing!", "\"Đội tuyển Việt Nam vô địch!\" — the whole end singing!" },
        ["IDN"] = new[] { "\"Indonesia! Indonesia!\" — the Garuda faithful in tremendous voice!", "\"Garuda! Garuda!\" — red-and-white bouncing!" },
        ["PHL"] = new[] { "\"Pinas! Pinas!\" — a blue-red-and-white roar!", "\"Pilipinas! Pilipinas!\" — the Azkals cheered on!" },
        ["IND"] = new[] { "\"India! India!\" — blue-and-orange bouncing!", "\"Jai Hind!\" — the whole Indian end singing!" },
        ["PAK"] = new[] { "\"Pakistan! Pakistan!\" — green-and-white in full voice!", "\"Jeetay ga! Jeetay ga! Pakistan!\"" },
        ["ZAM"] = new[] { "\"Zambia! Zambia!\" — the copper-bullet chant rolling!", "\"Chipolopolo!\" — the Copper Bullets cheered on!" },
        ["ZIM"] = new[] { "\"Zimbabwe! Zimbabwe!\" — green-and-yellow in full voice!", "\"Warriors! Warriors!\" — the whole Zimbabwe end singing!" },
        ["BOT"] = new[] { "\"Botswana! Botswana!\" — the Zebras cheered on!", "\"Zebras! Zebras!\" — a proud African roar!" },
        ["TAN"] = new[] { "\"Tanzania! Tanzania!\" — Taifa Stars faithful in voice!", "\"Taifa Stars!\" — the whole end bouncing!" },
        ["RWA"] = new[] { "\"Rwanda! Rwanda!\" — the Amavubi roar!", "\"Amavubi! Amavubi!\" — the Wasps cheered on!" },
        ["BFA"] = new[] { "\"Burkina Faso! Burkina Faso!\" — les Étalons cheered on!", "\"Étalons! Étalons!\" — the passionate Burkinabé roar!" },
        ["MLI"] = new[] { "\"Mali! Mali!\" — the Eagles of Mali in brilliant song!", "\"Aiglons! Aiglons!\" — a green-and-yellow roar!" },
        ["GAB"] = new[] { "\"Gabon! Gabon!\" — the Panthers cheered on!", "\"Panthères! Panthères!\" — full-throated Central African pride!" },
        ["CGO"] = new[] { "\"Congo! Congo!\" — the Red Devils of the Congo roar!", "\"Diables Rouges!\" — in joyful full voice!" },
        ["KEN"] = new[] { "\"Kenya! Kenya!\" — the Harambee Stars faithful in voice!", "\"Harambee Stars!\" — the whole Kenyan end roaring!" },
        ["ETH"] = new[] { "\"Ethiopia! Ethiopia!\" — Waliya Antelopes in song!", "\"Waliya! Waliya!\" — the whole Ethiopian end bouncing!" },
        ["LBY"] = new[] { "\"Libya! Libya!\" — the Mediterranean Knights in voice!" },
        ["SDN"] = new[] { "\"Sudan! Sudan!\" — the Nile Crocodiles cheered on!" },
        ["MRT"] = new[] { "\"Mauritania! Mauritania!\" — the Mourabitounes in full cry!" },
        ["NZL"] = new[] { "\"All Whites! All Whites!\" — the New Zealand faithful in voice!", "\"New Zealand! New Zealand!\" — a proud Pacific roar!" },
        ["FIJ"] = new[] { "\"Fiji! Fiji!\" — a Pacific island roar of pure joy!", "\"Bula! Bula! Fiji!\" — the whole Fijian end singing!" },
        ["PNG"] = new[] { "\"Papua New Guinea!\" — the Pukpuks cheered on!" },
        ["CUB"] = new[] { "\"Cuba! Cuba!\" — blue-and-white bouncing in the stands!", "\"¡Viva Cuba!\" — the passionate Caribbean roar!" },
        ["GUA"] = new[] { "\"¡Guatemala! ¡Guatemala!\" — the Quetzales faithful roar!", "\"¡Chapines! ¡Chapines!\" — the whole end in fine voice!" },
        ["NCA"] = new[] { "\"¡Nicaragua! ¡Nicaragua!\" — a passionate Central American roar!" },
        ["HAI"] = new[] { "\"Haiti! Haiti!\" — the Grenadiers in full voice!", "\"Les Grenadiers!\" — the passionate Haitian end sings!" },
        ["DOM"] = new[] { "\"República Dominicana!\" — the whole end bouncing with pride!" },
        ["GUY"] = new[] { "\"Guyana! Guyana!\" — the Golden Jaguars cheered on!" },
        ["SUR"] = new[] { "\"Suriname! Suriname!\" — a proud Surinamese roar!" },
        ["BIH"] = new[] { "\"Bosna! Bosna!\" — the Dragons in passionate full voice!", "\"Haj'mo Zmajevi!\" — \"Come on Dragons!\" rolling around the bowl!" },
        ["MNE"] = new[] { "\"Crna Gora!\" — Montenegro's faithful in full voice!" },
        ["MDA"] = new[] { "\"Moldova! Moldova!\" — the whole end cheering on their team!" },
        ["BLR"] = new[] { "\"Belarus! Belarus!\" — red-and-green bouncing!" },
        ["LVA"] = new[] { "\"Latvija! Latvija!\" — the whole Latvian end in voice!" },
        ["LTU"] = new[] { "\"Lietuva! Lietuva!\" — yellow-green-and-red bouncing!" },
        ["EST"] = new[] { "\"Eesti! Eesti!\" — the Estonian fans in full voice!" },
        ["GEO"] = new[] { "\"Sakartvelo! Sakartvelo!\" — the whole Georgian end roaring!", "\"Go Georgia!\" — chanted with enormous pride and passion!" },
        ["ARM"] = new[] { "\"Hayastan! Hayastan!\" — red-blue-and-orange bouncing!", "\"Haj Armenia!\" — the whole end in passionate full voice!" },
        ["AZE"] = new[] { "\"Azərbaycan! Azərbaycan!\" — the whole end cheering!" },
        ["KAZ"] = new[] { "\"Qazaqstan! Qazaqstan!\" — the Steppe Eagles cheered on!" },
        ["UZB"] = new[] { "\"Uzbekiston! Uzbekiston!\" — the White Wolves in full cry!" },
        ["MNG"] = new[] { "\"Mongol! Mongol!\" — the Mongolian faithful in voice!" },
        ["BRN"] = new[] { "\"Bahrain! Bahrain!\" — a passionate Gulf roar!" },
        ["KWT"] = new[] { "\"Kuwait! Kuwait!\" — the Blue Brigade cheered on!" },
        ["OMN"] = new[] { "\"Oman! Oman!\" — a Gulf roar from the passionate fans!" },
        ["YEM"] = new[] { "\"Yemen! Yemen!\" — the whole end cheering their team!" },
        ["JOR"] = new[] { "\"Al-Urdun! Al-Urdun!\" — the Naseem Al-Jabal faithful in voice!" },
        ["LBN"] = new[] { "\"Lubnân! Lubnân!\" — the Lebanese Cedar's fans in full voice!" },
        ["SYR"] = new[] { "\"Suria! Suria!\" — the whole Syrian end bouncing!" },
        ["PLE"] = new[] { "\"Falastin! Falastin!\" — the Palestinian fans in passionate song!" },
        ["CYP"] = new[] { "\"Kypros! Kypros!\" — the Moufflons cheered on!" },
        ["LIE"] = new[] { "\"Liechtenstein!\" — the tiny nation in outsized full voice!" },
        ["SMR"] = new[] { "\"San Marino! San Marino!\" — the Sammarinese faithful roar!" },
        ["AND"] = new[] { "\"Andorra! Andorra!\" — small nation, enormous passion!" },
        ["MLT"] = new[] { "\"Malta! Malta!\" — the Maltese in full-throated support!" },
        ["ISL"] = new[] { "The Viking clap! \"Hú!\" — the Icelandic thunderclap rolls around!", "\"Strákarnir okkar!\" — Iceland's faithful in magnificent voice!", "\"Ísland! Ísland!\" — followed by that thunderous Viking clap!" },
        ["IRL"] = new[] { "\"Come on you Boys in Green!\"", "\"Ole, ole, ole, ole — Ireland, Ireland!\"", "\"You'll Never Beat the Irish!\" — a green wall in brilliant voice!", "\"Amhrán na bhFiann!\" — the Irish anthem with emotion and full voice!" },
        ["WAL"] = new[] { "\"Gwnewch y pethau bychain!\" — and they absolutely do!", "\"Don't Take Me Home!\" — Wales bouncing in joyful defiance!", "\"Wal-es! Wal-es!\" — red dragons and a magnificent roar!", "\"Land of My Fathers!\" sung with genuine, heartfelt passion!" },
        ["NIR"] = new[] { "\"Green and White Army!\" — Northern Ireland in full voice!", "\"Will Grigg's on fire!\" — a joyful, irreverent roar!", "\"Sweet Caroline!\" — shared gleefully with the wider island!" },
        ["LUX"] = new[] { "\"Mir wëlle bleiwe wat mir sin!\" — Luxembourg's anthem ringing out!", "\"Lëtzebuerg! Lëtzebuerg!\" — the Red Lions cheered on!" },
        ["FRO"] = new[] { "\"Føroyar! Føroyar!\" — the Faroe Islands faithful in magnificent voice!" },
        ["GIB"] = new[] { "\"Gibraltar! Gibraltar!\" — the Rock in full, proud voice!" },
        ["KSV"] = new[] { "\"Kosova! Kosova!\" — the whole end bouncing with pride!" },
        ["RUS"] = new[] { "\"Ros-si-ya! Ros-si-ya!\" — a red-white-and-blue wave!", "\"Vpered, Rossiya!\" — the whole Russian end in full voice!" },
        ["SVN"] = new[] { "\"Slovenija! Slovenija!\" — the whole Slovenian end in fine voice!", "\"Bravo! Bravo!\" — appreciative and passionate Slovenian support!" },
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

    /// <summary>The crowd Ole-ing every pass during a possession masterclass.</summary>
    public static string OlePassingSpell(ref Xoshiro256 rng) => Pick(OlePassingSpellBank, ref rng);

    /// <summary>Acclaiming a great goalkeeping save.</summary>
    public static string SaveOvation(ref Xoshiro256 rng) => Pick(SaveOvationBank, ref rng);

    /// <summary>Playful, good-natured taunting of the opposition end.</summary>
    public static string TauntingOpposition(ref Xoshiro256 rng) => Pick(TauntingOppositionBank, ref rng);

    /// <summary>Tense, defiant support while the team tries to see out a lead.</summary>
    public static string DefendingLead(ref Xoshiro256 rng) => Pick(DefendingLeadBank, ref rng);

    /// <summary>A familiar nation-specific chant for the given team code, or a generic chant if unknown.</summary>
    public static string NationChant(string teamCode, ref Xoshiro256 rng)
    {
        return Nation.TryGetValue(teamCode, out var bank) ? Pick(bank, ref rng) : GenericChant(ref rng);
    }
}
