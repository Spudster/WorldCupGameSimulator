namespace WorldCup.Data.Models;

/// <summary>
/// A scheduled group-stage match. The group stage is a single round-robin within each
/// group of four (3 matches per team, 6 matches per group, 72 total).
/// </summary>
/// <param name="Group">Group letter A–L.</param>
/// <param name="Matchday">1, 2 or 3.</param>
/// <param name="HomeCode">3-letter code of the nominal home team.</param>
/// <param name="AwayCode">3-letter code of the nominal away team.</param>
/// <param name="KickoffUtc">Scheduled kickoff (UTC) — used to identify the "current" fixture.</param>
public sealed record GroupFixture(
    char Group,
    int Matchday,
    string HomeCode,
    string AwayCode,
    DateTime KickoffUtc);

/// <summary>
/// The final score of a match that has actually been played in the real tournament.
/// Used by "current state" mode to lock already-played results. <see cref="HomeCode"/>/<see cref="AwayCode"/>
/// must match the orientation of the corresponding <see cref="GroupFixture"/>.
/// </summary>
public sealed record PlayedResult(
    string HomeCode,
    string AwayCode,
    int HomeGoals,
    int AwayGoals)
{
    public string WinnerCode => HomeGoals > AwayGoals ? HomeCode
        : AwayGoals > HomeGoals ? AwayCode
        : string.Empty;
}
