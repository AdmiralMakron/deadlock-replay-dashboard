using System.Text.RegularExpressions;
using DeadlockDashboard.Core.Services;
using DeadlockDashboard.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DeadlockDashboard.Api.Controllers;

/// <summary>
/// Manages available demo files and initiates parsing.
/// </summary>
[ApiController]
[Route("api/v1/demos")]
[Produces("application/json")]
public partial class DemosController : ControllerBase
{
    private readonly JobStore _jobStore;
    private readonly ParseJobQueue _queue;
    private readonly string _demosPath;

    public DemosController(JobStore jobStore, ParseJobQueue queue, IConfiguration config)
    {
        _jobStore = jobStore;
        _queue = queue;
        _demosPath = config.GetValue<string>("DemosPath") ?? "/app/demos";
    }

    /// <summary>
    /// Returns a list of available .dem files in the demos directory.
    /// </summary>
    /// <returns>Array of demo file entries with filename and size.</returns>
    /// <response code="200">List of available demo files.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<DemoFileDto>), 200)]
    public IActionResult ListDemos()
    {
        if (!Directory.Exists(_demosPath))
            return Ok(Array.Empty<DemoFileDto>());

        var files = Directory.GetFiles(_demosPath, "*.dem")
            .Select(f => new FileInfo(f))
            .Select(fi => new DemoFileDto
            {
                Filename = fi.Name,
                FileSizeBytes = fi.Length
            })
            .OrderBy(f => f.Filename)
            .ToList();

        return Ok(files);
    }

    /// <summary>
    /// Initiates parsing of the specified demo file. Returns immediately with a job ID.
    /// </summary>
    /// <param name="filename">The demo filename to parse (e.g., 48525700.dem).</param>
    /// <returns>Job ID and location header pointing to the job status endpoint.</returns>
    /// <response code="202">Parse job created successfully.</response>
    /// <response code="400">Invalid filename format.</response>
    /// <response code="404">Demo file not found.</response>
    [HttpPost("{filename}/parse")]
    [ProducesResponseType(typeof(ParseJobCreatedDto), 202)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> ParseDemo(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename) || !SafeFilenameRegex().IsMatch(filename))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "INVALID_FILENAME",
                Message = "Filename must contain only alphanumeric characters, hyphens, underscores, and periods, and must end with .dem."
            });
        }

        var filePath = Path.Combine(_demosPath, filename);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new ErrorResponse
            {
                Error = "FILE_NOT_FOUND",
                Message = $"Demo file '{filename}' was not found."
            });
        }

        var job = _jobStore.CreateJob(filename);
        await _queue.EnqueueAsync(new ParseJobRequest
        {
            JobId = job.JobId,
            FilePath = filePath
        });

        Response.Headers["Location"] = $"/api/v1/jobs/{job.JobId}";
        return StatusCode(202, new ParseJobCreatedDto { JobId = job.JobId });
    }

    [GeneratedRegex(@"^[\w\-\.]+\.dem$")]
    private static partial Regex SafeFilenameRegex();
}
