using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// VAR review drama. The check is always narrated as UPHOLDING the on-field decision (a goal that was
/// already scored is confirmed; an appeal that was waved away stays waved away), so it adds tension and
/// theatre without ever adding or removing a goal/card/penalty — the calibrated totals are untouched.
/// </summary>
public static class VarNarratives
{
    private static string Pick(string[] bank, ref Xoshiro256 rng) => bank[rng.NextInt(bank.Length)];

    /// <summary>A full one-line description of a VAR check and its (upholding) outcome.</summary>
    public static string Describe(VarKind kind, ref Xoshiro256 rng)
    {
        string opener = Pick(Openers(kind), ref rng);
        string outcome = kind switch
        {
            VarKind.PenaltyAppeal => Pick(NoPen, ref rng),
            VarKind.PossibleRed => Pick(NoRed, ref rng),
            _ => Pick(GoalGiven, ref rng),
        };
        return $"{opener} {outcome}";
    }

    private static string[] Openers(VarKind kind) => kind switch
    {
        VarKind.OffsideOnGoal => OffsideOpeners,
        VarKind.HandballOnGoal => HandballOpeners,
        VarKind.GoalLine => GoalLineOpeners,
        VarKind.PenaltyAppeal => PenaltyOpeners,
        VarKind.PossibleRed => RedOpeners,
        _ => OffsideOpeners,
    };

    private static readonly string[] OffsideOpeners =
    {
        "The goal goes up to VAR for a tight offside check —",
        "Up to the screen they go, looking at a marginal offside in the build-up —",
        "The flag stayed down, but VAR is drawing the lines for offside —",
        "They're checking the toes and the armpits on this one, offside under review —",
        "A long, agonising VAR check for offside on the goal —",
        "The assistant referee kept the flag down, but VAR has flagged it for review — millimetres in it —",
        "They're drawing lines on that freeze-frame now, it could be an eyelash in it —",
        "Hold your celebrations — VAR is scrutinising the offside frame by frame —",
        "The crowd has gone quiet. VAR is checking the shoulder, the heel, every pixel —",
        "It looked fine in real time, but the technology will have the final word here —",
        "An agonising pause as the VAR team isolate the exact moment the ball was played —",
        "The linesman was unsighted but VAR certainly isn\'t — ultra-precise offside lines being drawn —",
        "Both sets of supporters holding their breath as the millimetre check continues —",
        "VAR zooming in on the attacker\'s body position at the moment of the pass —",
        "A nerve-shredding wait as the lines converge on that offside check —",
    };

    private static readonly string[] HandballOpeners =
    {
        "VAR is taking a look at a possible handball in the build-up to the goal —",
        "The defenders are screaming handball, and VAR is reviewing it —",
        "Up to the monitor — was there a touch of the arm before the goal? —",
        "A lengthy check for handball in the move that led to the goal —",
        "Did the ball strike the arm? VAR is going through it angle by angle —",
        "The protests are loud but the technology will settle this — handball under the microscope —",
        "The referee consults the pitchside monitor — was the arm in an unnatural position? —",
        "Every angle is being studied now for that potential handball in the build-up —",
        "VAR has flagged something — possible handball two passes before the goal —",
        "The ball seemed to clip the arm, and VAR is determined to get to the bottom of it —",
        "Slow motion, freeze-frame, every camera available — was it handball? —",
        "The arm position is the key question as VAR pauses the world to examine the frame —",
    };

    private static readonly string[] GoalLineOpeners =
    {
        "Goal-line technology and VAR confirming whether it crossed —",
        "Did the whole of the ball cross the line? Over to the technology —",
        "A check to see if it was scrambled away in time —",
        "Goal-line technology has been triggered — we await the verdict —",
        "That was hacked off the line — or was it? VAR and the sensors will tell us —",
        "Seven cameras, millimetre precision — did the ball fully cross before it was cleared? —",
        "The goalkeeper got something on it, but was it already over? Goal-line tech checking now —",
        "Bedlam in the goalmouth — VAR and the goal-line system are the only cool heads here —",
        "Every millimetre matters — did the whole of the ball cross the whole of the line? —",
        "The referee is waiting for the earpiece message from goal-line technology —",
    };

    private static readonly string[] PenaltyOpeners =
    {
        "Huge penalty appeals, and VAR is taking a look —",
        "The whole side is screaming for a spot-kick — over to the review —",
        "VAR checking a coming-together in the box —",
        "A lengthy review of a tangle of legs in the area —",
        "Down goes the attacker in the penalty box — VAR is examining whether there was contact —",
        "The referee waved play on, but VAR is not so sure and is taking a closer look —",
        "An enormous appeal for a penalty and the VAR team are poring over the replays —",
        "Was there a nudge, a clip, a trailing leg? VAR will decide the fate of this appeal —",
        "The player is insisting it was a foul, but the referee said no — VAR will have the final say —",
        "Lots of bodies in the box — was it a trip, a push, or good defending? VAR is checking —",
        "The bench is up in arms demanding a penalty — VAR is reviewing every angle available —",
        "A VAR check on that aerial challenge in the area — was there a foul? —",
        "Contact in the box, an appeal goes up, and the review room is now involved —",
    };

    private static readonly string[] RedOpeners =
    {
        "VAR is reviewing whether that challenge was a red card —",
        "The referee is being sent to the monitor to look again at the tackle —",
        "A check on a possible sending-off for that challenge —",
        "The initial decision was yellow, but VAR has flagged it for a severity review —",
        "That was a nasty one — VAR is checking the point of contact and the force of the tackle —",
        "The referee showed yellow but the VAR team want to look again at the replays —",
        "An ugly challenge and the VAR room is now assessing whether it merited more than a booking —",
        "He caught him high — was it reckless or was it dangerous? VAR will decide —",
        "The victim is down, the physios are on, and VAR is examining that challenge very carefully —",
        "Booked on the field, but VAR is not convinced that was sufficient punishment —",
        "A review of that lunge — the slow motion replay will be crucial here —",
        "The referee goes to the monitor at the side of the pitch to assess the tackle himself —",
    };

    private static readonly string[] GoalGiven =
    {
        "and after an agonising wait… the goal STANDS! Bedlam!",
        "and it's been GIVEN! The replay confirms it — it counts!",
        "the on-field decision is upheld — the goal is good!",
        "and the lines are drawn… he's ON! The goal stands!",
        "no infringement found — the goal is awarded and the celebrations can finally begin!",
        "and THAT IS A GOAL! The check is complete — it stands!",
        "clean. Absolutely clean. The goal is confirmed — get in!",
        "no question about it once you see it frame by frame — GOAL STANDS!",
        "and the verdict is in — no offence, the goal counts! The stadium erupts!",
        "everything checks out — that is a perfectly good goal, and the lead is extended!",
        "VAR has done its job — the goal is upheld and the scorer can celebrate properly now!",
        "and there it is — CONFIRMED! The goal stands and the noise inside this stadium is incredible!",
        "nothing wrong with that whatsoever — GOAL GIVEN, and the crowd goes absolutely wild!",
        "the technology agrees with the referee — it\'s a legitimate goal, it COUNTS!",
        "after all that, the goal is good! No controversy — it stands, it stands, it STANDS!",
    };

    private static readonly string[] NoPen =
    {
        "and there's nothing in it — no penalty, play on!",
        "the referee sticks with his decision — no spot-kick given, huge let-off!",
        "and VAR says no — play waved on, the defenders breathe again!",
        "minimal contact, says the review — no penalty, and the appeals are waved away!",
        "VAR backs the referee — no spot-kick, the defensive wall holds firm!",
        "not enough for a penalty — the on-field call is upheld, play continues!",
        "simulation suspected or simply not a foul — VAR waves it off, no penalty given!",
        "clean challenge confirmed — no penalty, and the keeper is pumping his fists!",
        "the referee was right all along — VAR agrees, no penalty, huge relief for the defence!",
        "too little contact to overturn the decision — no penalty awarded, play resumes!",
        "VAR has looked at it from every angle and found no foul — no penalty, breathe easy!",
        "the attacker went down but the defender got the ball — no penalty, and that\'s the correct call!",
    };

    private static readonly string[] NoRed =
    {
        "and the referee sticks with the yellow — no red, he stays on!",
        "VAR deems it worthy of only a booking — no sending-off!",
        "the challenge is reviewed and downgraded — he survives with a caution!",
        "reckless perhaps, but not dangerous — the yellow stands, he remains on the pitch!",
        "VAR agrees with the referee — yellow card upheld, no red, and he\'ll count himself lucky!",
        "strong challenge but not over the top of the ball — no red, he stays!",
        "the tackle looked worse in real time — on review, it\'s only a booking, he\'s staying on!",
        "intent not proven, force deemed acceptable — yellow card only, no dismissal!",
        "after a thorough look, VAR backs the yellow card decision — he\'s staying on the pitch!",
        "the contact was high but the speed of the game was a factor — caution confirmed, no red!",
        "VAR clears him of a red — it\'s just a booking, and the manager breathes again on the touchline!",
        "not as bad as first feared — the review is complete, he keeps his place on the field!",
    };
}
