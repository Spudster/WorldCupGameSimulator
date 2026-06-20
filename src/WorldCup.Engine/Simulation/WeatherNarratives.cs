using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Match-day weather flavour: a short descriptive note for the conditions readout, a richer pre-match
/// mention for the commentary, and an icon/label for the styled report. Purely atmospheric — it never
/// changes the simulated event stream.
/// </summary>
public static class WeatherNarratives
{
    private static string Pick(string[] bank, ref Xoshiro256 rng) => bank[rng.NextInt(bank.Length)];

    /// <summary>One descriptive line for the conditions readout (e.g. "A swirling wind off the stands").</summary>
    public static string Note(WeatherKind kind, ref Xoshiro256 rng) => Pick(Notes(kind), ref rng);

    /// <summary>A pre-match commentary mention of the conditions.</summary>
    public static string Mention(WeatherKind kind, ref Xoshiro256 rng) => Pick(Mentions(kind), ref rng);

    /// <summary>An emoji + short label for the conditions card / box score.</summary>
    public static (string Icon, string Label) Badge(WeatherKind kind) => kind switch
    {
        WeatherKind.Clear => ("🌙", "Clear"),
        WeatherKind.Sunny => ("☀", "Sunny"),
        WeatherKind.Overcast => ("☁", "Overcast"),
        WeatherKind.Breezy => ("🍃", "Breezy"),
        WeatherKind.Windy => ("🌬", "Windy"),
        WeatherKind.LightRain => ("🌦", "Light rain"),
        WeatherKind.HeavyRain => ("🌧", "Heavy rain"),
        WeatherKind.Humid => ("🥵", "Humid"),
        WeatherKind.Cold => ("❄", "Cold"),
        _ => ("🌡", "Fair"),
    };

    private static string[] Notes(WeatherKind kind) => kind switch
    {
        WeatherKind.Clear => ClearNotes,
        WeatherKind.Sunny => SunnyNotes,
        WeatherKind.Overcast => OvercastNotes,
        WeatherKind.Breezy => BreezyNotes,
        WeatherKind.Windy => WindyNotes,
        WeatherKind.LightRain => LightRainNotes,
        WeatherKind.HeavyRain => HeavyRainNotes,
        WeatherKind.Humid => HumidNotes,
        WeatherKind.Cold => ColdNotes,
        _ => ClearNotes,
    };

    private static string[] Mentions(WeatherKind kind) => kind switch
    {
        WeatherKind.Clear => ClearMentions,
        WeatherKind.Sunny => SunnyMentions,
        WeatherKind.Overcast => OvercastMentions,
        WeatherKind.Breezy => BreezyMentions,
        WeatherKind.Windy => WindyMentions,
        WeatherKind.LightRain => LightRainMentions,
        WeatherKind.HeavyRain => HeavyRainMentions,
        WeatherKind.Humid => HumidMentions,
        WeatherKind.Cold => ColdMentions,
        _ => ClearMentions,
    };

    private static readonly string[] ClearNotes =
    {
        "A clear, still evening — perfect for football", "Not a breath of wind under the lights",
        "Crisp and clear, the flags hanging limp", "A calm, settled night for it",
        "Stars overhead and a pitch in pristine nick", "Serene conditions under a clear sky",
        "Floodlights glowing against an ink-black sky", "A still, settled evening — ideal for flowing football",
        "Not a cloud to be seen, the air perfectly still", "Calm and composed conditions out there tonight",
    };
    private static readonly string[] ClearMentions =
    {
        "It's a clear, still night here — pristine conditions, no excuses for anyone.",
        "Barely a breath of wind under the floodlights — a pure test of football.",
        "Beautiful conditions, the surface gleaming — this should be a good one.",
        "A wonderful, settled evening for it — both sides will have nothing to blame but themselves.",
        "Crystal-clear skies and a pitch that looks immaculate — the stage is perfectly set.",
        "No weather problems to report — the ball will run true and the players have every chance to express themselves.",
        "Calm and clear under the floodlights — the kind of evening when the quality usually tells.",
    };

    private static readonly string[] SunnyNotes =
    {
        "Bright sunshine and a quick surface", "Glorious sunshine bathing the pitch",
        "Sun high overhead, the pitch lightning-quick", "A brilliant blue sky over the stadium",
        "Dazzling sunshine, the pitch gleaming", "A scorching, sun-drenched afternoon",
        "The sun blazing down on an immaculate surface", "Wall-to-wall sunshine, the grass shimmering",
        "Not a cloud in sight, the sun relentless overhead", "Full, blazing sun — this pitch is electric underfoot",
    };
    private static readonly string[] SunnyMentions =
    {
        "Brilliant sunshine here, the pitch slick and fast — expect the ball to fly.",
        "Not a cloud in the sky — the keepers will have one eye on that low sun.",
        "Glorious conditions, the surface watered and rapid underfoot.",
        "The sun is blazing down — the players will be glad of every drinks break going.",
        "Dazzling sunshine today, and the ball is pinging across that surface beautifully.",
        "Beautiful, sunny conditions — but that glare at the far end will be a real concern for the goalkeeper.",
        "A gorgeous day for it, the pitch looking a picture — conditions couldn't really be much better.",
    };

    private static readonly string[] OvercastNotes =
    {
        "Grey and overcast, heavy air", "A blanket of cloud over the ground",
        "Muggy and grey, no sun to speak of", "Leaden skies overhead",
        "Thick cloud cover, a dull and close evening", "An overcast sky pressing down on the stadium",
        "Flat, grey light under the cloud — the floodlights earning their keep",
        "Dull and murky, not a patch of blue to be found", "A grey, brooding sky overhead",
    };
    private static readonly string[] OvercastMentions =
    {
        "Grey skies overhead, the air a little heavy — but good conditions for running.",
        "Overcast and close — the kind of evening that can sap the legs late on.",
        "A flat, grey sky above the stadium — not glamorous, but the pitch is in excellent shape.",
        "No sun to worry about, just thick cloud — the players can get on with the football without any glare issues.",
        "Dull overhead, but the conditions underfoot are perfectly fine — plenty of football to be played here.",
        "Close and overcast — those grey skies have a way of making ninety minutes feel like a hundred and ten.",
    };

    private static readonly string[] BreezyNotes =
    {
        "A gentle breeze rolling across the pitch", "Just a breath of wind in off the corner",
        "A light breeze toying with the corner flags", "A soft wind, nothing the players can't handle",
        "A pleasant little breeze keeping things fresh", "A mild wind drifting across from the open end",
        "The flags barely stirring in a soft, welcome breeze", "A refreshing breeze circling the stadium",
        "Light airs — barely noticeable unless you're taking a free-kick",
    };
    private static readonly string[] BreezyMentions =
    {
        "There's a gentle breeze rolling through — enough to give the dead-ball specialists something to think about.",
        "A light wind here, the corner flags fluttering — a factor on the high balls but no more.",
        "Just a pleasant breeze tonight — welcome in this heat, and not enough to trouble the ball unduly.",
        "A soft wind across the pitch — the set-piece takers will want to check which way it's blowing.",
        "Mild and breezy out there — comfortable conditions overall, with just a little movement on the ball from dead situations.",
        "A gentle wind circling the ground — nothing dramatic, but the corners and long throws will need a second look.",
    };

    private static readonly string[] WindyNotes =
    {
        "A swirling wind off the stands", "A gusting, blustery wind down the pitch",
        "A strong wind whipping across the surface", "Blustery — the wind playing havoc with the long balls",
        "A ferocious gust rattling the corner flags", "The wind howling through the open end of the stadium",
        "A powerful crosswind tearing across the pitch", "Gusty and unpredictable — the ball barely obeys the laws of physics",
        "A fierce wind making every high ball a lottery", "A strong end-to-end wind — a significant advantage for whoever has it at their backs",
    };
    private static readonly string[] WindyMentions =
    {
        "A real swirling wind in this stadium — it's already playing havoc with the goal-kicks and the crosses.",
        "Blustery conditions, the wind gusting straight down the pitch — the keepers will hate this.",
        "The flags are standing straight out — a strong wind that'll test the touch and the dead-ball deliveries all night.",
        "It's seriously windy out there — the coin toss to pick ends could be just as important as anything in the technical area.",
        "A ferocious gust keeps sweeping through — high balls will be utterly unpredictable, and the keepers know it.",
        "The wind is a genuine factor tonight — players will need to take a touch rather than hoping to play it first time.",
        "Gusting and blustery from the off — the team with the wind at their backs in the second half will fancy their chances.",
    };

    private static readonly string[] LightRainNotes =
    {
        "A light drizzle greasing the surface", "Spots of rain and a slick pitch",
        "A fine rain falling, the ball zipping", "Drizzle in the air, the surface quickening",
        "A misty drizzle drifting across the floodlights", "The merest hint of rain — enough to glist the surface",
        "A fine spray of rain barely visible under the lights", "Gentle rain beginning to settle on the pitch",
        "A damp, drizzly evening under a dark sky", "Light rain pattering down — the surface starting to liven up",
    };
    private static readonly string[] LightRainMentions =
    {
        "A light rain falling — and that'll only quicken the surface and zip the ball through.",
        "Just a drizzle here, but enough to grease the pitch — watch for the ball skidding on.",
        "Spots of rain under the lights — slick conditions, and the defenders won't enjoy turning on this.",
        "A gentle drizzle has started to fall — nothing heavy, but the surface is already beginning to slicken up.",
        "A fine, misty rain hanging in the air — the first-touch merchants will love it; the heavy-footed ones, less so.",
        "Light rain spitting down — that moisture on the grass will add a yard of pace to every ball that's played in behind.",
        "Just enough rain to make the surface treacherous at speed — the full-backs will want to be careful when turning sharply.",
    };

    private static readonly string[] HeavyRainNotes =
    {
        "Rain lashing down, pools forming", "A downpour, the pitch sodden",
        "Driving rain sweeping across the stadium", "Torrential rain, standing water in the corners",
        "A relentless downpour — the pitch barely coping", "Heavy rain hammering the roof of the stadium",
        "A monsoon-like deluge drenching the turf", "The pitch awash, the lines barely visible",
        "A violent rainstorm turning the surface into a bog", "Rain sheeting in sideways across the floodlights",
    };
    private static readonly string[] HeavyRainMentions =
    {
        "The rain is absolutely lashing down — the ball is holding up in the puddles, this could get scrappy.",
        "A genuine downpour here, the pitch already sodden — expect skids, splashes and the odd comical slip.",
        "Driving rain sweeping across the ground — the groundstaff will be praying it holds together.",
        "It is absolutely tipping it down — the ball is like a bar of soap out there, and no one's quite sure what it'll do next.",
        "A heavy, relentless downpour — the drainage is being seriously tested, and so are the players' tempers.",
        "The rain is hammering down and showing no signs of letting up — this is a real test of character as much as quality.",
        "Standing water is starting to appear out there — the referee will have to keep a close eye on conditions throughout.",
    };

    private static readonly string[] HumidNotes =
    {
        "Sweltering and humid, sapping air", "Thick, muggy heat hanging over the pitch",
        "Oppressive humidity, the players already glistening", "Heavy, humid air — energy-sapping stuff",
        "Stifling humidity — not a breath of relief anywhere", "The air thick and soupy, the pitch shimmering",
        "A furnace-like humidity bearing down on the stadium", "Sultry, energy-draining conditions underfoot",
        "Tropical heat and humidity pressing in on all sides", "The warm, close air drawing sweat before kick-off",
    };
    private static readonly string[] HumidMentions =
    {
        "It's thick, muggy and humid out there — energy-sapping conditions that'll tell in the legs late on.",
        "Oppressive humidity tonight — hydration is going to be everything in the closing stages.",
        "The air is heavy and close — you can see the players are already glistening before a ball's been kicked.",
        "Stifling conditions out there — the medical staff will be on high alert, and the squad depth will be thoroughly tested.",
        "The humidity is absolutely punishing — rotation and fitness could decide this more than tactics.",
        "It's a real slog of a night in terms of the conditions — that thick, soupy air will drain the legs long before ninety minutes is up.",
        "Tropical and utterly draining — both sets of players will be leaning on their fitness reserves more than usual tonight.",
    };

    private static readonly string[] ColdNotes =
    {
        "Bitterly cold, breath visible", "A bitter chill, frost in the air",
        "Cold and clear, the breath hanging", "A freezing night under the lights",
        "A sharp frost nipping at the players' fingers", "An icy chill in the air, the ground firm underfoot",
        "The cold biting — every breath a little cloud of steam", "Sub-zero feel, the pitch as hard as iron",
        "A glacial night, the cold settling in fast", "Freezing temperatures and a biting wind chill",
    };
    private static readonly string[] ColdMentions =
    {
        "It's bitterly cold here, you can see the breath of the players — one to get the blood pumping early.",
        "A real chill in the air tonight — the kind of cold that stings a mis-hit shot.",
        "The temperature has absolutely plummeted — the players will want to get on the ball quickly just to keep warm.",
        "A biting frost tonight — the pitch is holding up, but the keepers will be doing plenty of jumping on the spot.",
        "It is absolutely freezing out there — the supporters who made the journey deserve enormous credit just for being here.",
        "Cold, clear and unforgiving — the muscles will take time to warm up, and those first-ten-minutes injuries are a real risk.",
        "A night for the long-sleeved shirt and the thermal vest — sharp, unforgiving cold right across the stadium.",
    };
}
