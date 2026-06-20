namespace WorldCup.Engine.Tournament;

/// <summary>A single team's group-stage record. Mutable while accumulating, then ranked.</summary>
public sealed class TeamStanding
{
    public TeamStanding(string code)
    {
        Code = code;
    }

    public string Code { get; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }

    /// <summary>Fair-play deduction points from cards (lower is better). 0 in fast mode.</summary>
    public int FairPlayPoints { get; set; }

    public int Points => Won * 3 + Drawn;
    public int GoalDifference => GoalsFor - GoalsAgainst;

    /// <summary>1-based finishing position within the group, assigned after ranking.</summary>
    public int Rank { get; set; }

    /// <summary>Group letter, set by the calculator for convenience.</summary>
    public char Group { get; set; }

    public void ApplyResult(int goalsFor, int goalsAgainst, int fairPlay)
    {
        Played++;
        GoalsFor += goalsFor;
        GoalsAgainst += goalsAgainst;
        FairPlayPoints += fairPlay;
        if (goalsFor > goalsAgainst)
        {
            Won++;
        }
        else if (goalsFor == goalsAgainst)
        {
            Drawn++;
        }
        else
        {
            Lost++;
        }
    }
}

/// <summary>A played group match used to compute standings.</summary>
public readonly record struct GroupMatchOutcome(
    string Home, string Away, int HomeGoals, int AwayGoals, int HomeFairPlay, int AwayFairPlay);
