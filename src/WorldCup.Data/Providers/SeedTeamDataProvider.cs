using WorldCup.Data.Models;
using WorldCup.Data.Serialization;

namespace WorldCup.Data.Providers;

/// <summary>
/// The default, always-available data provider. Reads the bundled offline seed files
/// (<c>seed_2026.json</c> + <c>results_2026.json</c>) so the app runs fully offline out of the box.
/// </summary>
public sealed class SeedTeamDataProvider : ITeamDataProvider
{
    private readonly string _seedPath;
    private readonly string _resultsPath;
    private TournamentData? _cached;

    public SeedTeamDataProvider(string? seedPath = null, string? resultsPath = null)
    {
        _seedPath = seedPath ?? DataPaths.SeedFile;
        _resultsPath = resultsPath ?? DataPaths.ResultsFile;
    }

    public string Name => "Offline seed (seed_2026.json)";

    public bool IsAvailable => File.Exists(_seedPath);

    public TournamentData GetTournamentData() => _cached ??= SeedDataLoader.LoadTournament(_seedPath);

    public IReadOnlyList<PlayedResult> GetPlayedResults() => SeedDataLoader.LoadResults(_resultsPath);
}
