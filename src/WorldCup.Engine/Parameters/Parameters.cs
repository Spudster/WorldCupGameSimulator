using System.Text.Json;
using System.Text.Json.Serialization;
using WorldCup.Data;
using WorldCup.Data.Models;

namespace WorldCup.Engine.Parameters;

/// <summary>
/// The full tunable parameter set for a simulation: the <see cref="GlobalParameters"/> plus
/// optional per-team strength overrides and per-player attribute overrides. Effective values
/// fall back to the seed data when no override is present.
/// <para>
/// "Starting" parameters are the pristine defaults (no overrides). "Current" parameters are a
/// mutable working copy the user edits at runtime; they can be saved to / loaded from disk.
/// </para>
/// </summary>
public sealed class SimulationParameters
{
    public GlobalParameters Global { get; set; } = new();

    /// <summary>Team code → overridden strength (0–100).</summary>
    public Dictionary<string, double> TeamStrengthOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Player id → overridden attributes.</summary>
    public Dictionary<string, PlayerAttributes> PlayerAttributeOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Team code → formation override (e.g. "4-4-2"). Falls back to the global default.</summary>
    public Dictionary<string, string> FormationOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Player ids that are unavailable (injured/suspended/rested) — excluded from selection.</summary>
    public HashSet<string> UnavailablePlayers { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Team code → a recent-form strength delta (points on the 0–100 scale), derived from already-played
    /// results by <c>FormModel</c>: positive when a team has over-performed its rating (e.g. an underdog
    /// holding a favourite), negative when it has under-performed. Empty by default — so pre-tournament
    /// odds and the calibration are unchanged — and populated only for "current state" forward
    /// predictions, so a team's last game carries into its next. Scaled by <see cref="GlobalParameters.FormWeight"/>.
    /// </summary>
    public Dictionary<string, double> TeamFormDeltas { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Player ids forced to the front of their position when picking the XI — used to correct the
    /// projected starter (e.g. the real first-choice goalkeeper) when the roster's listing order
    /// doesn't reflect who actually starts. Honoured by <see cref="LineupProjector"/> in both the
    /// displayed line-up and the simulated one.
    /// </summary>
    public HashSet<string> PreferredStarters { get; set; } = new(StringComparer.Ordinal);

    /// <summary>A short label shown in reports ("Starting", "Current", or a loaded file name).</summary>
    [JsonIgnore]
    public string Label { get; set; } = "Starting";

    /// <summary>
    /// Effective strength for a team: the FIFA-ranking strength (or override), nudged by how the
    /// <em>available</em> starting XI compares to the team's full-strength XI. The adjustment is 0 for a
    /// full, unedited squad (so calibration and the baseline odds are unchanged), and turns negative
    /// when key players are unavailable / weakened — which is how injuries and suspensions move the odds.
    /// </summary>
    public double EffectiveStrength(Team team)
    {
        double baseStrength = TeamStrengthOverrides.TryGetValue(team.Code, out double s) ? s : team.Strength;

        // Recent-form nudge from already-played results (0 unless populated for a current-state forecast).
        double formDelta = Global.FormWeight > 0 && TeamFormDeltas.TryGetValue(team.Code, out double fd)
            ? Global.FormWeight * fd
            : 0.0;

        // Skip the (allocating) XI projection entirely unless the squad has actually been altered.
        if (Global.SquadQualityWeight <= 0 ||
            (FormationOverrides.Count == 0 && UnavailablePlayers.Count == 0 && PlayerAttributeOverrides.Count == 0))
        {
            return formDelta != 0.0 ? Math.Max(5.0, baseStrength + formDelta) : baseStrength;
        }

        double delta = SquadRating(team, effective: true) - SquadRating(team, effective: false);
        return Math.Max(5.0, baseStrength + formDelta + Global.SquadQualityWeight * delta); // floor avoids a degenerate 0/negative strength
    }

    /// <summary>Average recovered overall rating of a team's starting XI (the squad-quality signal).</summary>
    private double SquadRating(Team team, bool effective)
    {
        var xi = LineupProjector.Project(team, effective ? Formation(team) : Global.DefaultFormation,
            effective ? IsAvailable : null, effective ? PreferredStarters : null).Xi;
        if (xi.Count == 0)
        {
            return 0;
        }

        double sum = 0;
        foreach (var pl in xi)
        {
            sum += PlayerRating(pl.Position, effective ? EffectiveAttributes(pl) : pl.Attributes);
        }

        return sum / xi.Count;
    }

    /// <summary>Recover an approximate 0–100 overall rating from a player's attributes (inverse of the
    /// attribute-building formula), used to score squad quality.</summary>
    private static double PlayerRating(Position pos, PlayerAttributes a) => pos switch
    {
        Position.GK => (a.Goalkeeping - 6) / 0.92,
        Position.FWD => (a.Finishing - 8) / 0.90,
        Position.MID => (a.Creativity - 5) / 0.88,
        Position.DEF => ((a.Creativity / 0.50) + (a.Finishing / 0.42)) / 2.0,
        _ => 50,
    };

    /// <summary>Effective attributes for a player (override if present, else the seed value).</summary>
    public PlayerAttributes EffectiveAttributes(Player player) =>
        PlayerAttributeOverrides.TryGetValue(player.Id, out var a) ? a : player.Attributes;

    /// <summary>The formation a team will line up in (override, else the global default).</summary>
    public string Formation(Team team) =>
        FormationOverrides.TryGetValue(team.Code, out var f) ? f : Global.DefaultFormation;

    /// <summary>Whether a player is available for selection (not injured/suspended/rested).</summary>
    public bool IsAvailable(Player player) => !UnavailablePlayers.Contains(player.Id);

    /// <summary>The pristine starting parameter set.</summary>
    public static SimulationParameters CreateStarting() => new() { Label = "Starting" };

    /// <summary>Deep clone (used to fork "current" from "starting").</summary>
    public SimulationParameters Clone() => new()
    {
        Global = Global.Clone(),
        TeamStrengthOverrides = new Dictionary<string, double>(TeamStrengthOverrides, StringComparer.OrdinalIgnoreCase),
        PlayerAttributeOverrides = new Dictionary<string, PlayerAttributes>(PlayerAttributeOverrides, StringComparer.OrdinalIgnoreCase),
        FormationOverrides = new Dictionary<string, string>(FormationOverrides, StringComparer.OrdinalIgnoreCase),
        UnavailablePlayers = new HashSet<string>(UnavailablePlayers, StringComparer.Ordinal),
        PreferredStarters = new HashSet<string>(PreferredStarters, StringComparer.Ordinal),
        TeamFormDeltas = new Dictionary<string, double>(TeamFormDeltas, StringComparer.OrdinalIgnoreCase),
        Label = Label,
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Persist this parameter set to a JSON file.</summary>
    public void Save(string path)
    {
        string json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>Load a parameter set from a JSON file.</summary>
    public static SimulationParameters Load(string path)
    {
        string json = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<SimulationParameters>(json, JsonOptions)
            ?? throw new InvalidDataException($"Could not parse parameters from '{path}'.");
        loaded.Label = Path.GetFileNameWithoutExtension(path);
        return loaded;
    }
}
