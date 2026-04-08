using System.Net.Http.Json;
using DeadlockDashboard.Shared;

namespace DeadlockDashboard.Web.Services;

/// <summary>
/// Typed HTTP wrapper around the REST API. Components must never talk to the
/// parser or stores directly — always go through this client.
/// </summary>
public sealed class DashboardApiClient
{
    private readonly HttpClient _http;

    public DashboardApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<DemoFileDto[]?> GetDemosAsync(CancellationToken ct = default) =>
        _http.GetFromJsonAsync<DemoFileDto[]>("api/v1/demos", ct);

    public async Task<ParseJobAcceptedDto?> ParseDemoAsync(string filename, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/demos/{Uri.EscapeDataString(filename)}/parse", content: null, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ParseJobAcceptedDto>(cancellationToken: ct);
    }

    public Task<ParseJobDto?> GetJobStatusAsync(Guid jobId, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<ParseJobDto>($"api/v1/jobs/{jobId}", ct);

    public Task<MatchSummaryDto[]?> GetMatchesAsync(CancellationToken ct = default) =>
        _http.GetFromJsonAsync<MatchSummaryDto[]>("api/v1/matches", ct);

    public Task<MatchDetailDto?> GetMatchAsync(string matchId, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<MatchDetailDto>($"api/v1/matches/{matchId}", ct);

    public Task<PlayerStatsDto[]?> GetMatchPlayersAsync(string matchId, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<PlayerStatsDto[]>($"api/v1/matches/{matchId}/players", ct);

    public Task<TimelineDto?> GetMatchTimelineAsync(string matchId, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<TimelineDto>($"api/v1/matches/{matchId}/timeline", ct);

    public Task<DeathEventDto[]?> GetMatchDeathsAsync(string matchId, CancellationToken ct = default) =>
        _http.GetFromJsonAsync<DeathEventDto[]>($"api/v1/matches/{matchId}/deaths", ct);

    public async Task<bool> DeleteMatchAsync(string matchId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/matches/{matchId}", ct);
        return resp.IsSuccessStatusCode;
    }
}
