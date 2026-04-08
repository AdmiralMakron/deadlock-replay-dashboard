using DeadlockDashboard.Api.Services;
using DeadlockDashboard.Api.Stores;
using DeadlockDashboard.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeadlockDashboard.Api.Controllers;

/// <summary>Endpoints for inspecting parsed matches held in memory.</summary>
[ApiController]
[Route("api/v1/matches")]
[Produces("application/json")]
public sealed class MatchesController : ControllerBase
{
    private readonly MatchStore _store;

    public MatchesController(MatchStore store)
    {
        _store = store;
    }

    /// <summary>List all parsed matches currently held in memory.</summary>
    [HttpGet(Name = "ListMatches")]
    [ProducesResponseType(typeof(IEnumerable<MatchSummaryDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<MatchSummaryDto>> List()
    {
        var items = _store.All()
            .OrderByDescending(m => m.ParsedAt)
            .Select(m => m.ToSummary())
            .ToArray();
        return Ok(items);
    }

    /// <summary>Get the full detail of a single parsed match.</summary>
    /// <param name="matchId">The match id returned by the parse job.</param>
    [HttpGet("{matchId}", Name = "GetMatch")]
    [ProducesResponseType(typeof(MatchDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public ActionResult<MatchDetailDto> Get(string matchId)
    {
        if (!_store.TryGet(matchId, out var m) || m is null)
            return NotFound(new ErrorResponseDto("not_found", $"Match '{matchId}' not found."));
        return Ok(m.ToDetail());
    }

    /// <summary>Get just the player stats for a match.</summary>
    [HttpGet("{matchId}/players", Name = "GetMatchPlayers")]
    [ProducesResponseType(typeof(IEnumerable<PlayerStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<PlayerStatsDto>> Players(string matchId)
    {
        if (!_store.TryGet(matchId, out var m) || m is null)
            return NotFound(new ErrorResponseDto("not_found", $"Match '{matchId}' not found."));
        return Ok(m.Players.Select(p => p.ToDto()).ToArray());
    }

    /// <summary>Get stats for a single player within a match.</summary>
    [HttpGet("{matchId}/players/{steamId}", Name = "GetMatchPlayer")]
    [ProducesResponseType(typeof(PlayerStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public ActionResult<PlayerStatsDto> Player(string matchId, ulong steamId)
    {
        if (!_store.TryGet(matchId, out var m) || m is null)
            return NotFound(new ErrorResponseDto("not_found", $"Match '{matchId}' not found."));
        var p = m.Players.FirstOrDefault(x => x.SteamId == steamId);
        if (p is null)
            return NotFound(new ErrorResponseDto("not_found", $"Player '{steamId}' not found in match."));
        return Ok(p.ToDto());
    }

    /// <summary>Get the per-snapshot timeline for a match.</summary>
    [HttpGet("{matchId}/timeline", Name = "GetMatchTimeline")]
    [ProducesResponseType(typeof(TimelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public ActionResult<TimelineDto> Timeline(string matchId)
    {
        if (!_store.TryGet(matchId, out var m) || m is null)
            return NotFound(new ErrorResponseDto("not_found", $"Match '{matchId}' not found."));
        var dto = new TimelineDto(m.DurationSeconds,
            m.Timeline.Select(s => s.ToDto()).ToList());
        return Ok(dto);
    }

    /// <summary>Get the chronological death event log for a match.</summary>
    [HttpGet("{matchId}/deaths", Name = "GetMatchDeaths")]
    [ProducesResponseType(typeof(IEnumerable<DeathEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<DeathEventDto>> Deaths(string matchId)
    {
        if (!_store.TryGet(matchId, out var m) || m is null)
            return NotFound(new ErrorResponseDto("not_found", $"Match '{matchId}' not found."));
        return Ok(m.Deaths.Select(d => d.ToDto()).ToArray());
    }

    /// <summary>Remove a parsed match from the in-memory store.</summary>
    [HttpDelete("{matchId}", Name = "DeleteMatch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public IActionResult Delete(string matchId)
    {
        if (!_store.Remove(matchId))
            return NotFound(new ErrorResponseDto("not_found", $"Match '{matchId}' not found."));
        return NoContent();
    }
}
