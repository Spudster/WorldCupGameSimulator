using WorldCup.Data.Models;
using WorldCup.Engine.Random;

namespace WorldCup.Engine.Simulation;

/// <summary>
/// Models the crowd inside the stadium for a match: who actually fills the seats (driven by host status
/// and how big a travelling support each nation brings to a North-American 2026 World Cup), the
/// attendance (bigger as the rounds get bigger), and therefore which way the noise leans. The commentary
/// uses it to make the crowd "go wild" for the right side and at the right moments — louder for the
/// better-supported team, and scaling with the drama of the game.
/// </summary>
public sealed class Crowd
{
    private static readonly HashSet<string> Hosts = new(StringComparer.OrdinalIgnoreCase) { "USA", "MEX", "CAN" };

    // How big a support each nation brings to a 2026 World Cup on home-ish soil (default 1.0). Hosts and
    // the giants of the Americas travel in the greatest numbers; Europe/Africa/Asia bring big diasporas.
    private static readonly Dictionary<string, double> Pull = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MEX"] = 3.2, ["USA"] = 2.6, ["CAN"] = 2.0, ["BRA"] = 2.8, ["ARG"] = 2.8, ["ENG"] = 2.4,
        ["COL"] = 2.4, ["MAR"] = 2.4, ["FRA"] = 2.0, ["GER"] = 2.0, ["ESP"] = 1.9, ["POR"] = 1.9,
        ["NED"] = 1.9, ["ITA"] = 1.9, ["URU"] = 1.8, ["ECU"] = 1.8, ["CRC"] = 1.7, ["JPN"] = 1.7,
        ["KOR"] = 1.7, ["SCO"] = 1.7, ["SEN"] = 1.6, ["NGA"] = 1.6, ["POL"] = 1.6, ["CRO"] = 1.6,
        ["BEL"] = 1.6, ["GHA"] = 1.5, ["CIV"] = 1.5, ["AUS"] = 1.5, ["SUI"] = 1.4, ["DEN"] = 1.4,
        ["SRB"] = 1.4,
    };

    private static double PullOf(string code) => Pull.TryGetValue(code, out double v) ? v : 1.0;

    public Crowd(MatchResult m)
    {
        HomeCode = m.HomeCode;
        AwayCode = m.AwayCode;
        HomeName = m.HomeName;
        AwayName = m.AwayName;

        double ph = PullOf(m.HomeCode) + (Hosts.Contains(m.HomeCode) ? 2.2 : 0);
        double pa = PullOf(m.AwayCode) + (Hosts.Contains(m.AwayCode) ? 2.2 : 0);
        HomeSupportShare = Math.Clamp(ph / (ph + pa), 0.12, 0.90);
        Attendance = m.Stage switch
        {
            Stage.Final => 82500,
            Stage.ThirdPlacePlayoff => 64000,
            Stage.SemiFinal => 72000,
            Stage.QuarterFinal => 70000,
            Stage.RoundOf16 => 65000,
            Stage.RoundOf32 => 60000,
            _ => 50000,
        };
    }

    public string HomeCode { get; }
    public string AwayCode { get; }
    public string HomeName { get; }
    public string AwayName { get; }

    /// <summary>Fraction of the crowd backing the home side (0–1).</summary>
    public double HomeSupportShare { get; }

    public int Attendance { get; }

    /// <summary>The better-supported side in the stadium.</summary>
    public string DominantCode => HomeSupportShare >= 0.5 ? HomeCode : AwayCode;

    public string DominantName => HomeSupportShare >= 0.5 ? HomeName : AwayName;

    /// <summary>True when one side clearly outnumbers the other (so the atmosphere is one-sided).</summary>
    public bool Partisan => Math.Abs(HomeSupportShare - 0.5) >= 0.16;

    /// <summary>A one-line description of who is in the ground.</summary>
    public string Summary
    {
        get
        {
            if (!Partisan)
            {
                return $"A roughly even split and a terrific atmosphere, {Attendance:N0} packed in";
            }

            int pct = (int)Math.Round(Math.Max(HomeSupportShare, 1 - HomeSupportShare) * 100);
            return $"Around {pct}% of the {Attendance:N0} here are backing {DominantName} — a partisan roar";
        }
    }

    /// <summary>Pre-match atmosphere — the dominant nation's chant if it's their crowd, else a general buzz.</summary>
    public string PreMatch(ref Xoshiro256 rng) =>
        Partisan ? CrowdChants.NationChant(DominantCode, ref rng) : CrowdChants.PreMatchAtmosphere(ref rng);

    /// <summary>An ambient chant during a lull — the dominant nation's chant if partisan, otherwise generic.</summary>
    public string Chant(ref Xoshiro256 rng) =>
        Partisan && rng.NextDouble() < 0.7 ? CrowdChants.NationChant(DominantCode, ref rng) : CrowdChants.GenericChant(ref rng);

    /// <summary>The crowd's reaction to a goal: a roar if their side scores, stunned silence if it's against them.</summary>
    public string OnGoal(string scoringCode, ref Xoshiro256 rng)
    {
        if (!Partisan)
        {
            return CrowdChants.GoalRoar(ref rng);
        }

        bool dominantScored = string.Equals(scoringCode, DominantCode, StringComparison.OrdinalIgnoreCase);
        return dominantScored ? CrowdChants.GoalRoar(ref rng) : CrowdChants.Disbelief(ref rng);
    }

    public string LatePush(ref Xoshiro256 rng) => CrowdChants.LatePush(ref rng);

    public string NearMiss(ref Xoshiro256 rng) => CrowdChants.NearMiss(ref rng);

    public string Boo(ref Xoshiro256 rng) => CrowdChants.Boo(ref rng);

    public string Tension(ref Xoshiro256 rng) => CrowdChants.Tension(ref rng);
}
