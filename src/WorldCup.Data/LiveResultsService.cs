using System.Text.Json;
using WorldCup.Data.Models;

namespace WorldCup.Data;

/// <summary>A match currently in play, with its live score, mapped to our team codes.</summary>
public sealed record LiveMatch(string HomeCode, string AwayCode, int HomeGoals, int AwayGoals, string Status);

/// <summary>
/// Pulls live results and fixtures from football-data.org on startup so the data stays current as
/// the real tournament plays out. Reads the API key from an environment variable (never hard-coded),
/// caches the raw response briefly, and is fully defensive: any failure (offline, no key, schema
/// change) returns false so callers fall back to the bundled data.
/// </summary>
public sealed class LiveResultsService
{
    public const string DefaultApiKeyEnvVar = "FOOTBALL_DATA_API_KEY";
    private const string DefaultBaseUrl = "https://api.football-data.org/v4/";
    private const string CompetitionPath = "competitions/WC/matches";
    private const string TeamsPath = "competitions/WC/teams";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SquadsCacheTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(6);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _apiKey;
    private readonly string _baseUrl;

    public LiveResultsService(IHttpClientFactory httpClientFactory, string? apiKeyEnvVar = null, string? baseUrl = null)
    {
        _httpClientFactory = httpClientFactory;

        // Environment variable wins; otherwise read it from the local config file (config.local.json).
        string? key = Environment.GetEnvironmentVariable(apiKeyEnvVar ?? DefaultApiKeyEnvVar);
        if (string.IsNullOrWhiteSpace(key))
        {
            key = AppConfig.FootballDataApiKeyFromFile();
        }

        _apiKey = key;
        _baseUrl = baseUrl ?? DefaultBaseUrl;
    }

    /// <summary>True when an API key is configured (the live pull is only attempted then).</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>True when the most recent live pull was rejected by the API as unauthorized/forbidden
    /// (i.e. the configured key is invalid or expired). False when the key works or none is set.</summary>
    public bool KeyUnauthorized { get; private set; }

    private static bool IsAuthError(Exception ex) =>
        ex is HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden };

    /// <summary>
    /// Fetch matches currently in play (with their live score). Returns false on any problem. Pass
    /// <paramref name="forceRefresh"/> to bypass the on-disk cache for an up-to-the-minute score.
    /// </summary>
    public bool TryGetInPlay(IReadOnlyList<Team> teams, out IReadOnlyList<LiveMatch> live, out string diagnostics, bool forceRefresh = false)
    {
        live = Array.Empty<LiveMatch>();
        if (!IsConfigured)
        {
            diagnostics = "Live updates off.";
            return false;
        }

        try
        {
            var resolve = BuildCodeResolver(teams);
            var list = new List<LiveMatch>();
            using var doc = JsonDocument.Parse(FetchMatches(forceRefresh));
            if (doc.RootElement.TryGetProperty("matches", out var matches) && matches.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in matches.EnumerateArray())
                {
                    string status = m.TryGetProperty("status", out var s) ? s.GetString() ?? string.Empty : string.Empty;
                    if (status is not ("IN_PLAY" or "PAUSED"))
                    {
                        continue;
                    }

                    string? home = ResolveSide(m, "homeTeam", resolve);
                    string? away = ResolveSide(m, "awayTeam", resolve);
                    if (home is null || away is null)
                    {
                        continue;
                    }

                    int hg = 0, ag = 0;
                    if (m.TryGetProperty("score", out var score) && score.TryGetProperty("fullTime", out var ft))
                    {
                        if (ft.TryGetProperty("home", out var h) && h.ValueKind == JsonValueKind.Number) hg = h.GetInt32();
                        if (ft.TryGetProperty("away", out var a) && a.ValueKind == JsonValueKind.Number) ag = a.GetInt32();
                    }

                    list.Add(new LiveMatch(home, away, hg, ag, status));
                }
            }

            live = list;
            diagnostics = $"{list.Count} match(es) in play.";
            return list.Count > 0;
        }
        catch (Exception ex)
        {
            diagnostics = $"Live score unavailable ({ex.Message}).";
            return false;
        }
    }

    /// <summary>
    /// Fetch and map live group-stage results and fixtures. Returns false (with a diagnostic message)
    /// on any problem; on success <paramref name="results"/>/<paramref name="fixtures"/> are populated.
    /// </summary>
    public bool TryFetch(
        IReadOnlyList<Team> teams,
        out IReadOnlyList<PlayedResult> results,
        out IReadOnlyList<GroupFixture> fixtures,
        out string diagnostics,
        bool forceRefresh = false)
    {
        results = Array.Empty<PlayedResult>();
        fixtures = Array.Empty<GroupFixture>();

        if (!IsConfigured)
        {
            diagnostics = $"Live updates off (set {DefaultApiKeyEnvVar} to enable) — using bundled data.";
            return false;
        }

        try
        {
            string json = FetchMatches(forceRefresh);
            (results, fixtures) = Parse(json, teams);
            KeyUnauthorized = false;
            diagnostics = $"Live update from football-data.org: {results.Count} results, {fixtures.Count} fixtures.";
            return results.Count > 0 || fixtures.Count > 0;
        }
        catch (Exception ex)
        {
            if (IsAuthError(ex)) KeyUnauthorized = true;
            diagnostics = $"Live update unavailable ({ex.Message}) — using bundled data.";
            return false;
        }
    }

    /// <summary>
    /// Fetch the real World Cup squads (player names + positions per team) from the competition teams
    /// endpoint, mapped to our team codes. Cached on disk (squads change rarely). Returns false on any
    /// problem so the caller keeps the bundled rosters.
    /// </summary>
    public bool TryFetchSquads(
        IReadOnlyList<Team> teams,
        out IReadOnlyDictionary<string, IReadOnlyList<(string Name, Position Position)>> squads,
        out string diagnostics,
        bool forceRefresh = false)
    {
        squads = new Dictionary<string, IReadOnlyList<(string, Position)>>();
        if (!IsConfigured)
        {
            diagnostics = "Live squads off (no API key).";
            return false;
        }

        try
        {
            var resolve = BuildCodeResolver(teams);
            var result = new Dictionary<string, IReadOnlyList<(string, Position)>>(StringComparer.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(FetchTeams(forceRefresh));
            if (doc.RootElement.TryGetProperty("teams", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in arr.EnumerateArray())
                {
                    string? tla = t.TryGetProperty("tla", out var x) ? x.GetString() : null;
                    string? name = t.TryGetProperty("name", out var n) ? n.GetString() : null;
                    string? code = resolve(tla, name);
                    if (code is null || !t.TryGetProperty("squad", out var sq) || sq.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var players = new List<(string, Position)>();
                    foreach (var pl in sq.EnumerateArray())
                    {
                        string? pos = pl.TryGetProperty("position", out var pp) ? pp.GetString() : null;
                        string? pname = pl.TryGetProperty("name", out var pn) ? pn.GetString() : null;
                        var mapped = MapPosition(pos);
                        if (mapped is not null && !string.IsNullOrWhiteSpace(pname))
                        {
                            players.Add((pname!, mapped.Value));
                        }
                    }

                    // Only adopt a squad that can actually field a side.
                    if (players.Count >= 14)
                    {
                        result[code] = players;
                    }
                }
            }

            squads = result;
            KeyUnauthorized = false;
            diagnostics = $"Live squads from football-data.org: {result.Count} teams.";
            return result.Count > 0;
        }
        catch (Exception ex)
        {
            if (IsAuthError(ex)) KeyUnauthorized = true;
            diagnostics = $"Live squads unavailable ({ex.Message}) — using bundled rosters.";
            return false;
        }
    }

    private static Position? MapPosition(string? apiPosition) => apiPosition switch
    {
        "Goalkeeper" => Position.GK,
        "Defence" => Position.DEF,
        "Midfield" => Position.MID,
        "Offence" => Position.FWD,
        _ => null, // "Coach" and anything unrecognised
    };

    private string FetchTeams(bool forceRefresh)
    {
        string cacheFile = Path.Combine(DataPaths.CacheDirectory, "wc_squads.json");
        if (!forceRefresh && File.Exists(cacheFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile) < SquadsCacheTtl)
        {
            return File.ReadAllText(cacheFile);
        }

        using var client = _httpClientFactory.CreateClient("football-data");
        client.BaseAddress = new Uri(_baseUrl);
        client.Timeout = RequestTimeout;
        client.DefaultRequestHeaders.Add("X-Auth-Token", _apiKey);

        string content = client.GetStringAsync(TeamsPath).GetAwaiter().GetResult();
        File.WriteAllText(cacheFile, content);
        return content;
    }

    private string FetchMatches(bool forceRefresh = false)
    {
        string cacheFile = Path.Combine(DataPaths.CacheDirectory, "wc_matches.json");
        if (!forceRefresh && File.Exists(cacheFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile) < CacheTtl)
        {
            return File.ReadAllText(cacheFile);
        }

        using var client = _httpClientFactory.CreateClient("football-data");
        client.BaseAddress = new Uri(_baseUrl);
        client.Timeout = RequestTimeout;
        client.DefaultRequestHeaders.Add("X-Auth-Token", _apiKey);

        // The console app loads data once at startup, so a synchronous wait is acceptable.
        string content = client.GetStringAsync(CompetitionPath).GetAwaiter().GetResult();
        File.WriteAllText(cacheFile, content);
        return content;
    }

    private static (IReadOnlyList<PlayedResult>, IReadOnlyList<GroupFixture>) Parse(string json, IReadOnlyList<Team> teams)
    {
        var resolve = BuildCodeResolver(teams);
        var results = new List<PlayedResult>();
        var fixtures = new List<GroupFixture>();

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("matches", out var matches) || matches.ValueKind != JsonValueKind.Array)
        {
            return (results, fixtures);
        }

        foreach (var m in matches.EnumerateArray())
        {
            char group = ExtractGroup(m);
            if (group == '\0')
            {
                continue; // not a group-stage match we can place
            }

            string? home = ResolveSide(m, "homeTeam", resolve);
            string? away = ResolveSide(m, "awayTeam", resolve);
            if (home is null || away is null)
            {
                continue;
            }

            int matchday = m.TryGetProperty("matchday", out var md) && md.ValueKind == JsonValueKind.Number ? md.GetInt32() : 1;
            DateTime kickoff = default;
            if (m.TryGetProperty("utcDate", out var d) && d.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(d.GetString(), out var parsed))
            {
                kickoff = parsed.ToUniversalTime();
            }

            fixtures.Add(new GroupFixture(group, matchday, home, away, kickoff));

            string status = m.TryGetProperty("status", out var s) ? s.GetString() ?? string.Empty : string.Empty;
            if (status == "FINISHED" &&
                m.TryGetProperty("score", out var score) &&
                score.TryGetProperty("fullTime", out var ft) &&
                ft.TryGetProperty("home", out var hg) && hg.ValueKind == JsonValueKind.Number &&
                ft.TryGetProperty("away", out var ag) && ag.ValueKind == JsonValueKind.Number)
            {
                results.Add(new PlayedResult(home, away, hg.GetInt32(), ag.GetInt32()));
            }
        }

        return (results, fixtures);
    }

    private static char ExtractGroup(JsonElement match)
    {
        if (!match.TryGetProperty("group", out var g) || g.ValueKind != JsonValueKind.String)
        {
            return '\0';
        }

        string text = g.GetString() ?? string.Empty;
        // Accept "GROUP_K", "Group K", etc. Take the last A–L letter.
        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = char.ToUpperInvariant(text[i]);
            if (c is >= 'A' and <= 'L')
            {
                return c;
            }
        }

        return '\0';
    }

    private static string? ResolveSide(JsonElement match, string side, Func<string?, string?, string?> resolve)
    {
        if (!match.TryGetProperty(side, out var team) || team.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? tla = team.TryGetProperty("tla", out var t) ? t.GetString() : null;
        string? name = team.TryGetProperty("name", out var n) ? n.GetString() : null;
        return resolve(tla, name);
    }

    /// <summary>Build a resolver from football-data.org TLA/name to our 3-letter codes.</summary>
    private static Func<string?, string?, string?> BuildCodeResolver(IReadOnlyList<Team> teams)
    {
        var byCode = teams.Select(t => t.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var byName = teams.ToDictionary(t => Normalize(t.Name), t => t.Code, StringComparer.Ordinal);

        // Aliases for names that differ from our seed labels.
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Normalize("Czech Republic")] = "CZE",
            [Normalize("Korea Republic")] = "KOR",
            [Normalize("South Korea")] = "KOR",
            [Normalize("IR Iran")] = "IRN",
            [Normalize("United States")] = "USA",
            [Normalize("USA")] = "USA",
            [Normalize("Cote dIvoire")] = "CIV",
            [Normalize("Ivory Coast")] = "CIV",
            [Normalize("Turkey")] = "TUR",
            [Normalize("Turkiye")] = "TUR",
            [Normalize("Saudi Arabia")] = "KSA",
            [Normalize("DR Congo")] = "COD",
            [Normalize("Congo DR")] = "COD",
            [Normalize("Cabo Verde")] = "CPV",
            [Normalize("Cape Verde")] = "CPV",
            [Normalize("Bosnia and Herzegovina")] = "BIH",
            [Normalize("Curacao")] = "CUW",
        };

        return (tla, name) =>
        {
            if (tla is not null && byCode.Contains(tla))
            {
                return tla.ToUpperInvariant();
            }

            if (name is not null)
            {
                string norm = Normalize(name);
                if (byName.TryGetValue(norm, out var code))
                {
                    return code;
                }

                if (aliases.TryGetValue(norm, out var alias))
                {
                    return alias;
                }
            }

            return null;
        };
    }

    private static string Normalize(string text)
    {
        Span<char> buffer = stackalloc char[text.Length];
        int n = 0;
        foreach (char c in text)
        {
            char lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z')
            {
                buffer[n++] = lower;
            }
            else if (lower is >= '0' and <= '9')
            {
                buffer[n++] = lower;
            }
            // strip accents/spaces/punctuation by mapping common accented vowels
            else
            {
                char folded = lower switch
                {
                    'á' or 'à' or 'â' or 'ä' or 'ã' => 'a',
                    'é' or 'è' or 'ê' or 'ë' => 'e',
                    'í' or 'ì' or 'î' or 'ï' => 'i',
                    'ó' or 'ò' or 'ô' or 'ö' or 'õ' => 'o',
                    'ú' or 'ù' or 'û' or 'ü' => 'u',
                    'ç' => 'c',
                    _ => '\0',
                };
                if (folded != '\0')
                {
                    buffer[n++] = folded;
                }
            }
        }

        return new string(buffer[..n]);
    }
}
