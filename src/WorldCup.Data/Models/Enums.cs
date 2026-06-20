namespace WorldCup.Data.Models;

/// <summary>A football confederation.</summary>
public enum Confederation
{
    UEFA,
    CONMEBOL,
    CONCACAF,
    CAF,
    AFC,
    OFC,
}

/// <summary>A player's primary playing position.</summary>
public enum Position
{
    GK,
    DEF,
    MID,
    FWD,
}

/// <summary>A stage of the tournament.</summary>
public enum Stage
{
    Group,
    RoundOf32,
    RoundOf16,
    QuarterFinal,
    SemiFinal,
    ThirdPlacePlayoff,
    Final,
}
