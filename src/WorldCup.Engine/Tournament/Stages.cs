using WorldCup.Data.Models;

namespace WorldCup.Engine.Tournament;

/// <summary>Helpers for ordering knockout stages.</summary>
public static class Stages
{
    /// <summary>A monotonic rank for "how far" a stage is (group = 0 … final = 5).</summary>
    public static int Rank(Stage stage) => stage switch
    {
        Stage.Group => 0,
        Stage.RoundOf32 => 1,
        Stage.RoundOf16 => 2,
        Stage.QuarterFinal => 3,
        Stage.SemiFinal => 4,
        Stage.ThirdPlacePlayoff => 4,
        Stage.Final => 5,
        _ => -1,
    };

    /// <summary>The knockout stages a team can reach, shallow → deep (excludes the playoff/group).</summary>
    public static readonly IReadOnlyList<Stage> KnockoutLadder = new[]
    {
        Stage.RoundOf32, Stage.RoundOf16, Stage.QuarterFinal, Stage.SemiFinal, Stage.Final,
    };

    public static string DisplayName(Stage stage) => stage switch
    {
        Stage.Group => "Group stage",
        Stage.RoundOf32 => "Round of 32",
        Stage.RoundOf16 => "Round of 16",
        Stage.QuarterFinal => "Quarter-final",
        Stage.SemiFinal => "Semi-final",
        Stage.ThirdPlacePlayoff => "Third-place playoff",
        Stage.Final => "Final",
        _ => stage.ToString(),
    };
}
