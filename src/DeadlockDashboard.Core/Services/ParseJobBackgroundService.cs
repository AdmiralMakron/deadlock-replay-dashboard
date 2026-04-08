using System.Threading.Channels;
using DeadlockDashboard.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeadlockDashboard.Core.Services;

public class ParseJobRequest
{
    public string JobId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public class ParseJobQueue
{
    private readonly Channel<ParseJobRequest> _channel = Channel.CreateUnbounded<ParseJobRequest>();

    public async ValueTask EnqueueAsync(ParseJobRequest request)
    {
        await _channel.Writer.WriteAsync(request);
    }

    public IAsyncEnumerable<ParseJobRequest> DequeueAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}

public class ParseJobBackgroundService : BackgroundService
{
    private readonly ParseJobQueue _queue;
    private readonly JobStore _jobStore;
    private readonly MatchStore _matchStore;
    private readonly DemoParserService _parser;
    private readonly ILogger<ParseJobBackgroundService> _logger;

    public ParseJobBackgroundService(
        ParseJobQueue queue,
        JobStore jobStore,
        MatchStore matchStore,
        DemoParserService parser,
        ILogger<ParseJobBackgroundService> logger)
    {
        _queue = queue;
        _jobStore = jobStore;
        _matchStore = matchStore;
        _parser = parser;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                _jobStore.UpdateJob(request.JobId, j =>
                {
                    j.Status = ParseJobStatus.Running;
                    j.Progress = 0;
                });

                var match = await _parser.ParseDemoAsync(
                    request.FilePath,
                    progress => _jobStore.UpdateJob(request.JobId, j => j.Progress = progress),
                    stoppingToken);

                _matchStore.AddMatch(match);

                _jobStore.UpdateJob(request.JobId, j =>
                {
                    j.Status = ParseJobStatus.Completed;
                    j.Progress = 100;
                    j.MatchId = match.MatchId;
                });

                _logger.LogInformation("Job {JobId} completed with match {MatchId}", request.JobId, match.MatchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobId} failed", request.JobId);
                _jobStore.UpdateJob(request.JobId, j =>
                {
                    j.Status = ParseJobStatus.Failed;
                    j.ErrorMessage = ex.Message;
                });
            }
        }
    }
}
