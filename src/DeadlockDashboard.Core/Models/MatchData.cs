namespace DeadlockDashboard.Core.Models;

public class MatchData
{
    public string MatchId { get; set; } = string.Empty;
    public string SourceFilename { get; set; } = string.Empty;
    public DateTime ParsedAt { get; set; }
    public TimeSpan MatchDuration { get; set; }
    public string WinningTeam { get; set; } = string.Empty;
    public int AmberTeamKills { get; set; }
    public int SapphireTeamKills { get; set; }
    public List<PlayerData> Players { get; set; } = new();
    public List<DeathEvent> Deaths { get; set; } = new();
    public TimelineData Timeline { get; set; } = new();
}

public class PlayerData
{
    public ulong SteamId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int NetWorth { get; set; }
    public int HeroDamage { get; set; }
    public int ObjectiveDamage { get; set; }
    public int HeroHealing { get; set; }
    public int SelfHealing { get; set; }
    public int LastHits { get; set; }
    public int Denies { get; set; }
    public int Level { get; set; }
}

public class DeathEvent
{
    public double GameTimeSeconds { get; set; }
    public ulong VictimSteamId { get; set; }
    public string VictimName { get; set; } = string.Empty;
    public ulong KillerSteamId { get; set; }
    public string KillerName { get; set; } = string.Empty;
    public string Weapon { get; set; } = string.Empty;
    public bool Headshot { get; set; }
    public List<ulong> AssisterSteamIds { get; set; } = new();
}

public class TimelineData
{
    public List<TimelineSnapshot> Snapshots { get; set; } = new();
}

public class TimelineSnapshot
{
    public double GameTimeSeconds { get; set; }
    public Dictionary<ulong, int> PlayerNetWorth { get; set; } = new();
    public Dictionary<ulong, int> PlayerKills { get; set; } = new();
    public Dictionary<ulong, int> PlayerDeaths { get; set; } = new();
    public int AmberTeamNetWorth { get; set; }
    public int SapphireTeamNetWorth { get; set; }
    public int DeathsInInterval { get; set; }
}
