using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Stats;

/// <summary>
/// A "crazy stat" record tracker. Implementations observe every match and keep the running extreme.
/// New records can be added cheaply by implementing this interface and registering it in
/// <see cref="RecordTrackers.CreateDefault"/>.
/// </summary>
public interface IRecordTracker
{
    string Category { get; }

    void Observe(MatchResult match, string homeName, string awayName);

    RecordEntry? Result { get; }
}

/// <summary>Factory for the default set of record trackers.</summary>
public static class RecordTrackers
{
    public static List<IRecordTracker> CreateDefault() => new()
    {
        new BestVergazoTracker(),
        new BestSaveTracker(),
        new LongestGoalTracker(),
        new FastestGoalTracker(),
        new LatestGoalTracker(),
        new BiggestWinTracker(),
        new BiggestUpsetTracker(),
        new BiggestComebackTracker(),
        new HighestScoringMatchTracker(),
        new MostGoalsByPlayerTracker(),
        new FastestCardTracker(),
        new FastestRedCardTracker(),
        new LongestShootoutTracker(),
    };

    private static string Match(string home, string away) => $"{home} v {away}";
}

internal sealed class BestVergazoTracker : IRecordTracker
{
    private double _best = -1;
    private string _desc = "—";

    public string Category => "Goal of the tournament (vergazo)";

    public void Observe(MatchResult m, string home, string away)
    {
        foreach (var goal in m.Goals)
        {
            if (goal.Vergazo > _best)
            {
                _best = goal.Vergazo;
                string style = goal.Type switch
                {
                    GoalType.BicycleKick => "bicycle kick",
                    GoalType.LongRange => "long-range",
                    GoalType.FreeKick => "free kick",
                    GoalType.Header => "header",
                    GoalType.Penalty => "penalty",
                    GoalType.OwnGoal => "own goal",
                    _ => "open play",
                };
                _desc = $"{goal.ScorerName} ({goal.TeamCode}) — {goal.Vergazo:0.0}/10 {style}, " +
                        $"{goal.DistanceMeters:0.0}m, {goal.DefendersPassed} beaten, {goal.Minute}', {home} v {away}";
            }
        }
    }

    public RecordEntry? Result => _best < 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class BestSaveTracker : IRecordTracker
{
    private double _best = -1;
    private string _desc = "—";

    public string Category => "Save of the tournament 🧤";

    public void Observe(MatchResult m, string home, string away)
    {
        foreach (var s in m.SaveEvents)
        {
            if (s.Rating > _best)
            {
                _best = s.Rating;
                _desc = $"{s.KeeperName} ({s.TeamCode}) — {s.Rating:0.0}/10, {s.ShotDistanceMeters:F1} m, {s.Minute}', {home} v {away}";
            }
        }
    }

    public RecordEntry? Result => _best < 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class LongestGoalTracker : IRecordTracker
{
    private double _best = -1;
    private string _desc = "—";

    public string Category => "Longest goal (screamer of the tournament)";

    public void Observe(MatchResult m, string home, string away)
    {
        foreach (var goal in m.Goals)
        {
            if (!goal.IsPenalty && goal.DistanceMeters > _best)
            {
                _best = goal.DistanceMeters;
                _desc = $"{goal.ScorerName} ({goal.TeamCode}) — {goal.DistanceMeters:F1} m, {goal.Minute}', {home} v {away}";
            }
        }
    }

    public RecordEntry? Result => _best < 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class FastestGoalTracker : IRecordTracker
{
    private int _best = int.MaxValue;
    private string _desc = "—";

    public string Category => "Fastest goal";

    public void Observe(MatchResult m, string home, string away)
    {
        foreach (var goal in m.Goals)
        {
            if (goal.Minute < _best)
            {
                _best = goal.Minute;
                _desc = $"{goal.ScorerName} ({goal.TeamCode}) — {goal.Minute}', {home} v {away}";
            }
        }
    }

    public RecordEntry? Result => _best == int.MaxValue ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class LatestGoalTracker : IRecordTracker
{
    private int _best = -1;
    private string _desc = "—";

    public string Category => "Latest goal (last-gasp)";

    public void Observe(MatchResult m, string home, string away)
    {
        foreach (var goal in m.Goals)
        {
            if (goal.Minute > _best)
            {
                _best = goal.Minute;
                _desc = $"{goal.ScorerName} ({goal.TeamCode}) — {goal.Minute}', {home} v {away}";
            }
        }
    }

    public RecordEntry? Result => _best < 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class BiggestWinTracker : IRecordTracker
{
    private int _best = -1;
    private string _desc = "—";

    public string Category => "Biggest win (largest margin)";

    public void Observe(MatchResult m, string home, string away)
    {
        int margin = Math.Abs(m.HomeGoals - m.AwayGoals);
        if (margin > _best)
        {
            _best = margin;
            _desc = $"{home} {m.HomeGoals}-{m.AwayGoals} {away} (margin {margin})";
        }
    }

    public RecordEntry? Result => _best < 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class BiggestUpsetTracker : IRecordTracker
{
    private double _best = -1;
    private string _desc = "—";

    public string Category => "Biggest upset (miracle of the tournament)";

    public void Observe(MatchResult m, string home, string away)
    {
        if (m.Upset is not { } u || u.MiracleRating <= _best)
        {
            return;
        }

        _best = u.MiracleRating;
        string winner = m.WinnerCode == m.HomeCode ? home : m.WinnerCode == m.AwayCode ? away : "draw";
        double winnerPre = m.WinnerCode == m.HomeCode ? u.PreMatchHomeWin
            : m.WinnerCode == m.AwayCode ? u.PreMatchAwayWin : u.PreMatchDraw;
        _desc = $"{home} {m.HomeGoals}-{m.AwayGoals} {away} — {winner} won (pre-match {winnerPre * 100:0.#}%), " +
                $"miracle {u.MiracleRating:0.0}/10";
    }

    public RecordEntry? Result => _best < 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class BiggestComebackTracker : IRecordTracker
{
    private int _best = 0;
    private string _desc = "—";

    public string Category => "Biggest comeback (deficit overcome to win)";

    public void Observe(MatchResult m, string home, string away)
    {
        var flow = MatchFlow.Analyze(m);
        int deficit = m.WinnerCode == m.HomeCode ? flow.MaxAwayLead
            : m.WinnerCode == m.AwayCode ? flow.MaxHomeLead
            : 0;

        if (deficit > _best)
        {
            _best = deficit;
            string who = m.WinnerCode == m.HomeCode ? home : away;
            _desc = $"{who} came from {deficit} down to win — {home} {m.HomeGoals}-{m.AwayGoals} {away}";
        }
    }

    public RecordEntry? Result => _best <= 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class HighestScoringMatchTracker : IRecordTracker
{
    private int _best = -1;
    private string _desc = "—";

    public string Category => "Highest-scoring match";

    public void Observe(MatchResult m, string home, string away)
    {
        int total = m.HomeGoals + m.AwayGoals;
        if (total > _best)
        {
            _best = total;
            _desc = $"{home} {m.HomeGoals}-{m.AwayGoals} {away} ({total} goals)";
        }
    }

    public RecordEntry? Result => _best < 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class MostGoalsByPlayerTracker : IRecordTracker
{
    private int _best = -1;
    private string _desc = "—";
    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

    public string Category => "Most goals by a player in a match";

    public void Observe(MatchResult m, string home, string away)
    {
        _counts.Clear();
        foreach (var goal in m.Goals)
        {
            if (goal.IsOwnGoal)
            {
                continue;
            }

            int c = _counts.TryGetValue(goal.ScorerId, out int cur) ? cur + 1 : 1;
            _counts[goal.ScorerId] = c;
            if (c > _best)
            {
                _best = c;
                _desc = $"{goal.ScorerName} ({goal.TeamCode}) — {c} in {home} v {away}";
            }
        }
    }

    public RecordEntry? Result => _best < 0 ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class FastestCardTracker : IRecordTracker
{
    private int _best = int.MaxValue;
    private string _desc = "—";

    public string Category => "Fastest card";

    public void Observe(MatchResult m, string home, string away)
    {
        foreach (var c in m.Cards)
        {
            if (!c.IsRed && c.Minute < _best)
            {
                _best = c.Minute;
                _desc = $"{c.PlayerName} ({c.TeamCode}) — {c.Minute}', {home} v {away}";
            }
        }
    }

    public RecordEntry? Result => _best == int.MaxValue ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class FastestRedCardTracker : IRecordTracker
{
    private int _best = int.MaxValue;
    private string _desc = "—";

    public string Category => "Fastest red card";

    public void Observe(MatchResult m, string home, string away)
    {
        foreach (var c in m.Cards)
        {
            if (c.IsRed && c.Minute < _best)
            {
                _best = c.Minute;
                _desc = $"{c.PlayerName} ({c.TeamCode}) — {c.Minute}', {home} v {away}";
            }
        }
    }

    public RecordEntry? Result => _best == int.MaxValue ? null : new RecordEntry(Category, _desc, _best);
}

internal sealed class LongestShootoutTracker : IRecordTracker
{
    private int _best = -1;
    private string _desc = "—";

    public string Category => "Longest penalty shootout";

    public void Observe(MatchResult m, string home, string away)
    {
        if (m.ShootoutRounds > _best)
        {
            _best = m.ShootoutRounds;
            _desc = $"{home} {m.HomePens}-{m.AwayPens} {away} ({m.ShootoutRounds} rounds)";
        }
    }

    public RecordEntry? Result => _best <= 0 ? null : new RecordEntry(Category, _desc, _best);
}
