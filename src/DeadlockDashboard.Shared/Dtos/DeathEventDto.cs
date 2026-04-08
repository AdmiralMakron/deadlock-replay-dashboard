namespace DeadlockDashboard.Shared.Dtos;

/// <summary>
/// A single death event during a match.
/// </summary>
public class DeathEventDto
{
    /// <summary>Game time in seconds when the death occurred.</summary>
    public double GameTimeSeconds { get; set; }

    /// <summary>Steam ID of the player who died.</summary>
    public ulong VictimSteamId { get; set; }

    /// <summary>Name of the player who died.</summary>
    public string VictimName { get; set; } = string.Empty;

    /// <summary>Steam ID of the killer.</summary>
    public ulong KillerSteamId { get; set; }

    /// <summary>Name of the killer.</summary>
    public string KillerName { get; set; } = string.Empty;

    /// <summary>Weapon or ability used for the kill.</summary>
    public string Weapon { get; set; } = string.Empty;

    /// <summary>Whether the kill was a headshot.</summary>
    public bool Headshot { get; set; }

    /// <summary>Steam IDs of players who assisted.</summary>
    public List<ulong> AssisterSteamIds { get; set; } = new();
}
