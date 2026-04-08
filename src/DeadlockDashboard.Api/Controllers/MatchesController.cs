using DeadlockDashboard.Core.Services;
using DeadlockDashboard.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DeadlockDashboard.Api.Controllers;

/// <summary>
/// Accesses parsed match data including players, timeline, and deaths.
/// </summary>
[ApiController]
[Route("api/v1/matches")]
[Produces("application/json")]
public class MatchesController : ControllerBase
{
    private readonly MatchStore _matchStore;

    public MatchesController(MatchStore matchStore)
    {
        _matchStore = matchStore;
    }

    /// <summary>
    /// Returns all parsed matches currently held in memory.
    /// </summary>
    /// <returns>Array of match summaries.</returns>
    /// <response code="200">List of parsed matches.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<MatchSummaryDto>), 200)]
    public IActionResult ListMatches()
    {
        var matches = _matchStore.GetAllMatches()
            .Select(DtoMapper.ToSummaryDto)
            .ToList();
        return Ok(matches);
    }

    /// <summary>
    /// Returns full parsed match data including players, deaths, and timeline.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <returns>Complete match data.</returns>
    /// <response code="200">Match data returned successfully.</response>
    /// <response code="404">Match not found.</response>
    [HttpGet("{matchId}")]
    [ProducesResponseType(typeof(MatchDetailDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult GetMatch(string matchId)
    {
        var match = _matchStore.GetMatch(matchId);
        if (match == null)
            return NotFound(new ErrorResponse { Error = "MATCH_NOT_FOUND", Message = $"Match '{matchId}' was not found." });

        return Ok(DtoMapper.ToDetailDto(match));
    }

    /// <summary>
    /// Returns player stats for a specific match.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <returns>Array of player stats.</returns>
    /// <response code="200">Player stats returned successfully.</response>
    /// <response code="404">Match not found.</response>
    [HttpGet("{matchId}/players")]
    [ProducesResponseType(typeof(List<PlayerDto>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult GetMatchPlayers(string matchId)
    {
        var match = _matchStore.GetMatch(matchId);
        if (match == null)
            return NotFound(new ErrorResponse { Error = "MATCH_NOT_FOUND", Message = $"Match '{matchId}' was not found." });

        return Ok(match.Players.Select(DtoMapper.ToPlayerDto).ToList());
    }

    /// <summary>
    /// Returns detailed stats for a single player within a match.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="steamId">The player's Steam ID.</param>
    /// <returns>Player stats.</returns>
    /// <response code="200">Player stats returned successfully.</response>
    /// <response code="404">Match or player not found.</response>
    [HttpGet("{matchId}/players/{steamId}")]
    [ProducesResponseType(typeof(PlayerDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult GetPlayer(string matchId, ulong steamId)
    {
        var match = _matchStore.GetMatch(matchId);
        if (match == null)
            return NotFound(new ErrorResponse { Error = "MATCH_NOT_FOUND", Message = $"Match '{matchId}' was not found." });

        var player = match.Players.FirstOrDefault(p => p.SteamId == steamId);
        if (player == null)
            return NotFound(new ErrorResponse { Error = "PLAYER_NOT_FOUND", Message = $"Player with Steam ID '{steamId}' was not found in match '{matchId}'." });

        return Ok(DtoMapper.ToPlayerDto(player));
    }

    /// <summary>
    /// Returns time-series data for a match including net worth snapshots and team totals.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <returns>Timeline data with periodic snapshots.</returns>
    /// <response code="200">Timeline data returned successfully.</response>
    /// <response code="404">Match not found.</response>
    [HttpGet("{matchId}/timeline")]
    [ProducesResponseType(typeof(TimelineDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult GetTimeline(string matchId)
    {
        var match = _matchStore.GetMatch(matchId);
        if (match == null)
            return NotFound(new ErrorResponse { Error = "MATCH_NOT_FOUND", Message = $"Match '{matchId}' was not found." });

        return Ok(DtoMapper.ToTimelineDto(match.Timeline));
    }

    /// <summary>
    /// Returns the chronological death event log for a match.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <returns>Array of death events.</returns>
    /// <response code="200">Death events returned successfully.</response>
    /// <response code="404">Match not found.</response>
    [HttpGet("{matchId}/deaths")]
    [ProducesResponseType(typeof(List<DeathEventDto>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult GetDeaths(string matchId)
    {
        var match = _matchStore.GetMatch(matchId);
        if (match == null)
            return NotFound(new ErrorResponse { Error = "MATCH_NOT_FOUND", Message = $"Match '{matchId}' was not found." });

        return Ok(match.Deaths.Select(DtoMapper.ToDeathEventDto).ToList());
    }

    /// <summary>
    /// Removes a parsed match from the in-memory store.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Match deleted successfully.</response>
    /// <response code="404">Match not found.</response>
    [HttpDelete("{matchId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult DeleteMatch(string matchId)
    {
        if (!_matchStore.RemoveMatch(matchId))
            return NotFound(new ErrorResponse { Error = "MATCH_NOT_FOUND", Message = $"Match '{matchId}' was not found." });

        return NoContent();
    }
}
