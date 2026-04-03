namespace DeadlockDashboard.Models;

/// <summary>
/// Top-level container for everything we extracted from a parsed .dem file.
/// All references to live DemoFile entities are flattened into POCOs so this
/// can be safely cached, serialized, and bound to Blazor components without
/// holding the parser or its entities alive.
/// </summary>
public sealed class MatchData
{
    public required string SourceFileName { get; init; }
    public required MatchInfo Info { get; init; }
    public required IReadOnlyList<PlayerSummary> Players { get; init; }
    public required IReadOnlyList<DeathEvent> Deaths { get; init; }
    public required IReadOnlyList<NetWorthSnapshot> NetWorthTimeline { get; init; }
}

public sealed class MatchInfo
{
    public required uint MatchId { get; init; }
    public required float DurationSeconds { get; init; }
    public required TeamSide? Winner { get; init; }
    public required int AmberScore { get; init; }
    public required int SapphireScore { get; init; }
}

public enum TeamSide
{
    Unknown = 0,
    Amber = 2,
    Sapphire = 3,
}

public sealed class PlayerSummary
{
    public required ulong SteamId { get; init; }
    public required string Name { get; init; }
    public required TeamSide Team { get; init; }
    public required int HeroId { get; init; }
    public required int Kills { get; init; }
    public required int Deaths { get; init; }
    public required int Assists { get; init; }
    public required int LastHits { get; init; }
    public required int Denies { get; init; }
    public required int NetWorth { get; init; }
    public required int HeroDamage { get; init; }
    public required int ObjectiveDamage { get; init; }
    public required int HeroHealing { get; init; }
    public required int Level { get; init; }
    public required IReadOnlyList<string> Items { get; init; }

    public string DisplayLabel => string.IsNullOrWhiteSpace(Name) ? $"Player {SteamId}" : Name;
}

public sealed class DeathEvent
{
    public required float GameTimeSeconds { get; init; }
    public required ulong VictimSteamId { get; init; }
    public required string VictimName { get; init; }
    public required TeamSide VictimTeam { get; init; }
    public required ulong AttackerSteamId { get; init; }
    public required string AttackerName { get; init; }
    public required TeamSide AttackerTeam { get; init; }
    /// <summary>Readable name of the ability/weapon/entity that did the killing blow, or empty if unknown.</summary>
    public required string KillSource { get; init; }
}

/// <summary>
/// One sample of per-player net worth at a given game time.
/// </summary>
public sealed class NetWorthSnapshot
{
    public required float GameTimeSeconds { get; init; }
    public required IReadOnlyDictionary<ulong, int> NetWorthBySteamId { get; init; }
    public required int AmberTotal { get; init; }
    public required int SapphireTotal { get; init; }

    public int Advantage => AmberTotal - SapphireTotal;
}
