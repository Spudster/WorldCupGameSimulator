namespace WorldCup.Data.Models;

/// <summary>How a Round-of-32 slot is filled from the group stage.</summary>
public enum SlotSpecKind
{
    /// <summary>The winner (1st place) of <see cref="SlotSpec.Group"/>.</summary>
    Winner,

    /// <summary>The runner-up (2nd place) of <see cref="SlotSpec.Group"/>.</summary>
    RunnerUp,

    /// <summary>
    /// The best-third-placed team assigned (via the eligibility table) to face the winner of
    /// <see cref="SlotSpec.Group"/>. The source group depends on which eight groups produce
    /// qualifying third-placed teams.
    /// </summary>
    ThirdForWinner,
}

/// <summary>A reference to a group-stage finishing position.</summary>
public readonly record struct SlotSpec(SlotSpecKind Kind, char Group)
{
    public static SlotSpec Winner(char g) => new(SlotSpecKind.Winner, g);
    public static SlotSpec RunnerUp(char g) => new(SlotSpecKind.RunnerUp, g);
    public static SlotSpec ThirdForWinner(char g) => new(SlotSpecKind.ThirdForWinner, g);

    public override string ToString() => Kind switch
    {
        SlotSpecKind.Winner => $"1{Group}",
        SlotSpecKind.RunnerUp => $"2{Group}",
        SlotSpecKind.ThirdForWinner => "3rd",
        _ => "?",
    };
}

/// <summary>Where one side of a knockout match comes from.</summary>
public enum FeederKind
{
    /// <summary>A group-stage finishing position (R32 only).</summary>
    GroupSlot,

    /// <summary>The winner of an earlier knockout match (by id).</summary>
    MatchWinner,

    /// <summary>The loser of an earlier knockout match (third-place playoff).</summary>
    MatchLoser,
}

/// <summary>A reference to one side of a knockout match.</summary>
public readonly record struct Feeder(FeederKind Kind, SlotSpec Slot, int MatchId)
{
    public static Feeder FromSlot(SlotSpec slot) => new(FeederKind.GroupSlot, slot, 0);
    public static Feeder WinnerOf(int matchId) => new(FeederKind.MatchWinner, default, matchId);
    public static Feeder LoserOf(int matchId) => new(FeederKind.MatchLoser, default, matchId);
}

/// <summary>
/// One knockout match definition. Matches are resolved in ascending <see cref="Id"/> order, so a
/// later match's feeders always reference already-decided matches.
/// </summary>
/// <param name="Id">FIFA match number (73–104).</param>
/// <param name="Stage">Which knockout round.</param>
/// <param name="Top">Source of the first team.</param>
/// <param name="Bottom">Source of the second team.</param>
/// <param name="Label">Short display label, e.g. "R32-1".</param>
public sealed record KnockoutMatchDef(int Id, Stage Stage, Feeder Top, Feeder Bottom, string Label);

/// <summary>
/// The fixed, FIFA-defined knockout bracket structure for the 48-team format: the explicit match
/// tree plus the data needed to assign third-placed teams to their R32 slots.
/// </summary>
/// <param name="Matches">All knockout matches (R32 → Final, plus the third-place playoff) in id order.</param>
/// <param name="ThirdPlaceWinnerGroups">The eight group-winner letters whose R32 opponent is a third-placed team.</param>
/// <param name="ThirdPlaceEligibleGroups">
/// For each winner group in <see cref="ThirdPlaceWinnerGroups"/>, the set of groups whose
/// third-placed team is eligible to be assigned there (FIFA's eligibility constraint).
/// </param>
public sealed record BracketDefinition(
    IReadOnlyList<KnockoutMatchDef> Matches,
    IReadOnlyList<char> ThirdPlaceWinnerGroups,
    IReadOnlyDictionary<char, IReadOnlyList<char>> ThirdPlaceEligibleGroups);
