using DeadlockDashboard.Core.Models;
using DeadlockDashboard.Api.Stores;
using DeadlockDashboard.Shared;

namespace DeadlockDashboard.Api.Services;

public static class Mapping
{
    public static MatchSummaryDto ToSummary(this ParsedMatch m) =>
        new(m.MatchId, m.Filename, m.ParsedAt, m.DurationSeconds, m.Winner);

    public static PlayerStatsDto ToDto(this ParsedPlayer p) =>
        new(p.SteamId, p.PlayerName, p.Team, p.HeroName, p.HeroId, p.Level,
            p.Kills, p.Deaths, p.Assists, p.NetWorth, p.HeroDamage, p.ObjectiveDamage,
            p.Healing, p.LastHits, p.Denies);

    public static DeathEventDto ToDto(this ParsedDeath d) =>
        new(d.GameTimeSeconds, d.VictimSteamId, d.VictimName, d.VictimTeam,
            d.KillerSteamId, d.KillerName, d.KillerTeam);

    public static PlayerSnapshotDto ToDto(this ParsedPlayerSnapshot s) =>
        new(s.SteamId, s.NetWorth, s.Kills, s.Deaths, s.Level, s.LastHits);

    public static TimelineSnapshotDto ToDto(this ParsedSnapshot s) =>
        new(s.GameTimeSeconds, s.AmberNetWorthTotal, s.SapphireNetWorthTotal,
            s.Players.Select(p => p.ToDto()).ToList());

    public static MatchDetailDto ToDetail(this ParsedMatch m) =>
        new(m.ToSummary(), m.AmberScore, m.SapphireScore,
            m.Players.Select(p => p.ToDto()).ToList(),
            m.Deaths.Select(d => d.ToDto()).ToList(),
            m.Timeline.Select(s => s.ToDto()).ToList());

    public static ParseJobDto ToDto(this ParseJob j) =>
        new(j.Id, j.Filename, j.Status, j.Progress, j.MatchId, j.Error, j.CreatedAt);
}
