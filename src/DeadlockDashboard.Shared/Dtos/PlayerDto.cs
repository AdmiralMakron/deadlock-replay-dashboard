namespace DeadlockDashboard.Shared.Dtos;

/// <summary>
/// Player statistics within a match.
/// </summary>
public class PlayerDto
{
    /// <summary>Player's Steam ID.</summary>
    public ulong SteamId { get; set; }

    /// <summary>Player display name.</summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>Team name (Amber or Sapphire).</summary>
    public string Team { get; set; } = string.Empty;

    /// <summary>Total kills.</summary>
    public int Kills { get; set; }

    /// <summary>Total deaths.</summary>
    public int Deaths { get; set; }

    /// <summary>Total assists.</summary>
    public int Assists { get; set; }

    /// <summary>Gold net worth at end of match.</summary>
    public int NetWorth { get; set; }

    /// <summary>Total damage dealt to heroes.</summary>
    public int HeroDamage { get; set; }

    /// <summary>Total damage dealt to objectives.</summary>
    public int ObjectiveDamage { get; set; }

    /// <summary>Total healing done to other heroes.</summary>
    public int HeroHealing { get; set; }

    /// <summary>Total self-healing.</summary>
    public int SelfHealing { get; set; }

    /// <summary>Total last hits on creeps.</summary>
    public int LastHits { get; set; }

    /// <summary>Total denies.</summary>
    public int Denies { get; set; }

    /// <summary>Hero level at end of match.</summary>
    public int Level { get; set; }
}
