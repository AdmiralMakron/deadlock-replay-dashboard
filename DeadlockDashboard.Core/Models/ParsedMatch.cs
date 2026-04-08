namespace DeadlockDashboard.Core.Models;

public sealed class ParsedMatch
{
    public required string MatchId { get; init; }
    public required string Filename { get; init; }
    public required DateTime ParsedAt { get; init; }
    public double DurationSeconds { get; set; }
    public string? Winner { get; set; }
    public int AmberScore { get; set; }
    public int SapphireScore { get; set; }
    public List<ParsedPlayer> Players { get; init; } = new();
    public List<ParsedDeath> Deaths { get; init; } = new();
    public List<ParsedSnapshot> Timeline { get; init; } = new();
}

public sealed class ParsedPlayer
{
    public ulong SteamId { get; set; }
    public string PlayerName { get; set; } = "";
    public string Team { get; set; } = "Unassigned";
    public string? HeroName { get; set; }
    public int HeroId { get; set; }
    public int Level { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int NetWorth { get; set; }
    public int HeroDamage { get; set; }
    public int ObjectiveDamage { get; set; }
    public int Healing { get; set; }
    public int LastHits { get; set; }
    public int Denies { get; set; }
}

public sealed class ParsedDeath
{
    public double GameTimeSeconds { get; set; }
    public ulong VictimSteamId { get; set; }
    public string VictimName { get; set; } = "";
    public string VictimTeam { get; set; } = "";
    public ulong? KillerSteamId { get; set; }
    public string? KillerName { get; set; }
    public string? KillerTeam { get; set; }
}

public sealed class ParsedSnapshot
{
    public double GameTimeSeconds { get; set; }
    public int AmberNetWorthTotal { get; set; }
    public int SapphireNetWorthTotal { get; set; }
    public List<ParsedPlayerSnapshot> Players { get; init; } = new();
}

public sealed class ParsedPlayerSnapshot
{
    public ulong SteamId { get; set; }
    public int NetWorth { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Level { get; set; }
    public int LastHits { get; set; }
}
