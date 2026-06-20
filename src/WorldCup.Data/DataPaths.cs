namespace WorldCup.Data;

/// <summary>
/// Resolves the on-disk location of the bundled data files (<c>seed_2026.json</c>,
/// <c>results_2026.json</c>, the API cache). Searches, in order: the output directory's
/// <c>data</c> folder, then walks up from both the binary location and the working directory
/// looking for a <c>data</c> folder — so it works whether run from <c>dotnet run</c>, the test
/// host, or a published binary.
/// </summary>
public static class DataPaths
{
    public const string SeedFileName = "seed_2026.json";
    public const string ResultsFileName = "results_2026.json";
    public const string SquadsFileName = "squads_2026.json";
    public const string ScheduleFileName = "schedule_2026.json";
    public const string ConfigFileName = "config.local.json";

    /// <summary>
    /// Locate the optional local config file (holds secrets such as the API key) by walking up from
    /// the binary location and the working directory. Returns null if not found. Kept out of the data
    /// folder (and the build output) so the secret is not copied around.
    /// </summary>
    public static string? FindConfigFile()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            string? dir = start;
            for (int i = 0; i < 10 && dir is not null; i++)
            {
                string candidate = Path.Combine(dir, ConfigFileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }
        }

        return null;
    }

    /// <summary>The resolved data directory. Throws if it cannot be located.</summary>
    public static string DataDirectory => _dataDir ??= Resolve();

    private static string? _dataDir;

    public static string SeedFile => Path.Combine(DataDirectory, SeedFileName);

    public static string ResultsFile => Path.Combine(DataDirectory, ResultsFileName);

    /// <summary>Optional real squads file (real player names/positions/ratings).</summary>
    public static string SquadsFile => Path.Combine(DataDirectory, SquadsFileName);

    /// <summary>Optional real fixture list (official pairings + dates).</summary>
    public static string ScheduleFile => Path.Combine(DataDirectory, ScheduleFileName);

    /// <summary>Directory used to cache live-API responses.</summary>
    public static string CacheDirectory
    {
        get
        {
            string dir = Path.Combine(DataDirectory, "cache");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string Resolve()
    {
        var candidates = new List<string>();
        void AddChain(string? start)
        {
            var dir = start;
            for (int i = 0; i < 8 && dir is not null; i++)
            {
                candidates.Add(Path.Combine(dir, "data"));
                dir = Directory.GetParent(dir)?.FullName;
            }
        }

        AddChain(AppContext.BaseDirectory);
        AddChain(Directory.GetCurrentDirectory());

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, SeedFileName)))
            {
                return candidate;
            }
        }

        // Fall back to the first plausible location even if the seed isn't there yet.
        string fallback = Path.Combine(AppContext.BaseDirectory, "data");
        return fallback;
    }
}
