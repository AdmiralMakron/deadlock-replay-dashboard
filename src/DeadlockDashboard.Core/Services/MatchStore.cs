using System.Collections.Concurrent;
using DeadlockDashboard.Core.Models;

namespace DeadlockDashboard.Core.Services;

public class MatchStore
{
    private readonly ConcurrentDictionary<string, MatchData> _matches = new();

    public void AddMatch(MatchData match)
    {
        _matches[match.MatchId] = match;
    }

    public MatchData? GetMatch(string matchId)
    {
        _matches.TryGetValue(matchId, out var match);
        return match;
    }

    public List<MatchData> GetAllMatches()
    {
        return _matches.Values.ToList();
    }

    public bool RemoveMatch(string matchId)
    {
        return _matches.TryRemove(matchId, out _);
    }
}
