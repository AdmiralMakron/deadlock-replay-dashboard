using DeadlockDashboard.Core.Models;
using DemoFile;
using DemoFile.Game.Deadlock;
using Microsoft.Extensions.Logging;

namespace DeadlockDashboard.Core.Services;

/// <summary>
/// Parses Deadlock .dem replay files into <see cref="ParsedMatch"/> domain models.
/// This service has no knowledge of HTTP or Blazor and can be consumed from any layer.
/// </summary>
public interface IDemoParserService
{
    /// <summary>
    /// Parse the given demo file, invoking <paramref name="progress"/> (0-100)
    /// periodically as parsing progresses.
    /// </summary>
    Task<ParsedMatch> ParseAsync(
        string filePath,
        string sourceFilename,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class DemoParserService : IDemoParserService
{
    private readonly ILogger<DemoParserService> _logger;
    private const double SnapshotIntervalSeconds = 30.0;

    public DemoParserService(ILogger<DemoParserService> logger)
    {
        _logger = logger;
    }

    public async Task<ParsedMatch> ParseAsync(
        string filePath,
        string sourceFilename,
        Action<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Demo file not found", filePath);

        var match = new ParsedMatch
        {
            MatchId = Guid.NewGuid().ToString("N")[..12],
            Filename = sourceFilename,
            ParsedAt = DateTime.UtcNow,
        };

        var demo = new DeadlockDemoParser();

        // Track game start so we can compute elapsed time cleanly.
        double? gameStart = null;
        double lastGameTime = 0;
        double lastSnapshot = double.NegativeInfinity;

        void TakeSnapshot(double gameTime)
        {
            var snap = new ParsedSnapshot { GameTimeSeconds = Math.Max(0, gameTime) };
            int amberTotal = 0;
            int sapphireTotal = 0;
            foreach (var p in demo.Players)
            {
                var pdg = p.PlayerDataGlobal;
                var steam = p.SteamID;
                if (steam == 0) continue;
                var team = (TeamNumber)p.TeamNum;
                var nw = pdg.GoldNetWorth;
                snap.Players.Add(new ParsedPlayerSnapshot
                {
                    SteamId = steam,
                    NetWorth = nw,
                    Kills = pdg.PlayerKills,
                    Deaths = pdg.Deaths,
                    Level = pdg.Level,
                    LastHits = pdg.LastHits,
                });
                if (team == TeamNumber.Amber) amberTotal += nw;
                else if (team == TeamNumber.Sapphire) sapphireTotal += nw;
            }
            snap.AmberNetWorthTotal = amberTotal;
            snap.SapphireNetWorthTotal = sapphireTotal;
            match.Timeline.Add(snap);
        }

        demo.Source1GameEvents.PlayerDeath += e =>
        {
            double gameTime = demo.CurrentGameTime.Value;
            if (gameStart.HasValue) gameTime -= gameStart.Value;
            if (gameTime < 0) gameTime = 0;

            var victim = e.Player;
            var attacker = e.Attacker;
            if (victim == null) return;

            var victimTeam = ((TeamNumber)victim.TeamNum).ToString();
            string? attackerTeam = attacker != null ? ((TeamNumber)attacker.TeamNum).ToString() : null;

            match.Deaths.Add(new ParsedDeath
            {
                GameTimeSeconds = gameTime,
                VictimSteamId = victim.SteamID,
                VictimName = string.IsNullOrEmpty(victim.PlayerName) ? e.Victimname : victim.PlayerName,
                VictimTeam = victimTeam,
                KillerSteamId = attacker?.SteamID,
                KillerName = attacker != null
                    ? (string.IsNullOrEmpty(attacker.PlayerName) ? e.Attackername : attacker.PlayerName)
                    : (string.IsNullOrEmpty(e.Attackername) ? null : e.Attackername),
                KillerTeam = attackerTeam,
            });
        };

        var reader = DemoFileReader.Create(demo, File.OpenRead(filePath));
        reader.OnProgress = ev =>
        {
            var pct = (int)Math.Clamp(ev.ProgressRatio * 100f, 0, 99);
            progress?.Invoke(pct);

            // Anchor game start once we're actually into the game.
            var now = demo.CurrentGameTime.Value;
            if (now > 0 && !gameStart.HasValue && demo.Players.Any())
            {
                gameStart = now;
            }
            lastGameTime = now;

            // Periodic snapshot based on (elapsed) game time.
            var elapsed = gameStart.HasValue ? now - gameStart.Value : now;
            if (elapsed >= lastSnapshot + SnapshotIntervalSeconds && demo.Players.Any())
            {
                TakeSnapshot(elapsed);
                lastSnapshot = elapsed;
            }
        };

        try
        {
            await reader.ReadAllAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing demo file {File}", filePath);
            throw;
        }

        // Final snapshot at match end
        var finalElapsed = gameStart.HasValue ? lastGameTime - gameStart.Value : lastGameTime;
        if (finalElapsed < 0) finalElapsed = 0;
        if (demo.Players.Any())
        {
            TakeSnapshot(finalElapsed);
        }

        match.DurationSeconds = finalElapsed;

        // Collect final player stats
        foreach (var p in demo.PlayersIncludingDisconnected)
        {
            if (p.SteamID == 0) continue;
            var pdg = p.PlayerDataGlobal;
            var teamNum = (TeamNumber)p.TeamNum;
            if (teamNum != TeamNumber.Amber && teamNum != TeamNumber.Sapphire) continue;

            var heroId = (int)pdg.HeroID.Value;
            match.Players.Add(new ParsedPlayer
            {
                SteamId = p.SteamID,
                PlayerName = p.PlayerName,
                Team = teamNum.ToString(),
                HeroId = heroId,
                HeroName = HeroNameLookup.Get(heroId),
                Level = pdg.Level,
                Kills = pdg.PlayerKills,
                Deaths = pdg.Deaths,
                NetWorth = pdg.GoldNetWorth,
                HeroDamage = pdg.HeroDamage,
                ObjectiveDamage = pdg.ObjectiveDamage,
                Healing = pdg.HeroHealing,
                LastHits = pdg.LastHits,
                Denies = pdg.Denies,
            });
        }

        // Compute assists from death events (each killer's assisters get +1).
        var assistsBySteam = new Dictionary<ulong, int>();
        foreach (var death in match.Deaths)
        {
            // We need the original death event assist list; we only stored victim/killer.
            // Fall back: we didn't capture assisters above, so assist count will come from
            // the per-death assister scan below using demo events. Leave zeros here.
        }
        // (Assists: the PlayerDataGlobal.PlayerAssists field would be ideal — use that.)
        var assistMap = new Dictionary<ulong, int>();
        foreach (var p in demo.PlayersIncludingDisconnected)
        {
            if (p.SteamID == 0) continue;
            assistMap[p.SteamID] = p.PlayerDataGlobal.PlayerAssists;
        }
        foreach (var player in match.Players)
        {
            if (assistMap.TryGetValue(player.SteamId, out var a)) player.Assists = a;
        }

        // Scores/winner
        try
        {
            match.AmberScore = demo.TeamAmber.Score;
            match.SapphireScore = demo.TeamSapphire.Score;
        }
        catch
        {
            // Team entities may be gone at end of demo; derive from player kills as a fallback.
            match.AmberScore = match.Players.Where(p => p.Team == "Amber").Sum(p => p.Kills);
            match.SapphireScore = match.Players.Where(p => p.Team == "Sapphire").Sum(p => p.Kills);
        }
        match.Winner = match.AmberScore == match.SapphireScore
            ? null
            : (match.AmberScore > match.SapphireScore ? "Amber" : "Sapphire");

        progress?.Invoke(100);
        return match;
    }
}
