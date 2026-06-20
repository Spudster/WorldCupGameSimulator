using System.Text.Json;

namespace WorldCup.Engine.Tournament;

/// <summary>
/// Saves and reloads a complete <see cref="TournamentResult"/> as JSON, so a simulated tournament can
/// be revisited (re-printed, re-exported) later without re-running it. The snapshot is the full result —
/// bracket, standings and (in detailed mode) every box score — so it reloads byte-for-byte identical.
/// </summary>
public static class TournamentSnapshot
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Save(TournamentResult result, string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(result, Options));
    }

    public static TournamentResult Load(string path)
    {
        return JsonSerializer.Deserialize<TournamentResult>(File.ReadAllText(path), Options)
            ?? throw new InvalidOperationException("The file is not a valid tournament snapshot.");
    }
}
