using System.Net.Http.Json;
using DeadlockDashboard.Shared.Dtos;

namespace DeadlockDashboard.Web.Services;

public class DashboardApiClient
{
    private readonly HttpClient _http;

    public DashboardApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<DemoFileDto>> GetDemosAsync()
    {
        return await _http.GetFromJsonAsync<List<DemoFileDto>>("api/v1/demos") ?? new();
    }

    public async Task<ParseJobCreatedDto> ParseDemoAsync(string filename)
    {
        var response = await _http.PostAsync($"api/v1/demos/{Uri.EscapeDataString(filename)}/parse", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ParseJobCreatedDto>())!;
    }

    public async Task<ParseJobDto> GetJobStatusAsync(string jobId)
    {
        return (await _http.GetFromJsonAsync<ParseJobDto>($"api/v1/jobs/{jobId}"))!;
    }

    public async Task<List<MatchSummaryDto>> GetMatchesAsync()
    {
        return await _http.GetFromJsonAsync<List<MatchSummaryDto>>("api/v1/matches") ?? new();
    }

    public async Task<MatchDetailDto> GetMatchAsync(string matchId)
    {
        return (await _http.GetFromJsonAsync<MatchDetailDto>($"api/v1/matches/{matchId}"))!;
    }

    public async Task<List<PlayerDto>> GetMatchPlayersAsync(string matchId)
    {
        return await _http.GetFromJsonAsync<List<PlayerDto>>($"api/v1/matches/{matchId}/players") ?? new();
    }

    public async Task<PlayerDto> GetPlayerAsync(string matchId, ulong steamId)
    {
        return (await _http.GetFromJsonAsync<PlayerDto>($"api/v1/matches/{matchId}/players/{steamId}"))!;
    }

    public async Task<TimelineDto> GetMatchTimelineAsync(string matchId)
    {
        return (await _http.GetFromJsonAsync<TimelineDto>($"api/v1/matches/{matchId}/timeline"))!;
    }

    public async Task<List<DeathEventDto>> GetMatchDeathsAsync(string matchId)
    {
        return await _http.GetFromJsonAsync<List<DeathEventDto>>($"api/v1/matches/{matchId}/deaths") ?? new();
    }

    public async Task DeleteMatchAsync(string matchId)
    {
        var response = await _http.DeleteAsync($"api/v1/matches/{matchId}");
        response.EnsureSuccessStatusCode();
    }
}
