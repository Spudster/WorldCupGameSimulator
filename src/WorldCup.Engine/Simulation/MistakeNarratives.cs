using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Provides vivid, randomized narrative detail clauses for simulated match mistakes:
/// outfield defensive errors, goalkeeper errors, and refereeing bad calls. Each phrase is an
/// action/detail clause only — it contains no player names and no outcome words. The caller is
/// responsible for attaching the player name and describing the consequence.
/// </summary>
public static class MistakeNarratives
{
    /// <summary>
    /// Defensive blunders that directly led to a goal. Clear, costly mistakes.
    /// </summary>
    private static readonly string[] DefensiveErrorLedToGoal =
    {
        "a misplaced back-pass",
        "a sliced clearance under no pressure",
        "slipped at the worst moment",
        "dwelt on the ball and was dispossessed",
        "a loose square pass intercepted",
        "ball-watched and let the runner in",
        "a heavy first touch in his own box",
        "played the striker onside with a slack line",
        "miscontrolled and gifted possession",
        "a weak headed clearance that fell invitingly",
        "a hospital back-pass straight to the striker",
        "switched off at the back post",
        "a needless step-up that broke the offside trap",
        "let the cross sail over his head",
        "a catastrophic miscommunication with his partner",
        "left stranded after dawdling on the ball",
        "an air-kicked clearance that gift-wrapped possession",
        "played everyone onside with a comical step forward",
        "a wretched miskick straight into the path of the striker",
        "failed to track his runner from a set-piece",
        "a needless square ball across his own six-yard box",
        "stood and watched instead of clearing his lines",
        "collided with his own goalkeeper going for the same ball",
        "a dreadful clip in behind his own defence",
        "sold his partner short with a soft roll across the box",
        "a moment of pure madness — dribbling into his own danger zone",
        "let it bounce when he should have headed clear",
        "a sloppy touch that rolled agonisingly into the striker\'s feet",
        "failed to deal with a routine long ball — simply froze",
        "an unforgivable hesitation that handed the striker a free run",
        "tried to be too clever and was robbed in a dangerous area",
        "a stray header back that completely bypassed the goalkeeper",
        "wrong-footed by a simple bounce and lost the ball instantly",
    };

    /// <summary>
    /// Defensive mistakes framed as near-misses that did not lead to a goal.
    /// </summary>
    private static readonly string[] DefensiveErrorNoGoal =
    {
        "a sloppy giveaway in a dangerous area",
        "a heavy touch under pressure",
        "a wayward clearance",
        "a mistimed challenge that opened a gap",
        "a careless pass across the box",
        "a slip on the turn",
        "a loose touch that nearly let the runner in",
        "a half-hearted clearance back into trouble",
        "a ragged offside line that almost backfired",
        "a stray pass hooked away just in time",
        "a scuffed clearance that flashed across the area",
        "a moment of hesitation on the ball",
        "a telegraphed back-pass that nearly invited trouble",
        "a clumsy attempt to play out from the back",
        "a dreadful first touch that squandered a good position",
        "a misread bounce that almost let the forward through",
        "an unnecessary dribble in his own penalty area",
        "a misdirected header under pressure that fell short",
        "a defensive mix-up narrowly sorted at the last moment",
        "almost tripped over his own feet trying to control it",
        "a rash lunge that nearly opened up a shooting lane",
        "dawdled in possession and nearly paid the price",
        "switched off momentarily and allowed a dangerous flick-on",
        "a wild swing that could easily have gone anywhere",
        "a loose touch on the edge of his own box — cleared just in time",
        "a mistimed jump at a routine header",
        "went to ground under no real pressure and struggled to recover",
        "a short goal-kick played straight into danger — scrambled clear",
        "a poorly positioned defensive line quickly corrected before damage was done",
        "let the ball bobble past him — saved by a covering team-mate",
    };

    /// <summary>
    /// Goalkeeper howlers that directly led to a goal.
    /// </summary>
    private static readonly string[] GoalkeeperErrorLedToGoal =
    {
        "spilled a routine shot",
        "flapped at a cross",
        "let a tame effort squirm under the body",
        "misjudged the flight of the cross",
        "a weak punch that dropped to an attacker",
        "beaten at his near post",
        "rushed out and missed everything",
        "a sloppy throw straight to an opponent",
        "parried it back into the danger zone",
        "got his feet in a tangle",
        "a fumble through the hands",
        "wrong-footed by a tame deflection",
        "rooted to his line by a gentle effort",
        "a panicked clearance charged down",
        "caught in no-man\'s-land and left with no chance",
        "palmed it directly into the path of the onrushing forward",
        "dropped it at the feet of the lurking attacker",
        "came for the cross and got nowhere near it",
        "spilled a pea-roller under his own body",
        "let a speculative long-range effort dip under his crossbar",
        "punched when he should have caught — and found an attacker",
        "allowed a weak near-post shot to squirm inside the upright",
        "turned his back on a long throw and lost track of the ball",
        "misjudged the pace of a back-pass and slid into an embarrassing mistouch",
        "dithered over a clearance and was closed down instantly",
        "attempted an ambitious save and allowed the ball to loop over him",
        "failed to get down quickly enough to a low driven shot",
        "dived early and was beaten by a shot through his body",
        "took his eye off the ball at the critical moment",
        "punched under pressure straight to a forward twenty yards out",
        "a catastrophic misfield left the net gaping",
        "fumbled a corner directly into his own goal",
    };

    /// <summary>
    /// Goalkeeper mistakes framed as near-misses that did not lead to a goal.
    /// </summary>
    private static readonly string[] GoalkeeperErrorNoGoal =
    {
        "spilled it but smothered the rebound",
        "flapped at a cross and scrambled back",
        "a mis-hit clearance under pressure",
        "almost let one slip through his grasp",
        "caught in two minds off his line",
        "a juggled catch gathered at the second attempt",
        "a nervy punch that just cleared the danger",
        "stumbled on his line but recovered in time",
        "a loose grip clawed back before the line",
        "rushed a clearance that nearly fell short",
        "palmed it into the danger zone but colleagues scrambled it clear",
        "dropped to his knees to smother a tame effort — just about held on",
        "came and went for the cross — somehow recovered before it was too late",
        "a wild kick that spooned into orbit — no harm done",
        "nearly let a routine low cross squirm through — reacted just in time",
        "lost his footing under no pressure but scrambled back to his feet",
        "a wobbly claim at a corner that had everyone holding their breath",
        "hesitated over a straightforward catch and almost let it drop",
        "punched when he should have claimed — the clearance was haphazard but sufficient",
        "misjudged a back-pass and had to improvise with his chest",
        "threw to an area instead of a player — fortunate it landed safely",
        "fluffed the collect and had to paw at it a second time — just enough",
        "a shaky one-handed push around the post when a clean catch was on",
        "dithered coming off his line and only survived thanks to a team-mate\'s recovery",
        "nearly pushed a corner into his own net — scrambled to the relief of all",
    };

    /// <summary>
    /// Detail clauses for a penalty wrongly awarded.
    /// </summary>
    private static readonly string[] WrongPenaltyAwarded =
    {
        "a soft coming-together the referee bought",
        "minimal contact dressed up as a foul",
        "a theatrical tumble the referee fell for",
        "an accidental tangle of legs given as a spot-kick",
        "a clip the attacker exaggerated beautifully",
        "a fair shoulder-to-shoulder ruled a foul",
        "a dive in the box the referee rewarded",
        "a phantom trip pointed straight to the spot",
        "an attacker going down far too easily from a brush of the sleeve",
        "a clever piece of play-acting that bamboozled the official",
        "a ball striking an arm tight to the body — harshly deemed handball",
        "a perfectly legitimate challenge somehow interpreted as a penalty",
        "the attacker made the most of it and the referee was convinced",
        "a wild over-reaction to minimal contact right in front of the official",
        "the lightest of touches transformed into a sprawling tumble",
        "a natural arm position flagged as deliberate handball — very harsh",
        "contact so slight it was barely visible on the replay, yet the spot-kick was given",
        "the official pointed to the spot despite replays showing clean contact",
        "VAR upheld the on-field decision but few in the stadium agreed",
        "a marginal call that will be debated long after the final whistle",
        "the defender barely grazed him but the referee had no hesitation",
    };

    /// <summary>
    /// Detail clauses for a clear penalty wrongly denied.
    /// </summary>
    private static readonly string[] PenaltyDenied =
    {
        "a clear shirt-pull waved away",
        "a stonewall trip ignored",
        "a blatant handball missed",
        "an obvious shove in the back overlooked",
        "a scything challenge the referee waved on",
        "a clear foul the referee somehow missed",
        "a cynical drag-back left unpunished",
        "a nailed-on spot-kick denied",
        "a blatant body-check inside the area that went completely unpunished",
        "a desperate lunge catching the attacker\'s ankles — the referee waved play on",
        "a goalkeeper bringing down the forward and inexplicably escaping censure",
        "both hands used to haul back the striker — somehow unseen",
        "a deliberate elbow inside the box that went completely unpunished",
        "a trip so obvious the whole stadium appealed — waved on regardless",
        "the handball in the box was glaring yet the referee refused to point to the spot",
        "a clear two-handed shove in the back dismissed by the official",
        "VAR reviewed it and still declined to intervene — baffling the commentators",
        "the attacker was scythed down inside the box; the referee saw nothing wrong",
        "a clumsy late challenge that would have been given anywhere else on the pitch",
        "the assistant flagged for offside, denying what would otherwise have been a nailed-on penalty",
    };

    /// <summary>
    /// Detail clauses for a wrongly issued card.
    /// </summary>
    private static readonly string[] WrongCard =
    {
        "a mistimed-but-fair challenge punished",
        "the wrong man booked for the foul",
        "a soft caution for a clean tackle",
        "a harsh yellow for winning the ball cleanly",
        "a booking for a foul that never was",
        "a caution waved at an innocent bystander",
        "an unjust card for an honest challenge",
        "a needless booking for a fair shoulder-charge",
        "a yellow for alleged simulation when replays showed clear contact",
        "booked for dissent after barely uttering a word",
        "penalised for a perfectly timed, textbook sliding tackle",
        "a caution for time-wasting when the clock showed barely sixty minutes",
        "sin-binned for a foul that most would have called fifty-fifty",
        "the wrong player cautioned when the guilty party stood yards away",
        "a second yellow for an innocuous challenge — harshly sent off",
        "a red card issued for a foul that warranted a yellow at most",
        "booked for accidentally catching the opponent on the follow-through",
        "a yellow shown for a firm but entirely legal aerial challenge",
        "cautioned for a fair tackle from behind that won the ball cleanly",
        "a booking that changed the complexion of the whole match needlessly",
    };

    /// <summary>
    /// Detail clauses for a card that should have been shown but was not.
    /// </summary>
    private static readonly string[] MissedCard =
    {
        "a reckless lunge that warranted a red",
        "a clear last-man foul unpunished",
        "studs-up challenge the referee waved play on",
        "a cynical professional foul that escaped a card",
        "a wild two-footed tackle left unbooked",
        "a deliberate handball that deserved a yellow",
        "an off-the-ball elbow the referee missed",
        "a dangerous late challenge that went uncarded",
        "a forearm smash into the face of an opponent the referee somehow overlooked",
        "a brutal challenge from behind — no card, unbelievably",
        "a deliberate stamp in full view that the official waved off",
        "a headbutt off the ball spotted by everyone except the referee",
        "a violent chest-to-chest confrontation that provoked no reaction from the official",
        "a cynical trip on the last defender — left completely unpunished",
        "studs shown on an opponent\'s shin with no red card forthcoming",
        "a wrestling hold at a set-piece that would have warranted a caution anywhere else",
        "a shirt-pull that denied a clear goalscoring opportunity — let off lightly",
        "a scything challenge that had the physios running on, yet not a card in sight",
        "repeated fouling that should have earned a booking long ago",
        "a clear elbow thrown in the referee\'s line of sight — incredible that it went unpunished",
        "two clear bookable offences missed within moments; the official was entirely unsighted",
        "a cynical rugby tackle in open play — somehow not even a free-kick",
    };

    /// <summary>
    /// Detail clauses for a good goal that was wrongly disallowed.
    /// </summary>
    private static readonly string[] GoalWronglyDisallowed =
    {
        "ruled out for a phantom offside",
        "chalked off for a non-existent foul",
        "flagged despite being clearly onside",
        "disallowed for an imaginary handball",
        "wrongly ruled out for a foul that never happened",
        "scrubbed off by a mistaken offside flag",
        "harshly cancelled for a clean shoulder in the build-up",
        "denied by a flag that should never have gone up",
        "chalked off for an offside that VAR ultimately confirmed was incorrect",
        "ruled out after a lengthy VAR check found nothing to support the decision",
        "cancelled for a handball that replays showed struck the shoulder, not the arm",
        "disallowed following a dubious foul call on the goalkeeper",
        "wrongly cancelled — the arm was in a natural position, no matter what the officials decided",
        "rubbed out by the video assistant despite the attacker being clearly onside",
        "negated by a controversial foul in the build-up that looked perfectly fair",
        "ruled out for a marginal offside that required multiple replay angles to even debate",
        "chalked off for a foul that went unnoticed in real time — the goal appeared perfectly good",
        "denied after the assistant\'s flag was controversially upheld despite the replays",
        "scrubbed off by the referee following a seemingly phantom infringement at a corner",
        "cancelled for a push that most pitchside observers could not even identify",
        "disallowed in baffling circumstances that left the goalscorer staring in disbelief",
    };

    /// <summary>
    /// Detail clauses for a goal that should not have been allowed.
    /// </summary>
    private static readonly string[] GoalWronglyAllowed =
    {
        "should have been flagged offside",
        "a clear push in the build-up missed",
        "the ball was out of play in the build-up",
        "a blatant handball in the move overlooked",
        "an offside the assistant failed to spot",
        "a foul on the keeper that went unseen",
        "a clear offside in the build-up waved through",
        "a handball in the lead-up the referee missed",
        "the scorer was yards offside — the flag inexplicably stayed down",
        "a deliberate handball to control the ball, somehow missed by all three officials",
        "the goalkeeper was barged off the ball in the build-up — the foul went uncalled",
        "an elbow thrown in the move that should have stopped play immediately",
        "the ball clearly crossed the touchline before the cross was delivered",
        "VAR reviewed the offside and, bafflingly, allowed the goal to stand",
        "a foul in the lead-up was plain to see on every replay — yet the goal was given",
        "the attacker used his arm to bring the ball under control; not a single official caught it",
        "the assistant\'s flag stayed down despite a clear offside — an error that will haunt them",
        "a shove on the goalkeeper went unseen and a soft goal was the result",
        "the ball struck the forward\'s hand moments before the finish — entirely missed",
        "the goal was tainted by an off-the-ball incident the referee never spotted",
        "a foul throw preceded the move; the goal stood despite the protestations",
        "the offside was tight but clear on the replay — the assistant got it badly wrong",
    };

    /// <summary>
    /// Returns a specific, vivid description of an outfield DEFENSIVE error as an action phrase only,
    /// with no player names and no outcome words.
    /// </summary>
    /// <param name="ledToGoal">When <c>true</c>, selects a clear blunder; otherwise a near-miss.</param>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>An action/detail clause describing the defensive error.</returns>
    public static string DefensiveError(bool ledToGoal, ref Xoshiro256 rng)
    {
        string[] pool = ledToGoal ? DefensiveErrorLedToGoal : DefensiveErrorNoGoal;
        return pool[rng.NextInt(pool.Length)];
    }

    /// <summary>
    /// Returns a specific description of a GOALKEEPER error as an action phrase only, with no player
    /// names and no outcome words.
    /// </summary>
    /// <param name="ledToGoal">When <c>true</c>, selects a keeper howler; otherwise a near-miss.</param>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>An action/detail clause describing the goalkeeper error.</returns>
    public static string GoalkeeperError(bool ledToGoal, ref Xoshiro256 rng)
    {
        string[] pool = ledToGoal ? GoalkeeperErrorLedToGoal : GoalkeeperErrorNoGoal;
        return pool[rng.NextInt(pool.Length)];
    }

    /// <summary>
    /// Returns a specific detail clause for a refereeing bad call, with no team or player names.
    /// </summary>
    /// <param name="type">The kind of bad call to describe.</param>
    /// <param name="rng">The random number generator, passed by reference.</param>
    /// <returns>A detail clause describing the bad call.</returns>
    public static string BadCall(BadCallType type, ref Xoshiro256 rng)
    {
        string[] pool = type switch
        {
            BadCallType.WrongPenaltyAwarded => WrongPenaltyAwarded,
            BadCallType.PenaltyDenied => PenaltyDenied,
            BadCallType.WrongCard => WrongCard,
            BadCallType.MissedCard => MissedCard,
            BadCallType.GoalWronglyDisallowed => GoalWronglyDisallowed,
            BadCallType.GoalWronglyAllowed => GoalWronglyAllowed,
            _ => WrongPenaltyAwarded,
        };
        return pool[rng.NextInt(pool.Length)];
    }
}
