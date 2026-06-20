namespace WorldCup.Engine.Tournament.Fast;

/// <summary>
/// Thread-local accumulator of fast-mode tournament outcomes. One per worker; merged at the end.
/// All per-team arrays are indexed by the model's team index.
/// </summary>
internal sealed class TournamentAggregator
{
    public TournamentAggregator(int teamCount)
    {
        TeamCount = teamCount;
        Champion = new long[teamCount];
        ReachedFinal = new long[teamCount];
        ReachedSemi = new long[teamCount];
        ReachedQuarter = new long[teamCount];
        ReachedR16 = new long[teamCount];
        ReachedR32 = new long[teamCount];
        TopGroup = new long[teamCount];
        GroupPointsSum = new double[teamCount];
        FinalMatchups = new Dictionary<(int, int), long>();
    }

    public int TeamCount { get; }
    public long Tournaments { get; set; }

    public long[] Champion { get; }
    public long[] ReachedFinal { get; }
    public long[] ReachedSemi { get; }
    public long[] ReachedQuarter { get; }
    public long[] ReachedR16 { get; }
    public long[] ReachedR32 { get; }
    public long[] TopGroup { get; }
    public double[] GroupPointsSum { get; }
    public Dictionary<(int, int), long> FinalMatchups { get; }

    public void Merge(TournamentAggregator other)
    {
        Tournaments += other.Tournaments;
        for (int i = 0; i < TeamCount; i++)
        {
            Champion[i] += other.Champion[i];
            ReachedFinal[i] += other.ReachedFinal[i];
            ReachedSemi[i] += other.ReachedSemi[i];
            ReachedQuarter[i] += other.ReachedQuarter[i];
            ReachedR16[i] += other.ReachedR16[i];
            ReachedR32[i] += other.ReachedR32[i];
            TopGroup[i] += other.TopGroup[i];
            GroupPointsSum[i] += other.GroupPointsSum[i];
        }

        foreach (var (k, v) in other.FinalMatchups)
        {
            FinalMatchups[k] = FinalMatchups.TryGetValue(k, out long cur) ? cur + v : v;
        }
    }
}
