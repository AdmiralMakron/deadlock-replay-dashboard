using DeadlockDashboard.Core.Models;
using DeadlockDashboard.Shared.Dtos;

namespace DeadlockDashboard.Api;

internal static class DtoMapper
{
    public static MatchSummaryDto ToSummaryDto(MatchData m) => new()
    {
        MatchId = m.MatchId,
        SourceFilename = m.SourceFilename,
        ParsedAt = m.ParsedAt,
        MatchDurationSeconds = m.MatchDuration.TotalSeconds,
        WinningTeam = m.WinningTeam
    };

    public static MatchDetailDto ToDetailDto(MatchData m) => new()
    {
        MatchId = m.MatchId,
        SourceFilename = m.SourceFilename,
        ParsedAt = m.ParsedAt,
        MatchDurationSeconds = m.MatchDuration.TotalSeconds,
        WinningTeam = m.WinningTeam,
        AmberTeamKills = m.AmberTeamKills,
        SapphireTeamKills = m.SapphireTeamKills,
        Players = m.Players.Select(ToPlayerDto).ToList(),
        Deaths = m.Deaths.Select(ToDeathEventDto).ToList(),
        Timeline = ToTimelineDto(m.Timeline)
    };

    public static PlayerDto ToPlayerDto(PlayerData p) => new()
    {
        SteamId = p.SteamId,
        PlayerName = p.PlayerName,
        Team = p.Team,
        Kills = p.Kills,
        Deaths = p.Deaths,
        Assists = p.Assists,
        NetWorth = p.NetWorth,
        HeroDamage = p.HeroDamage,
        ObjectiveDamage = p.ObjectiveDamage,
        HeroHealing = p.HeroHealing,
        SelfHealing = p.SelfHealing,
        LastHits = p.LastHits,
        Denies = p.Denies,
        Level = p.Level
    };

    public static DeathEventDto ToDeathEventDto(DeathEvent d) => new()
    {
        GameTimeSeconds = d.GameTimeSeconds,
        VictimSteamId = d.VictimSteamId,
        VictimName = d.VictimName,
        KillerSteamId = d.KillerSteamId,
        KillerName = d.KillerName,
        Weapon = d.Weapon,
        Headshot = d.Headshot,
        AssisterSteamIds = d.AssisterSteamIds
    };

    public static TimelineDto ToTimelineDto(TimelineData t) => new()
    {
        Snapshots = t.Snapshots.Select(s => new TimelineSnapshotDto
        {
            GameTimeSeconds = s.GameTimeSeconds,
            PlayerNetWorth = s.PlayerNetWorth.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            PlayerKills = s.PlayerKills.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            PlayerDeaths = s.PlayerDeaths.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            AmberTeamNetWorth = s.AmberTeamNetWorth,
            SapphireTeamNetWorth = s.SapphireTeamNetWorth,
            DeathsInInterval = s.DeathsInInterval
        }).ToList()
    };
}
