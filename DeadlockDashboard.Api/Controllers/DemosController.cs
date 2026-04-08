using DeadlockDashboard.Api.Services;
using DeadlockDashboard.Api.Stores;
using DeadlockDashboard.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeadlockDashboard.Api.Controllers;

/// <summary>
/// Endpoints for discovering and parsing demo files on disk.
/// </summary>
[ApiController]
[Route("api/v1/demos")]
[Produces("application/json")]
public sealed class DemosController : ControllerBase
{
    private readonly DemoDirectory _dir;
    private readonly JobStore _jobs;
    private readonly IParseJobQueue _queue;

    public DemosController(DemoDirectory dir, JobStore jobs, IParseJobQueue queue)
    {
        _dir = dir;
        _jobs = jobs;
        _queue = queue;
    }

    /// <summary>List all .dem files available in the configured demos directory.</summary>
    /// <response code="200">Array of demo files with filename and size.</response>
    [HttpGet(Name = "ListDemos")]
    [ProducesResponseType(typeof(IEnumerable<DemoFileDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<DemoFileDto>> List()
    {
        if (!Directory.Exists(_dir.Path))
            return Ok(Array.Empty<DemoFileDto>());

        var files = Directory.GetFiles(_dir.Path, "*.dem")
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.Name)
            .Select(f => new DemoFileDto(f.Name, f.Length))
            .ToArray();
        return Ok(files);
    }

    /// <summary>Kick off an asynchronous parse job for the given demo filename.</summary>
    /// <param name="filename">Filename of the .dem file (no path).</param>
    /// <response code="202">Parse job accepted; poll /api/v1/jobs/{jobId} for progress.</response>
    /// <response code="400">Filename is invalid or contains path separators.</response>
    /// <response code="404">Filename does not exist on disk.</response>
    [HttpPost("{filename}/parse", Name = "ParseDemo")]
    [ProducesResponseType(typeof(ParseJobAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public ActionResult<ParseJobAcceptedDto> Parse(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename) ||
            filename.Contains('/') || filename.Contains('\\') || filename.Contains("..") ||
            !filename.EndsWith(".dem", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ErrorResponseDto("invalid_filename", "Filename must be a plain .dem filename."));
        }

        var path = Path.Combine(_dir.Path, filename);
        if (!System.IO.File.Exists(path))
            return NotFound(new ErrorResponseDto("not_found", $"Demo file '{filename}' not found."));

        var job = _jobs.Create(filename);
        _queue.Enqueue(job, path);

        var statusUrl = Url.Link("GetJob", new { jobId = job.Id }) ?? $"/api/v1/jobs/{job.Id}";
        Response.Headers.Location = statusUrl;
        return Accepted(statusUrl, new ParseJobAcceptedDto(job.Id, statusUrl));
    }
}
