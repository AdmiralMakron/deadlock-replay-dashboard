namespace DeadlockDashboard.Shared.Dtos;

/// <summary>
/// Summary of a parsed match.
/// </summary>
public class MatchSummaryDto
{
    /// <summary>Unique match identifier.</summary>
    public string MatchId { get; set; } = string.Empty;

    /// <summary>Source demo filename.</summary>
    public string SourceFilename { get; set; } = string.Empty;

    /// <summary>When the match was parsed.</summary>
    public DateTime ParsedAt { get; set; }

    /// <summary>Match duration in seconds.</summary>
    public double MatchDurationSeconds { get; set; }

    /// <summary>The winning team name.</summary>
    public string WinningTeam { get; set; } = string.Empty;
}

/// <summary>
/// Full match data including players, deaths, and timeline.
/// </summary>
public class MatchDetailDto
{
    /// <summary>Unique match identifier.</summary>
    public string MatchId { get; set; } = string.Empty;

    /// <summary>Source demo filename.</summary>
    public string SourceFilename { get; set; } = string.Empty;

    /// <summary>When the match was parsed.</summary>
    public DateTime ParsedAt { get; set; }

    /// <summary>Match duration in seconds.</summary>
    public double MatchDurationSeconds { get; set; }

    /// <summary>The winning team name.</summary>
    public string WinningTeam { get; set; } = string.Empty;

    /// <summary>Total Amber team kills.</summary>
    public int AmberTeamKills { get; set; }

    /// <summary>Total Sapphire team kills.</summary>
    public int SapphireTeamKills { get; set; }

    /// <summary>All players in the match.</summary>
    public List<PlayerDto> Players { get; set; } = new();

    /// <summary>All death events in chronological order.</summary>
    public List<DeathEventDto> Deaths { get; set; } = new();

    /// <summary>Timeline data with periodic snapshots.</summary>
    public TimelineDto Timeline { get; set; } = new();
}
