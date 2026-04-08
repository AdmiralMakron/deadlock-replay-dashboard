using DemoFile;
using DemoFile.Game.Deadlock;
using DeadlockDashboard.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeadlockDashboard.Core.Services;

public class DemoParserService
{
    private readonly ILogger<DemoParserService> _logger;

    public DemoParserService(ILogger<DemoParserService> logger)
    {
        _logger = logger;
    }

    public async Task<MatchData> ParseDemoAsync(string filePath, Action<int>? onProgress = null, CancellationToken ct = default)
    {
        var filename = Path.GetFileName(filePath);
        var matchId = Path.GetFileNameWithoutExtension(filePath) + "_" + Guid.NewGuid().ToString("N")[..6];

        _logger.LogInformation("Starting parse of {Filename}", filename);

        var deaths = new List<DeathEvent>();
        var snapshots = new List<TimelineSnapshot>();
        var playerTeams = new Dictionary<ulong, string>();
        var playerNames = new Dictionary<ulong, string>();
        double lastSnapshotTime = -30;
        int lastDeathCount = 0;

        var demo = new DeadlockDemoParser();

        demo.Source1GameEvents.PlayerDeath += e =>
        {
            if (e.Player == null) return;

            var victimSteamId = e.Player.SteamID;
            var victimName = e.Player.PlayerName ?? "Unknown";
            var killerSteamId = e.Attacker?.SteamID ?? 0;
            var killerName = e.Attacker?.PlayerName ?? "World";

            var assisters = new List<ulong>();
            if (e.Assister1controller?.SteamID is > 0) assisters.Add(e.Assister1controller.SteamID);
            if (e.Assister2controller?.SteamID is > 0) assisters.Add(e.Assister2controller.SteamID);
            if (e.Assister3controller?.SteamID is > 0) assisters.Add(e.Assister3controller.SteamID);
            if (e.Assister4controller?.SteamID is > 0) assisters.Add(e.Assister4controller.SteamID);
            if (e.Assister5controller?.SteamID is > 0) assisters.Add(e.Assister5controller.SteamID);

            deaths.Add(new DeathEvent
            {
                GameTimeSeconds = demo.CurrentGameTime.Value,
                VictimSteamId = victimSteamId,
                VictimName = victimName,
                KillerSteamId = killerSteamId,
                KillerName = killerName,
                Weapon = e.Weapon ?? "",
                Headshot = e.Headshot,
                AssisterSteamIds = assisters
            });
        };

        await using var stream = File.OpenRead(filePath);
        var reader = DemoFileReader.Create(demo, stream);

        reader.OnProgress += e =>
        {
            var pct = (int)(e.ProgressRatio * 100);
            onProgress?.Invoke(pct);
        };

        // Collect snapshots during parsing via a timer-like approach
        // We'll use a post-tick approach by hooking into progress events
        var tickInterval = 30.0; // snapshot every 30 seconds

        reader.OnProgress += _ =>
        {
            var currentTime = (double)demo.CurrentGameTime.Value;
            if (currentTime - lastSnapshotTime >= tickInterval && currentTime > 0)
            {
                TakeSnapshot(demo, snapshots, deaths, playerTeams, playerNames, ref lastDeathCount);
                lastSnapshotTime = currentTime;
            }
        };

        await reader.ReadAllAsync(ct);

        // Take final snapshot
        TakeSnapshot(demo, snapshots, deaths, playerTeams, playerNames, ref lastDeathCount);

        // Collect final player stats
        var players = new List<PlayerData>();
        foreach (var p in demo.PlayersIncludingDisconnected)
        {
            if (p.SteamID == 0) continue;
            var team = p.TeamNum == (byte)TeamNumber.Amber ? "Amber" :
                       p.TeamNum == (byte)TeamNumber.Sapphire ? "Sapphire" : "Unknown";
            if (team == "Unknown") continue;

            var stats = p.PlayerDataGlobal;
            players.Add(new PlayerData
            {
                SteamId = p.SteamID,
                PlayerName = p.PlayerName ?? $"Player_{p.SteamID}",
                Team = team,
                Kills = stats.PlayerKills,
                Deaths = stats.Deaths,
                Assists = stats.PlayerAssists,
                NetWorth = stats.GoldNetWorth,
                HeroDamage = stats.HeroDamage,
                ObjectiveDamage = stats.ObjectiveDamage,
                HeroHealing = stats.HeroHealing,
                SelfHealing = stats.SelfHealing,
                LastHits = stats.LastHits,
                Denies = stats.Denies,
                Level = stats.Level
            });
        }

        // Determine winner
        string winningTeam = "Unknown";
        try
        {
            var ggTeam = demo.GameRules.GGTeam;
            if (ggTeam == (int)TeamNumber.Amber)
                winningTeam = "Sapphire"; // The team that calls GG / loses their patron
            else if (ggTeam == (int)TeamNumber.Sapphire)
                winningTeam = "Amber";
        }
        catch { }

        if (winningTeam == "Unknown")
        {
            // Fallback: determine winner by total kills
            var amberKills = players.Where(p => p.Team == "Amber").Sum(p => p.Kills);
            var sapphireKills = players.Where(p => p.Team == "Sapphire").Sum(p => p.Kills);
            winningTeam = amberKills > sapphireKills ? "Amber" : "Sapphire";
        }

        var matchDuration = demo.CurrentGameTime.ToTimeSpan();

        var match = new MatchData
        {
            MatchId = matchId,
            SourceFilename = filename,
            ParsedAt = DateTime.UtcNow,
            MatchDuration = matchDuration,
            WinningTeam = winningTeam,
            AmberTeamKills = players.Where(p => p.Team == "Amber").Sum(p => p.Kills),
            SapphireTeamKills = players.Where(p => p.Team == "Sapphire").Sum(p => p.Kills),
            Players = players,
            Deaths = deaths,
            Timeline = new TimelineData { Snapshots = snapshots }
        };

        _logger.LogInformation("Finished parsing {Filename}: {PlayerCount} players, {DeathCount} deaths, {Duration}",
            filename, players.Count, deaths.Count, matchDuration);

        return match;
    }

    private void TakeSnapshot(DeadlockDemoParser demo, List<TimelineSnapshot> snapshots,
        List<DeathEvent> deaths, Dictionary<ulong, string> playerTeams,
        Dictionary<ulong, string> playerNames, ref int lastDeathCount)
    {
        var snapshot = new TimelineSnapshot
        {
            GameTimeSeconds = demo.CurrentGameTime.Value,
            PlayerNetWorth = new Dictionary<ulong, int>(),
            PlayerKills = new Dictionary<ulong, int>(),
            PlayerDeaths = new Dictionary<ulong, int>()
        };

        int amberNw = 0, sapphireNw = 0;

        foreach (var p in demo.PlayersIncludingDisconnected)
        {
            if (p.SteamID == 0) continue;
            var team = p.TeamNum == (byte)TeamNumber.Amber ? "Amber" :
                       p.TeamNum == (byte)TeamNumber.Sapphire ? "Sapphire" : "Unknown";
            if (team == "Unknown") continue;

            playerTeams[p.SteamID] = team;
            playerNames[p.SteamID] = p.PlayerName ?? $"Player_{p.SteamID}";

            var stats = p.PlayerDataGlobal;
            snapshot.PlayerNetWorth[p.SteamID] = stats.GoldNetWorth;
            snapshot.PlayerKills[p.SteamID] = stats.PlayerKills;
            snapshot.PlayerDeaths[p.SteamID] = stats.Deaths;

            if (team == "Amber") amberNw += stats.GoldNetWorth;
            else sapphireNw += stats.GoldNetWorth;
        }

        snapshot.AmberTeamNetWorth = amberNw;
        snapshot.SapphireTeamNetWorth = sapphireNw;
        snapshot.DeathsInInterval = deaths.Count - lastDeathCount;
        lastDeathCount = deaths.Count;

        snapshots.Add(snapshot);
    }
}
