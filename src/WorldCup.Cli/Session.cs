using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;

namespace WorldCup.Cli;

/// <summary>Mutable application state for a CLI session.</summary>
public sealed class Session
{
    private readonly ITeamDataProvider _provider;
    private readonly LiveResultsService? _live;

    public Session(ITeamDataProvider provider, string providerDiagnostics, LiveResultsService? live = null)
    {
        _provider = provider;
        _live = live;
        ProviderDiagnostics = providerDiagnostics;
        Data = provider.GetTournamentData();
        PlayedResults = provider.GetPlayedResults();

        // Built-in live pull on startup: refresh results (and fixtures) from the live source when
        // configured, otherwise keep the bundled data.
        LiveDiagnostics = RefreshFromLive();

        Starting = SimulationParameters.CreateStarting();
        Current = Starting.Clone();
        Current.Label = "Current";
    }

    public ITeamDataProvider Provider => _provider;

    public string ProviderDiagnostics { get; }

    /// <summary>Status of the live startup refresh (shown to the user).</summary>
    public string LiveDiagnostics { get; private set; }

    public TournamentData Data { get; private set; }

    public IReadOnlyList<PlayedResult> PlayedResults { get; private set; }

    public SimulationParameters Starting { get; }

    public SimulationParameters Current { get; set; }

    public bool IncludeThirdPlacePlayoff { get; set; } = true;

    /// <summary>Whether a live data source is configured (built-in startup pull is active).</summary>
    public bool LiveConfigured => _live?.IsConfigured == true;

    /// <summary>True when an API key IS set but the live source rejected it (invalid / expired key).</summary>
    public bool LiveKeyUnauthorized => _live?.KeyUnauthorized == true;

    /// <summary>The environment-variable name the API key can be supplied through.</summary>
    public static string ApiKeyEnvVar => LiveResultsService.DefaultApiKeyEnvVar;

    /// <summary>True when "Current" differs from the pristine "Starting" defaults (so the parameter-set
    /// choice is meaningful; otherwise the two are identical and the prompt can be skipped).</summary>
    public bool ParametersEdited =>
        Current.TeamStrengthOverrides.Count > 0 || Current.PlayerAttributeOverrides.Count > 0 ||
        Current.FormationOverrides.Count > 0 || Current.UnavailablePlayers.Count > 0 ||
        Current.PreferredStarters.Count > 0 ||
        System.Text.Json.JsonSerializer.Serialize(Current.Global) != System.Text.Json.JsonSerializer.Serialize(Starting.Global);

    /// <summary>The live score for a fixture currently in play, oriented to the requested home/away,
    /// or null if not configured / not in play. Pass refresh to bypass the cache for a fresh pull.</summary>
    public (int Home, int Away)? TryGetLiveScore(string homeCode, string awayCode, bool refresh = false)
    {
        if (_live is null || !_live.IsConfigured)
        {
            return null;
        }

        if (!_live.TryGetInPlay(Data.Teams, out var live, out _, refresh))
        {
            return null;
        }

        foreach (var m in live)
        {
            if (string.Equals(m.HomeCode, homeCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.AwayCode, awayCode, StringComparison.OrdinalIgnoreCase))
            {
                return (m.HomeGoals, m.AwayGoals);
            }

            if (string.Equals(m.HomeCode, awayCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.AwayCode, homeCode, StringComparison.OrdinalIgnoreCase))
            {
                return (m.AwayGoals, m.HomeGoals); // live match is oriented the other way
            }
        }

        return null;
    }

    public void ResetCurrentToStarting()
    {
        Current = Starting.Clone();
        Current.Label = "Current";
    }

    /// <summary>Re-pull from the live source (or re-read the bundled file when not configured).</summary>
    public void RefreshResults()
    {
        PlayedResults = _provider.GetPlayedResults();
        LiveDiagnostics = RefreshFromLive();
    }

    /// <summary>
    /// Force a fresh pull of rosters, results and fixtures from the live source, bypassing the on-disk
    /// caches (the squads cache is 12h, results 10m) so the very latest data is used before a sim.
    /// </summary>
    public void RefreshLatest()
    {
        PlayedResults = _provider.GetPlayedResults();
        LiveDiagnostics = RefreshFromLive(forceRefresh: true);
    }

    private string RefreshFromLive(bool forceRefresh = false)
    {
        if (_live is null || !_live.IsConfigured)
        {
            return _live?.IsConfigured == false
                ? $"Live updates off (set {LiveResultsService.DefaultApiKeyEnvVar} to enable) — using bundled data."
                : "Using bundled data.";
        }

        var notes = new List<string>();

        // Rosters: keep the researched bundled 2026 squads (they're current and individually rated);
        // only fall back to live API data for any team that lacks a bundled squad.
        if (_live.TryFetchSquads(Data.Teams, out var squads, out string squadDiag, forceRefresh))
        {
            Data = LiveSquadApplier.Apply(Data, squads, out int applied);
            squadDiag = applied > 0
                ? $"Squads: researched 2026 rosters kept; filled {applied} missing roster(s) from football-data.org."
                : "Squads: using the researched 2026 rosters.";
        }

        notes.Add(squadDiag);

        // Results + fixtures.
        if (_live.TryFetch(Data.Teams, out var results, out var fixtures, out string diagnostics, forceRefresh))
        {
            if (results.Count > 0)
            {
                PlayedResults = results;
            }

            // Only adopt live fixtures if they form a complete, valid schedule.
            if (ScheduleGenerator.IsComplete(fixtures, Data.Teams))
            {
                Data = Data with { GroupSchedule = fixtures.OrderBy(f => f.KickoffUtc).ThenBy(f => f.Group).ToList() };
            }
        }

        notes.Add(diagnostics);
        return string.Join(" ", notes);
    }
}
