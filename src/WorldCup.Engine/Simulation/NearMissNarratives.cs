using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Near-miss and woodwork narration. Fragments slot after the player's name — e.g.
/// "{player} {fragment}" — to describe a chance that came to nothing. Pure flavour generated after the
/// final whistle, so it never touches the score.
/// </summary>
public static class NearMissNarratives
{
    private static string Pick(string[] bank, ref Xoshiro256 rng) => bank[rng.NextInt(bank.Length)];

    /// <summary>Full one-line description of a near miss (e.g. "Smith rattled the crossbar from 20 yards").</summary>
    public static string Describe(NearMissKind kind, string player, ref Xoshiro256 rng) =>
        $"{player} {Pick(Fragments(kind), ref rng)}";

    /// <summary>An emoji + short label for the box-score / report.</summary>
    public static (string Icon, string Label) Badge(NearMissKind kind) => kind switch
    {
        NearMissKind.HitThePost => ("🪵", "Off the post"),
        NearMissKind.HitTheBar => ("🪵", "Off the bar"),
        NearMissKind.OffTheLine => ("🧱", "Cleared off the line"),
        NearMissKind.GreatBlock => ("🛡", "Heroic block"),
        NearMissKind.BlazedOver => ("🚀", "Blazed over"),
        NearMissKind.JustWide => ("😬", "Inches wide"),
        NearMissKind.HeaderOff => ("🤕", "Header off target"),
        NearMissKind.RattledTheWoodwork => ("🪵", "Off the woodwork"),
        _ => ("⚽", "Near miss"),
    };

    private static string[] Fragments(NearMissKind kind) => kind switch
    {
        NearMissKind.HitThePost => Post,
        NearMissKind.HitTheBar => Bar,
        NearMissKind.OffTheLine => OffLine,
        NearMissKind.GreatBlock => Block,
        NearMissKind.BlazedOver => Over,
        NearMissKind.JustWide => Wide,
        NearMissKind.HeaderOff => Header,
        NearMissKind.RattledTheWoodwork => Woodwork,
        _ => Wide,
    };

    private static readonly string[] Post =
    {
        "smacked a fierce drive against the inside of the post",
        "curled an effort that kissed the outside of the upright",
        "rifled one off the base of the post and away to safety",
        "saw a low shot clip the foot of the post and roll agonisingly across the face of goal",
        "thumped a half-volley that cannoned back off the post",
        "steered a crisp side-foot effort onto the right-hand post and out",
        "arrowed a first-time shot that rebounded off the near post and fell to safety",
        "found the inside of the post with a pinpoint chip — only for the ball to stay out",
        "drilled a low drive that struck the far post and deflected away from goal",
        "bent one around the goalkeeper only to watch it come back off the post",
        "saw his angled drive ricochet off the post and into the grateful keeper's arms",
    };

    private static readonly string[] Bar =
    {
        "rattled the crossbar with a thunderous strike from distance",
        "cracked an unstoppable effort off the underside of the bar — and somehow out",
        "dipped a free-kick onto the top of the crossbar",
        "watched a looping header drop onto the bar and bounce clear",
        "smashed one against the bar that had the keeper rooted",
        "struck a volley that clipped the top of the bar and cleared the net",
        "chipped the goalkeeper cleanly, only to see the ball bounce off the crossbar and away",
        "bent a free-kick onto the underside of the bar and it came down right on the line",
        "lofted a delicate effort that grazed the crossbar on its way over",
        "unleashed a swerving drive that crashed back off the top of the bar",
        "had the bar to thank for stopping him scoring an absolute screamer of an own goal",
    };

    private static readonly string[] OffLine =
    {
        "was denied by a magnificent goal-line clearance",
        "saw a goalbound effort hooked off the line at the last instant",
        "thought he'd scored — only for a defender to scramble it off the line",
        "had a header clawed away from under the bar and hacked off the line",
        "was foiled by a desperate, sliding block right on the goal-line",
        "watched in disbelief as a goal-line clearance kept his shot out",
        "had a tap-in denied by a breathtaking last-ditch clearance from nowhere",
        "saw the ball spin towards an open net before a covering defender hacked it away",
        "had an effort cleared by the defender with barely a centimetre to spare",
        "was left stunned as a scrambling defender somehow diverted it off the line",
        "lost out to a remarkable goal-line block — the scores still level",
    };

    private static readonly string[] Block =
    {
        "was denied by a brave, last-ditch block",
        "had a goalbound shot charged down by a defender flinging himself in the way",
        "saw a certain goal smothered by a heroic sliding challenge",
        "was thwarted by a superbly-timed block right on the stretch",
        "had the effort blocked by a defender taking it full in the face",
        "was denied by a selfless block from a covering defender who had no right to get there",
        "saw a low drive brilliantly blocked by an outstretched boot at the near post",
        "had a point-blank effort stopped by a remarkable instinctive block",
        "watched his driven cross-shot charged down by a defender at the vital moment",
        "was foiled in the act of shooting by a perfectly-timed sliding intervention",
        "saw a goalbound effort deflected behind by a well-positioned last-gasp block",
    };

    private static readonly string[] Over =
    {
        "blazed the chance high over the bar from a glorious position",
        "leaned back and ballooned it over when it looked easier to score",
        "snatched at it and lashed the ball into the stands",
        "skied a free header that he'll want back",
        "got under it completely and sent it sailing over the top",
        "miscued a volley that looped well over the bar from six yards",
        "shinned a close-range chance over the crossbar in the most excruciating fashion",
        "attempted an ambitious overhead kick and hoofed it into the upper tier",
        "rushed his shot and spooned the ball high over the bar under no real pressure",
        "had all the time in the world but still managed to steer it over the top",
        "connected with the ball too cleanly from the angle and watched it drift over",
    };

    private static readonly string[] Wide =
    {
        "dragged a shot agonisingly wide of the far post",
        "curled one inches the wrong side of the upright",
        "slid in at the back post and just couldn't quite turn it home",
        "rolled a one-on-one a whisker past the post",
        "flashed a low drive across the face of goal and inches wide",
        "poked a toe at it on the half-turn and watched the ball trickle just wide",
        "swept a first-time effort that shaved the outside of the post",
        "fired a side-foot shot from the edge of the box fractionally wide of the angle",
        "cut inside and let fly, only to see the effort slide narrowly past the far post",
        "dinked a clever chip that drifted just wide of the upright with the keeper stranded",
        "drove a snapshot wide of the near post when he might have taken an extra touch",
    };

    private static readonly string[] Header =
    {
        "rose highest but nodded his header just past the post",
        "met the cross perfectly only to glance it the wrong side of the upright",
        "powered a header that flew narrowly over",
        "got his header all wrong and steered it wide when it seemed easier to score",
        "thumped a downward header that bounced just past the post",
        "attacked the near-post delivery but directed his header straight at the keeper",
        "flicked a header towards the far corner that drifted just wide",
        "planted a firm header goalwards only to see it deflect just over the bar",
        "glanced a header across goal that crept wide of the far post",
        "stooped to meet a low cross and nodded it agonisingly over from close range",
        "found himself unmarked at the back post but looped his header over the bar",
    };

    private static readonly string[] Woodwork =
    {
        "struck the woodwork with a ferocious effort",
        "saw a swerving shot clatter the frame of the goal",
        "rattled the woodwork and the whole crowd gasped as one",
        "watched the ball ping off the woodwork and bounce clear",
        "hit the frame for the second time in the match — it's just not his night",
        "clattered the upright with a blistering long-range drive",
        "sent a dipping volley crashing off the junction of post and bar",
        "beat the goalkeeper all ends up, only for the woodwork to spare the blushes",
        "struck the frame of the goal from an impossible angle and the ball rebounded to safety",
        "rattled the post with a venomous left-foot curler that the keeper had no chance with",
        "watched a deflected effort spin into the woodwork and out — fortune refusing to smile",
    };
}
