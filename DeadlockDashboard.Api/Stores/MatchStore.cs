using System.Collections.Concurrent;
using DeadlockDashboard.Core.Models;

namespace DeadlockDashboard.Api.Stores;

/// <summary>
/// Thread-safe in-memory store of parsed matches. Registered as a singleton.
/// </summary>
public sealed class MatchStore
{
    private readonly ConcurrentDictionary<string, ParsedMatch> _matches = new();

    public void Add(ParsedMatch match) => _matches[match.MatchId] = match;

    public bool TryGet(string matchId, out ParsedMatch? match)
    {
        var ok = _matches.TryGetValue(matchId, out var m);
        match = m;
        return ok;
    }

    public bool Remove(string matchId) => _matches.TryRemove(matchId, out _);

    public IReadOnlyCollection<ParsedMatch> All() => _matches.Values.ToArray();
}
