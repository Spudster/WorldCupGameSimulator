using System.Text.Json;
using WorldCup.Data.Models;

namespace WorldCup.Data.Serialization;

/// <summary>
/// Loads <c>seed_2026.json</c> into a <see cref="TournamentData"/>: deserialises the team list, then
/// for each team either builds a REAL squad from <c>squads_2026.json</c> (if present) or a synthetic
/// one. Uses the official fixtures from <c>schedule_2026.json</c> (if present) or a generated
/// round-robin otherwise, and attaches the official knockout bracket. Also loads
/// <c>results_2026.json</c> into <see cref="PlayedResult"/>s.
/// </summary>
public static class SeedDataLoader
{
    public static TournamentData LoadTournament(string seedPath, string? squadsPath = null, string? schedulePath = null)
    {
        string json = File.ReadAllText(seedPath);
        var seed = JsonSerializer.Deserialize(json, SeedJsonContext.Default.SeedFile)
            ?? throw new InvalidDataException($"Could not parse seed file '{seedPath}'.");

        string dir = Path.GetDirectoryName(Path.GetFullPath(seedPath)) ?? Directory.GetCurrentDirectory();
        var squads = LoadSquads(squadsPath ?? Path.Combine(dir, DataPaths.SquadsFileName));

        var teams = new List<Team>(seed.Teams.Count);
        foreach (var t in seed.Teams)
        {
            if (!Enum.TryParse<Confederation>(t.Confederation, ignoreCase: true, out var conf))
            {
                throw new InvalidDataException($"Unknown confederation '{t.Confederation}' for team {t.Code}.");
            }

            char group = char.ToUpperInvariant(t.Group.Trim()[0]);

            IReadOnlyList<Player> squad;
            bool synthetic;
            if (squads is not null && squads.TryGetValue(t.Code, out var dtoList) && dtoList.Count > 0)
            {
                var triples = dtoList
                    .Select(d => (d.Name, ParsePosition(d.Pos, t.Code), d.Rating))
                    .ToList();
                squad = RealSquadBuilder.Build(t.Code, triples);
                synthetic = false;
            }
            else
            {
                squad = SyntheticSquadGenerator.Generate(t.Code, t.Strength);
                synthetic = true;
            }

            teams.Add(new Team(
                Code: t.Code,
                Name: t.Name,
                Confederation: conf,
                Group: group,
                Pot: t.Pot,
                Strength: t.Strength,
                FifaRanking: t.FifaRanking,
                Squad: squad,
                IsSyntheticSquad: synthetic));
        }

        var metadata = new TournamentMetadata(
            seed.Metadata.Name,
            seed.Metadata.Hosts,
            seed.Metadata.FinalDateUtc,
            seed.Metadata.SourceNote);

        var schedule = LoadSchedule(schedulePath ?? Path.Combine(dir, DataPaths.ScheduleFileName), teams)
            ?? ScheduleGenerator.Build(teams);
        var bracket = OfficialBracket2026.Build();

        return new TournamentData(metadata, teams, schedule, bracket);
    }

    public static IReadOnlyList<PlayedResult> LoadResults(string resultsPath)
    {
        if (!File.Exists(resultsPath))
        {
            return Array.Empty<PlayedResult>();
        }

        string json = File.ReadAllText(resultsPath);
        var file = JsonSerializer.Deserialize(json, SeedJsonContext.Default.ResultsFile);
        if (file is null)
        {
            return Array.Empty<PlayedResult>();
        }

        return file.Results
            .Select(r => new PlayedResult(r.Home, r.Away, r.HomeGoals, r.AwayGoals))
            .ToList();
    }

    private static Dictionary<string, List<SquadPlayerDto>>? LoadSquads(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var file = JsonSerializer.Deserialize(File.ReadAllText(path), SeedJsonContext.Default.SquadsFile);
        return file?.Squads;
    }

    /// <summary>
    /// Load the official fixtures and validate they form a complete single round-robin (6 per group,
    /// 3 per team, valid codes). Returns null on any problem so the caller falls back to the
    /// generated schedule.
    /// </summary>
    private static IReadOnlyList<GroupFixture>? LoadSchedule(string path, IReadOnlyList<Team> teams)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        ScheduleFile? file;
        try
        {
            file = JsonSerializer.Deserialize(File.ReadAllText(path), SeedJsonContext.Default.ScheduleFile);
        }
        catch (JsonException)
        {
            return null;
        }

        if (file is null || file.Fixtures.Count == 0)
        {
            return null;
        }

        var byCode = teams.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        var fixtures = new List<GroupFixture>(file.Fixtures.Count);
        foreach (var f in file.Fixtures)
        {
            if (!byCode.TryGetValue(f.Home, out var home) || !byCode.TryGetValue(f.Away, out var away))
            {
                return null; // unknown code — fall back
            }

            char group = char.ToUpperInvariant(f.Group.Trim()[0]);
            if (home.Group != group || away.Group != group)
            {
                return null; // inconsistent group — fall back
            }

            fixtures.Add(new GroupFixture(group, f.Matchday, f.Home.ToUpperInvariant(), f.Away.ToUpperInvariant(), f.KickoffUtc));
        }

        // Validate completeness: every team plays exactly 3, and each group has 6 fixtures.
        var appearances = teams.ToDictionary(t => t.Code, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var f in fixtures)
        {
            appearances[f.HomeCode]++;
            appearances[f.AwayCode]++;
        }

        if (appearances.Values.Any(c => c != 3))
        {
            return null;
        }

        foreach (var group in teams.Select(t => t.Group).Distinct())
        {
            if (fixtures.Count(f => f.Group == group) != 6)
            {
                return null;
            }
        }

        return fixtures.OrderBy(f => f.KickoffUtc).ThenBy(f => f.Group).ToList();
    }

    private static Position ParsePosition(string pos, string teamCode)
    {
        if (Enum.TryParse<Position>(pos.Trim(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidDataException($"Unknown player position '{pos}' for team {teamCode}.");
    }
}
