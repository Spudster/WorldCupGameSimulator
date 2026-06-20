using WorldCup.Data.Models;

namespace WorldCup.Engine.Tournament;

/// <summary>
/// Resolves knockout-match <see cref="Feeder"/>s to concrete team codes as the bracket is played.
/// Group winners/runners-up and the third-place assignment are fixed up front; match
/// winners/losers are recorded as each match is decided.
/// </summary>
public sealed class KnockoutResolver
{
    private readonly IReadOnlyDictionary<char, string> _winners;
    private readonly IReadOnlyDictionary<char, string> _runnersUp;
    private readonly IReadOnlyDictionary<char, string> _thirdForWinnerGroup;
    private readonly Dictionary<int, string> _matchWinner = new();
    private readonly Dictionary<int, string> _matchLoser = new();

    public KnockoutResolver(
        IReadOnlyDictionary<char, string> winners,
        IReadOnlyDictionary<char, string> runnersUp,
        IReadOnlyDictionary<char, string> thirdForWinnerGroup)
    {
        _winners = winners;
        _runnersUp = runnersUp;
        _thirdForWinnerGroup = thirdForWinnerGroup;
    }

    public string Resolve(Feeder feeder) => feeder.Kind switch
    {
        FeederKind.MatchWinner => _matchWinner[feeder.MatchId],
        FeederKind.MatchLoser => _matchLoser[feeder.MatchId],
        FeederKind.GroupSlot => feeder.Slot.Kind switch
        {
            SlotSpecKind.Winner => _winners[feeder.Slot.Group],
            SlotSpecKind.RunnerUp => _runnersUp[feeder.Slot.Group],
            SlotSpecKind.ThirdForWinner => _thirdForWinnerGroup[feeder.Slot.Group],
            _ => throw new InvalidOperationException("Unknown slot kind."),
        },
        _ => throw new InvalidOperationException("Unknown feeder kind."),
    };

    public void Record(int matchId, string winnerCode, string loserCode)
    {
        _matchWinner[matchId] = winnerCode;
        _matchLoser[matchId] = loserCode;
    }
}
