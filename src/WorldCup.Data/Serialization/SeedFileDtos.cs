using System.Text.Json.Serialization;

namespace WorldCup.Data.Serialization;

/// <summary>Root DTO for <c>seed_2026.json</c>.</summary>
internal sealed class SeedFile
{
    public SeedMetadataDto Metadata { get; set; } = new();
    public List<SeedTeamDto> Teams { get; set; } = new();
}

internal sealed class SeedMetadataDto
{
    public string Name { get; set; } = "FIFA World Cup 2026";
    public List<string> Hosts { get; set; } = new();
    public DateTime FinalDateUtc { get; set; }
    public string SourceNote { get; set; } = string.Empty;
}

internal sealed class SeedTeamDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Confederation { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public int Pot { get; set; }
    public double Strength { get; set; }
    public int FifaRanking { get; set; }
}

/// <summary>Root DTO for the optional <c>squads_2026.json</c> (real player data).</summary>
internal sealed class SquadsFile
{
    public string Note { get; set; } = string.Empty;
    public Dictionary<string, List<SquadPlayerDto>> Squads { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class SquadPlayerDto
{
    public string Name { get; set; } = string.Empty;
    public string Pos { get; set; } = string.Empty;
    public int Rating { get; set; }
}

/// <summary>Root DTO for the optional <c>schedule_2026.json</c> (official fixtures).</summary>
internal sealed class ScheduleFile
{
    public string Note { get; set; } = string.Empty;
    public DateTime? UpdatedUtc { get; set; }
    public List<ScheduleFixtureDto> Fixtures { get; set; } = new();
}

internal sealed class ScheduleFixtureDto
{
    public string Group { get; set; } = string.Empty;
    public int Matchday { get; set; }
    public string Home { get; set; } = string.Empty;
    public string Away { get; set; } = string.Empty;
    public DateTime KickoffUtc { get; set; }
}

/// <summary>Root DTO for <c>results_2026.json</c> (already-played matches).</summary>
internal sealed class ResultsFile
{
    public string Note { get; set; } = string.Empty;
    public DateTime? AsOfUtc { get; set; }
    public List<ResultDto> Results { get; set; } = new();
}

internal sealed class ResultDto
{
    public string Home { get; set; } = string.Empty;
    public string Away { get; set; } = string.Empty;
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
}

/// <summary>Source-generated JSON context for fast, trim-safe (de)serialization.</summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SeedFile))]
[JsonSerializable(typeof(ResultsFile))]
[JsonSerializable(typeof(SquadsFile))]
[JsonSerializable(typeof(ScheduleFile))]
internal partial class SeedJsonContext : JsonSerializerContext
{
}
