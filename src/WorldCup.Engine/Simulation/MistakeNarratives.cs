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
