using System.Threading.Channels;
using DeadlockDashboard.Api.Stores;
using DeadlockDashboard.Core.Services;
using DeadlockDashboard.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeadlockDashboard.Api.Services;

/// <summary>
/// Queues parse work and exposes the enqueue entry point to controllers.
/// </summary>
public interface IParseJobQueue
{
    void Enqueue(ParseJob job, string filePath);
}

public sealed class ParseJobQueue : IParseJobQueue
{
    private readonly Channel<(ParseJob Job, string FilePath)> _channel =
        Channel.CreateUnbounded<(ParseJob, string)>();

    public ChannelReader<(ParseJob Job, string FilePath)> Reader => _channel.Reader;

    public void Enqueue(ParseJob job, string filePath) =>
        _channel.Writer.TryWrite((job, filePath));
}

/// <summary>
/// Hosted background service that drains the parse queue and runs the parser.
/// </summary>
public sealed class ParseJobWorker : BackgroundService
{
    private readonly ParseJobQueue _queue;
    private readonly IDemoParserService _parser;
    private readonly MatchStore _matchStore;
    private readonly ILogger<ParseJobWorker> _logger;

    public ParseJobWorker(
        IParseJobQueue queue,
        IDemoParserService parser,
        MatchStore matchStore,
        ILogger<ParseJobWorker> logger)
    {
        _queue = (ParseJobQueue)queue;
        _parser = parser;
        _matchStore = matchStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var work in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            var (job, filePath) = work;
            try
            {
                job.Status = JobStatus.Running;
                job.Progress = 0;
                _logger.LogInformation("Parse job {Id} starting for {File}", job.Id, job.Filename);

                var match = await _parser.ParseAsync(
                    filePath,
                    job.Filename,
                    pct => job.Progress = pct,
                    stoppingToken);

                _matchStore.Add(match);
                job.MatchId = match.MatchId;
                job.Progress = 100;
                job.Status = JobStatus.Completed;
                _logger.LogInformation("Parse job {Id} completed -> match {MatchId}", job.Id, match.MatchId);
            }
            catch (OperationCanceledException)
            {
                job.Status = JobStatus.Failed;
                job.Error = "Parsing was cancelled";
            }
            catch (Exception ex)
            {
                job.Status = JobStatus.Failed;
                job.Error = ex.Message;
                _logger.LogError(ex, "Parse job {Id} failed", job.Id);
            }
        }
    }
}
