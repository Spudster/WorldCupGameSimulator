namespace WorldCup.Engine.Simulation;

using WorldCup.Engine.Random;

/// <summary>
/// Discipline, penalties, substitutions, injuries and goalkeeping play-by-play
/// announcer phrases. Partial of <see cref="CommentaryPhrases"/>.
/// </summary>
public static partial class CommentaryPhrases
{
    private static readonly string[] PenaltiesAwarded =
    {
        "Penalty! The referee points straight to the spot.",
        "Penalty! No hesitation — the official points to the spot.",
        "It's a spot kick! The referee has no doubts whatsoever.",
        "Penalty given! The whistle blows and the finger points to the spot.",
        "The referee points to the spot — it's a penalty!",
        "Penalty to be taken! The official is absolutely certain.",
        "He's pointed to the spot! A penalty has been awarded.",
        "Spot kick! The referee was perfectly placed and gives the penalty.",
        "Penalty! The referee blows up and races to the spot.",
        "It's a penalty! The official points without a moment's pause.",
        "Penalty awarded! The referee marches over and points to the spot.",
        "The whistle goes — penalty! The finger jabs towards the spot.",
        "Penalty! The referee is sure of it and points to the spot.",
        "A penalty! The official signals immediately for the spot kick.",
        "Spot kick awarded! The referee saw it all and points.",
        "Penalty! The referee consults nobody — straight to the spot.",
        "It's a penalty kick! The official has made his decision instantly.",
        "Penalty! The arm goes up and the finger comes down to the spot.",
        "The referee gives it — penalty! He's pointing to the spot.",
        "Penalty! A clear and obvious decision for the official.",
        "Spot kick! The referee was right on top of it and points.",
        "Penalty! No need for a second look — he points to the spot.",
        "It's given! The referee signals a penalty to the spot.",
        "Penalty! The official has spotted the foul inside the box.",
        "The referee points to the spot — and it's a penalty kick!",
        "Penalty awarded! The whistle, then the finger to the spot.",
        "Spot kick! The referee is convinced and points straight away.",
        "Penalty! The official races in and points firmly to the spot.",
        "It's a penalty! The referee had a perfect view and points.",
        "Penalty! The finger is down — straight to the spot.",
        "The referee gives the penalty — he's pointing to the spot!",
        "Penalty! A decisive call, the official points to the spot.",
        "Spot kick awarded! The referee saw the contact and points.",
        "Penalty! The official is in no mood for protests — to the spot.",
        "It's a spot kick! The referee points and the box erupts.",
        "Penalty! The whistle shrieks and the finger finds the spot.",
        "The referee has given it — penalty, straight to the spot!",
    };

    /// <summary>Returns an exclamatory phrase announcing a penalty award.</summary>
    public static string PenaltyAwarded(ref Xoshiro256 rng)
    {
        return PenaltiesAwarded[rng.NextInt(PenaltiesAwarded.Length)];
    }

    private static readonly string[] PenaltiesScored =
    {
        "sends the keeper the wrong way — buried!",
        "tucks it away with ice in his veins!",
        "smashes it into the roof of the net!",
        "buries it low in the corner — no chance for the keeper!",
        "rolls it home with the keeper diving the other way!",
        "thumps it straight down the middle as the keeper dives!",
        "places it perfectly into the bottom corner!",
        "dispatches it with total composure!",
        "drills it past the keeper — emphatic!",
        "slots it home cool as you like!",
        "lashes it into the top corner — unstoppable!",
        "makes no mistake from twelve yards!",
        "sends the keeper one way and the ball the other!",
        "crashes it into the net — clinical finish!",
        "side-foots it into the corner with ease!",
        "blasts it high into the net — and it's in!",
        "keeps his nerve and finishes superbly!",
        "fizzes it past the keeper into the side netting!",
        "calmly strokes it into the corner!",
        "hammers it home — the keeper had no answer!",
        "dinks it right down the middle — audacious!",
        "rifles it into the bottom corner!",
        "converts with the utmost confidence!",
        "sends it into the net and the keeper the wrong way!",
        "powers it past the keeper's despairing dive!",
        "finds the corner with a beautifully struck penalty!",
        "tucks it into the bottom left — perfect!",
        "smashes it past the keeper without a flicker of doubt!",
        "guides it into the corner — clinical from the spot!",
        "buries it under the bar as the keeper goes early!",
        "sweeps it home with supreme assurance!",
        "thrashes it into the net — the keeper never moved in time!",
        "slams it past the keeper for the cleanest of finishes!",
        "rolls it into the empty corner — keeper sent the wrong way!",
        "lifts it into the roof of the net — what a penalty!",
        "strikes it true and the net ripples!",
        "makes it look easy from the spot!",
    };

    /// <summary>Returns a phrase describing a successfully converted penalty.</summary>
    public static string PenaltyScored(ref Xoshiro256 rng)
    {
        return PenaltiesScored[rng.NextInt(PenaltiesScored.Length)];
    }

    private static readonly string[] PenaltiesMissed =
    {
        "blazes it over the bar! what a miss!",
        "skies it high over the crossbar — dreadful!",
        "drags it horribly wide of the post!",
        "smashes it against the crossbar — and away!",
        "clatters it off the post — it stays out!",
        "scuffs it well wide — a shocking penalty!",
        "balloons it over the top — he'll be haunted by that!",
        "fires it wide of the upright — agony!",
        "slips as he strikes and screws it wide!",
        "hits the woodwork and the chance is gone!",
        "sends it sailing over — an awful effort!",
        "puts it wide of the far post — unbelievable!",
        "rattles the bar and it bounces clear!",
        "blasts it into the stands — what a waste!",
        "pulls it wide of the left-hand post!",
        "lifts it over the bar from twelve yards!",
        "thumps it off the post and away to safety!",
        "drives it miles over — he can't believe it!",
        "wraps his foot around it and drags it wide!",
        "fluffs his lines and sends it well over!",
        "cannons it off the crossbar — heartbreak!",
        "shanks it wide — a glorious chance squandered!",
        "hooks it past the post — that's a terrible miss!",
        "blazes over when he had to score!",
        "strikes the upright and the ball rebounds clear!",
        "sends it high and wide — a moment to forget!",
        "leans back and lashes it over the bar!",
        "places it too close and clips the post — out!",
        "gets under it and balloons the penalty over!",
        "misses the target completely — sliced wide!",
        "smacks the bar and watches it bounce away!",
        "puts his penalty into row Z!",
        "drives it against the post — so close, yet no goal!",
        "fires it wide and holds his head in his hands!",
        "completely mishits it and it trundles wide!",
        "sends the ball over the bar and into the crowd!",
        "hammers it onto the woodwork — the keeper's relieved!",
    };

    /// <summary>Returns a phrase describing a penalty missed off target or the woodwork.</summary>
    public static string PenaltyMissed(ref Xoshiro256 rng)
    {
        return PenaltiesMissed[rng.NextInt(PenaltiesMissed.Length)];
    }

    private static readonly string[] PenaltiesSaved =
    {
        "saved! the keeper guesses right!",
        "saved! a magnificent stop from the spot!",
        "the keeper dives low and pushes it away — saved!",
        "saved! he read it perfectly and got down well!",
        "what a save! the keeper denies him from twelve yards!",
        "the keeper flings himself to his right — and saves it!",
        "saved! strong hands keep the penalty out!",
        "the keeper guesses correctly and palms it clear!",
        "saved! a brilliant dive to his left!",
        "the keeper stands tall and blocks it — saved!",
        "saved! he stayed big and smothered the effort!",
        "the keeper springs across and tips it wide — saved!",
        "saved! a superb piece of goalkeeping from the spot!",
        "the keeper waits, then dives — and keeps it out!",
        "saved! down he goes and the penalty is repelled!",
        "what a stop! the keeper guesses right and parries it away!",
        "saved! the keeper got a firm hand to it!",
        "the keeper plunges to his right and claws it clear!",
        "saved! he picked the corner and got there!",
        "the keeper produces a stunning save from the spot!",
        "saved! a fingertip is enough to divert it!",
        "the keeper dives full stretch and keeps it out!",
        "saved! he read the taker like a book!",
        "the keeper throws up a hand and blocks it — saved!",
        "saved! a heroic stop to deny the penalty!",
        "the keeper guesses right and pushes it onto the post!",
        "saved! he gets down quickly and smothers it!",
        "the keeper hurls himself across and saves brilliantly!",
        "saved! the spot kick is beaten away!",
        "the keeper anticipates it and parries to safety — saved!",
        "saved! an outstanding low dive keeps it out!",
        "the keeper stops it with his legs — what a save!",
        "saved! he dives the right way and holds firm!",
        "the keeper denies him at the death — saved!",
        "saved! a remarkable reaction stop from the spot!",
        "the keeper guesses right and the penalty is foiled!",
        "saved! he gets a strong wrist to it and turns it away!",
    };

    /// <summary>Returns a phrase describing a penalty saved by the goalkeeper.</summary>
    public static string PenaltySaved(ref Xoshiro256 rng)
    {
        return PenaltiesSaved[rng.NextInt(PenaltiesSaved.Length)];
    }

    private static readonly string[] YellowCards =
    {
        "shown a yellow card",
        "into the referee's book",
        "cautioned by the referee",
        "booked",
        "into the book",
        "shown the yellow",
        "given a yellow card",
        "cautioned for the challenge",
        "booked by the official",
        "carded by the referee",
        "shown a caution",
        "in the book now",
        "picks up a yellow card",
        "earns himself a booking",
        "goes into the referee's notebook",
        "is cautioned",
        "shown a yellow",
        "finds his name in the book",
        "collects a yellow card",
        "is booked for that",
        "handed a caution",
        "shown the yellow card by the referee",
        "gets his name taken",
        "booked without complaint",
        "into the book he goes",
        "receives a yellow card",
        "is shown a caution by the official",
        "joins the list in the referee's book",
        "picks up a caution",
        "cautioned and that's a yellow",
        "duly booked by the referee",
        "carded for the offence",
        "shown a yellow and a stern word",
        "goes in the book",
        "is given a caution",
        "lands himself a yellow card",
        "added to the referee's book",
    };

    /// <summary>Returns a phrase describing a player being cautioned with a yellow card.</summary>
    public static string Yellow(ref Xoshiro256 rng)
    {
        return YellowCards[rng.NextInt(YellowCards.Length)];
    }

    private static readonly string[] StraightReds =
    {
        "OFF! a straight red card!",
        "shown a straight red card — he's off!",
        "sent off! a straight red from the referee",
        "dismissed! the referee brandishes a straight red",
        "a straight red — and he has to go!",
        "OFF! the referee produces a straight red card",
        "shown red — a straight dismissal!",
        "sent for an early bath with a straight red",
        "a red card, no yellow first — he's gone!",
        "marching orders! a straight red card",
        "OFF! the referee reaches for the red card",
        "dismissed with a straight red — no second chance!",
        "shown a straight red and sent off!",
        "the referee shows red — straight off!",
        "a straight red card — he is dismissed!",
        "off he goes — a straight red from the official!",
        "OFF! a clear red card straight away",
        "given a straight red — his afternoon is over!",
        "sent off! the referee had no choice but red",
        "a straight red, and down to ten they go!",
        "shown the red card — a straight dismissal!",
        "OFF! the referee flashes a straight red",
        "a red card with no caution first — he's off!",
        "dismissed! a straight red leaves them a man short",
        "the referee produces red — straight off he goes!",
        "OFF! a straight red and an early shower",
        "sent off with a straight red card!",
        "shown a straight red — the official was certain",
        "a straight red — the referee points to the tunnel!",
        "OFF! red card, no hesitation from the referee",
        "given his marching orders — a straight red!",
        "dismissed! the referee holds aloft a straight red",
        "a straight red card and he trudges off!",
        "OFF! the red card is out — a straight dismissal",
        "shown red on the spot — a straight sending-off!",
        "a straight red — there's no way back from that!",
        "the referee shows a straight red — he's off!",
    };

    /// <summary>Returns a phrase describing a player being sent off with a straight red card.</summary>
    public static string StraightRed(ref Xoshiro256 rng)
    {
        return StraightReds[rng.NextInt(StraightReds.Length)];
    }

    private static readonly string[] SecondYellowReds =
    {
        "a second yellow — and he's off!",
        "shown a second yellow, and that's a red!",
        "a second caution — the red card follows!",
        "booked again — and that means an early bath!",
        "a second yellow card, and he has to go!",
        "cautioned for the second time — he's sent off!",
        "two yellows make a red — he's dismissed!",
        "a second booking, and out comes the red!",
        "shown yellow again — and now the red card!",
        "his second caution of the match — off he goes!",
        "a second yellow, then the inevitable red!",
        "booked for a second time — and that's a sending-off!",
        "a second yellow card — he leaves them a man down!",
        "cautioned twice — the referee shows red!",
        "another yellow, and that's two — he's off!",
        "a second yellow — the red card is brandished!",
        "shown a second caution, and he's dismissed!",
        "two bookings and he's gone — a second yellow!",
        "a second yellow card means red — off he trudges!",
        "his second yellow, and the referee points him away!",
        "cautioned again — a second yellow and a red!",
        "a second booking — he's sent for an early shower!",
        "yellow number two — and out comes the red card!",
        "a second caution, and that's his match finished!",
        "shown a second yellow and then red — he's off!",
        "two yellows, one red — he has to depart!",
        "a second yellow card, and down to ten they go!",
        "booked twice and dismissed — a second yellow!",
        "a second caution — the official has no choice but red!",
        "his second yellow of the afternoon — he's off!",
        "a second booking, and the red card is shown!",
        "cautioned for a second time and sent off!",
        "a second yellow — and his game is over!",
        "two yellows, and the referee reaches for red!",
        "a second yellow card — he walks before the red is even up!",
        "shown a second yellow — that's a dismissal!",
        "a second caution and a red — he heads for the tunnel!",
    };

    /// <summary>Returns a phrase describing a player dismissed for a second yellow card.</summary>
    public static string SecondYellowRed(ref Xoshiro256 rng)
    {
        return SecondYellowReds[rng.NextInt(SecondYellowReds.Length)];
    }

    private static readonly string[] Substitutions =
    {
        "a change is made",
        "fresh legs coming on",
        "a substitution for the side",
        "the manager makes a switch",
        "fresh legs introduced to the contest",
        "a change on the touchline",
        "the board goes up — a substitution",
        "on comes a replacement",
        "a tactical change from the bench",
        "the manager turns to his bench",
        "a switch is made here",
        "new legs onto the pitch",
        "a substitution as the numbers go up",
        "the boss freshens things up",
        "a change in personnel",
        "off comes one, on comes another",
        "a substitute is readied and sent on",
        "the bench is called into action",
        "fresh impetus from the bench",
        "a change to shake things up",
        "the fourth official raises the board",
        "a substitution to alter the shape",
        "on trots the replacement",
        "a swap on the sidelines",
        "the manager rolls the dice with a change",
        "a fresh face enters the fray",
        "an early substitution here",
        "the side make a change",
        "a new man strips off and comes on",
        "a like-for-like switch",
        "the manager makes his move from the bench",
        "fresh legs to chase the game",
        "a substitution to see the game out",
        "a change as the board is lifted",
        "the replacement is sent into the action",
        "a switch of personnel on the hour",
        "a substitution rings the changes",
    };

    /// <summary>Returns a phrase describing a substitution being made.</summary>
    public static string Substitution(ref Xoshiro256 rng)
    {
        return Substitutions[rng.NextInt(Substitutions.Length)];
    }

    private static readonly string[] Injuries =
    {
        "is down and needs treatment",
        "is down and the physios are called",
        "stays down clutching his leg",
        "needs treatment on the pitch",
        "is hurt and the medical team rushes on",
        "goes down clutching his hamstring",
        "is receiving treatment from the physio",
        "is down injured and play is stopped",
        "lies on the turf in some discomfort",
        "calls for the trainers",
        "is down and the game pauses for treatment",
        "is being treated on the sideline",
        "limps and signals to the bench",
        "is hurt after the challenge",
        "is down and clearly in pain",
        "needs the magic sponge",
        "is stretched out as the physios attend",
        "stays down and the referee stops play",
        "feels something and pulls up",
        "is down holding his ankle",
        "requires lengthy treatment",
        "is hurt and waves for assistance",
        "goes to ground and stays there",
        "is down and the stretcher is readied",
        "winces and signals he can't continue",
        "is treated by the medical staff",
        "is down with what looks like a knock",
        "pulls up sharply and goes down",
        "is hobbling and needs attention",
        "is down and the trainers sprint on",
        "stays down and the crowd falls quiet",
        "is being patched up on the touchline",
        "feels his muscle and drops to the turf",
        "is down — the physios are waved on",
        "needs treatment before he can continue",
        "is grimacing as the medics arrive",
        "goes down and the game is held up",
    };

    /// <summary>Returns a phrase describing a player who is injured and needs treatment.</summary>
    public static string Injury(ref Xoshiro256 rng)
    {
        return Injuries[rng.NextInt(Injuries.Length)];
    }

    private static readonly string[] GreatSaves =
    {
        "what a save!",
        "a stunning stop!",
        "an incredible save by the keeper!",
        "a magnificent stop to keep it out!",
        "what a save — that was destined for the net!",
        "the keeper flies across and tips it over!",
        "a brilliant reflex save!",
        "an outstanding save from point-blank range!",
        "the keeper claws it out of the top corner!",
        "what a save — he came from nowhere!",
        "a superb fingertip save!",
        "the keeper denies him with a stunning stop!",
        "an unbelievable save to preserve the scoreline!",
        "the keeper throws up a hand and parries it away!",
        "a remarkable save at full stretch!",
        "what a save — the keeper has won them a point!",
        "the keeper turns it round the post brilliantly!",
        "a wonderful diving save!",
        "the keeper gets down sharply to smother it!",
        "an acrobatic save to keep them level!",
        "the keeper produces a save from the very top drawer!",
        "what a stop — he stood firm and blocked it!",
        "a phenomenal save with the legs!",
        "the keeper springs across to deny a certain goal!",
        "a stunning one-handed save!",
        "the keeper reacts brilliantly to push it wide!",
        "what a save — pure goalkeeping instinct!",
        "the keeper claws it away from under the bar!",
        "an extraordinary stop to keep the door shut!",
        "the keeper flings himself to his left and saves!",
        "a breathtaking save to deny the breakaway!",
        "the keeper gets a strong hand to it and over it goes!",
        "what a save — he's kept his side in this!",
        "a world-class stop from the goalkeeper!",
        "the keeper smothers it bravely at his feet!",
        "an instinctive save to turn it onto the bar!",
        "the keeper produces a stop out of nothing!",
    };

    /// <summary>Returns a phrase describing an outstanding goalkeeping save.</summary>
    public static string GreatSave(ref Xoshiro256 rng)
    {
        return GreatSaves[rng.NextInt(GreatSaves.Length)];
    }
}
