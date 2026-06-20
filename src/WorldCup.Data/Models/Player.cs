namespace WorldCup.Data.Models;

/// <summary>
/// Per-player attributes (each on a 0–100 scale) that drive event probabilities
/// in the detailed simulation. Higher is "more / better" for every attribute
/// EXCEPT discipline, where a higher value means a <em>better-behaved</em> player
/// (i.e. less card-prone — the model inverts it when computing foul/card rates).
/// </summary>
/// <param name="Finishing">Likelihood/quality of converting chances into goals.</param>
/// <param name="Creativity">Tendency to create chances and register assists.</param>
/// <param name="Discipline">Higher = cleaner player (fewer cards). Inverted in the card model.</param>
/// <param name="InjuryProneness">Higher = more likely to pick up an injury.</param>
/// <param name="Goalkeeping">Shot-stopping quality (only meaningful for GKs).</param>
public readonly record struct PlayerAttributes(
    double Finishing,
    double Creativity,
    double Discipline,
    double InjuryProneness,
    double Goalkeeping)
{
    /// <summary>Clamp all attributes into the valid 0–100 range.</summary>
    public PlayerAttributes Clamped() => new(
        Math.Clamp(Finishing, 0, 100),
        Math.Clamp(Creativity, 0, 100),
        Math.Clamp(Discipline, 0, 100),
        Math.Clamp(InjuryProneness, 0, 100),
        Math.Clamp(Goalkeeping, 0, 100));
}

/// <summary>
/// A single squad player. Immutable; identified by <see cref="Id"/> which is unique
/// within the whole tournament (so stat trackers can key on it without ambiguity).
/// </summary>
/// <param name="Id">Tournament-unique id, e.g. "ARG-07".</param>
/// <param name="Name">Display name.</param>
/// <param name="TeamCode">3-letter code of the player's team.</param>
/// <param name="Position">Primary position.</param>
/// <param name="Attributes">Event-probability attributes.</param>
/// <param name="IsSynthetic">True when this player was generated rather than sourced from real data.</param>
public sealed record Player(
    string Id,
    string Name,
    string TeamCode,
    Position Position,
    PlayerAttributes Attributes,
    bool IsSynthetic);
