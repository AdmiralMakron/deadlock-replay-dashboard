namespace DeadlockDashboard.Shared.Dtos;

/// <summary>
/// Time-series match data.
/// </summary>
public class TimelineDto
{
    /// <summary>Periodic snapshots of match state.</summary>
    public List<TimelineSnapshotDto> Snapshots { get; set; } = new();
}

/// <summary>
/// A single point-in-time snapshot of match state.
/// </summary>
public class TimelineSnapshotDto
{
    /// <summary>Game time in seconds.</summary>
    public double GameTimeSeconds { get; set; }

    /// <summary>Net worth for each player keyed by Steam ID.</summary>
    public Dictionary<string, int> PlayerNetWorth { get; set; } = new();

    /// <summary>Kill count for each player keyed by Steam ID.</summary>
    public Dictionary<string, int> PlayerKills { get; set; } = new();

    /// <summary>Death count for each player keyed by Steam ID.</summary>
    public Dictionary<string, int> PlayerDeaths { get; set; } = new();

    /// <summary>Total Amber team net worth.</summary>
    public int AmberTeamNetWorth { get; set; }

    /// <summary>Total Sapphire team net worth.</summary>
    public int SapphireTeamNetWorth { get; set; }

    /// <summary>Number of deaths in this time interval.</summary>
    public int DeathsInInterval { get; set; }
}
