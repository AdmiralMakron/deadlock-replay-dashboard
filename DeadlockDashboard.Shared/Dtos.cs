using System.Text.Json.Serialization;

namespace DeadlockDashboard.Shared;

public record DemoFileDto(string Filename, long SizeBytes);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public record ParseJobDto(
    Guid JobId,
    string Filename,
    JobStatus Status,
    int ProgressPercent,
    string? MatchId,
    string? Error,
    DateTime CreatedAt);

public record ParseJobAcceptedDto(Guid JobId, string StatusUrl);

public record MatchSummaryDto(
    string MatchId,
    string Filename,
    DateTime ParsedAt,
    double DurationSeconds,
    string? Winner);

public record MatchDetailDto(
    MatchSummaryDto Info,
    int AmberScore,
    int SapphireScore,
    List<PlayerStatsDto> Players,
    List<DeathEventDto> Deaths,
    List<TimelineSnapshotDto> Timeline);

public record PlayerStatsDto(
    ulong SteamId,
    string PlayerName,
    string Team,
    string? HeroName,
    int HeroId,
    int Level,
    int Kills,
    int Deaths,
    int Assists,
    int NetWorth,
    int HeroDamage,
    int ObjectiveDamage,
    int Healing,
    int LastHits,
    int Denies);

public record DeathEventDto(
    double GameTimeSeconds,
    ulong VictimSteamId,
    string VictimName,
    string VictimTeam,
    ulong? KillerSteamId,
    string? KillerName,
    string? KillerTeam);

public record PlayerSnapshotDto(
    ulong SteamId,
    int NetWorth,
    int Kills,
    int Deaths,
    int Level,
    int LastHits);

public record TimelineSnapshotDto(
    double GameTimeSeconds,
    int AmberNetWorthTotal,
    int SapphireNetWorthTotal,
    List<PlayerSnapshotDto> Players);

public record TimelineDto(
    double DurationSeconds,
    List<TimelineSnapshotDto> Snapshots);

public record ErrorResponseDto(string Code, string Message, string? Details = null);
