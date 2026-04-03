using DemoFile;
using DemoFile.Game.Deadlock;
using DeadlockDashboard.Models;

namespace DeadlockDashboard.Services;

public class DemoParserService
{
    private readonly ILogger<DemoParserService> _logger;
    private readonly string _demosPath;

    public DemoParserService(ILogger<DemoParserService> logger, IConfiguration config)
    {
        _logger = logger;
        _demosPath = config.GetValue<string>("DemosPath") ?? Path.Combine(Directory.GetCurrentDirectory(), "demos");
    }

    public string[] GetAvailableDemos()
    {
        if (!Directory.Exists(_demosPath))
            return [];
        return Directory.GetFiles(_demosPath, "*.dem")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .OrderBy(f => f)
            .ToArray();
    }

    public async Task<MatchData> ParseDemoAsync(string fileName, Action<int>? onProgress = null, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_demosPath, fileName);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Demo file not found: {fileName}");

        var matchData = new MatchData();
        var killEvents = new List<KillEvent>();
        var snapshots = new List<TimelineSnapshot>();
        float lastSnapshotTime = -30f;

        var demo = new DeadlockDemoParser();

        // Collect kill events
        demo.Source1GameEvents.PlayerDeath += e =>
        {
            var gameTime = demo.CurrentGameTime.Value;
            if (gameTime < 0) return;

            var kill = new KillEvent
            {
                GameTimeSeconds = Math.Round(gameTime, 1),
                AttackerName = e.Attacker?.PlayerName ?? e.Attackername ?? "Unknown",
                VictimName = e.Player?.PlayerName ?? e.Victimname ?? "Unknown",
                Weapon = e.Weapon ?? "",
                IsHeadshot = e.Headshot,
                AssisterNames = GetAssisters(e)
            };
            killEvents.Add(kill);
        };

        // Periodic snapshots of player souls
        demo.OnCommandFinishPersistent = () =>
        {
            var gameTime = demo.CurrentGameTime.Value;
            if (gameTime < 0) return;
            if (gameTime - lastSnapshotTime < 30f) return;

            lastSnapshotTime = gameTime;

            var snapshot = new TimelineSnapshot
            {
                GameTimeSeconds = Math.Round(gameTime, 1),
                PlayerSouls = new Dictionary<string, int>()
            };

            foreach (var player in demo.PlayersIncludingDisconnected)
            {
                var name = player.PlayerName;
                if (string.IsNullOrEmpty(name) || name == "SourceTV") continue;
                snapshot.PlayerSouls[name] = player.PlayerDataGlobal.GoldNetWorth;
            }

            if (snapshot.PlayerSouls.Count > 0)
                snapshots.Add(snapshot);

            // Report progress
            if (onProgress != null && demo.TickCount.Value > 0)
            {
                var progress = (int)(Math.Max(0, demo.CurrentDemoTick.Value) * 100.0 / demo.TickCount.Value);
                onProgress(Math.Clamp(progress, 0, 100));
            }
        };

        await using var fileStream = File.OpenRead(filePath);
        var reader = DemoFileReader.Create(demo, fileStream);

        _logger.LogInformation("Starting parse of {FileName} ({Size:F0} MB)", fileName, fileStream.Length / 1048576.0);

        await reader.ReadAllAsync(ct);

        _logger.LogInformation("Parse complete. Collecting final player stats.");

        // Collect final player stats
        foreach (var player in demo.PlayersIncludingDisconnected)
        {
            var name = player.PlayerName;
            if (string.IsNullOrEmpty(name) || name == "SourceTV") continue;

            var data = player.PlayerDataGlobal;
            var teamNum = player.CitadelTeamNum;

            matchData.Players.Add(new PlayerStats
            {
                Name = name,
                HeroId = data.HeroID.Value,
                Team = teamNum == TeamNumber.Amber ? "Amber" : teamNum == TeamNumber.Sapphire ? "Sapphire" : "Unknown",
                Kills = data.PlayerKills,
                Deaths = data.Deaths,
                Assists = data.PlayerAssists,
                HeroDamage = data.HeroDamage,
                ObjectiveDamage = data.ObjectiveDamage,
                Healing = data.HeroHealing + data.SelfHealing,
                GoldNetWorth = data.GoldNetWorth,
                LastHits = data.LastHits,
                Denies = data.Denies,
                Level = data.Level
            });
        }

        matchData.KillEvents = killEvents;
        matchData.Timeline = snapshots;
        matchData.MatchDurationSeconds = Math.Round(demo.CurrentGameTime.Value, 1);

        _logger.LogInformation("Parsed {PlayerCount} players, {KillCount} kills, {SnapshotCount} snapshots over {Duration:F0}s",
            matchData.Players.Count, matchData.KillEvents.Count, matchData.Timeline.Count, matchData.MatchDurationSeconds);

        return matchData;
    }

    private static List<string> GetAssisters(Source1PlayerDeathEvent e)
    {
        var assisters = new List<string>();
        if (e.Assister1controller is { } a1) assisters.Add(a1.PlayerName);
        if (e.Assister2controller is { } a2) assisters.Add(a2.PlayerName);
        if (e.Assister3controller is { } a3) assisters.Add(a3.PlayerName);
        if (e.Assister4controller is { } a4) assisters.Add(a4.PlayerName);
        if (e.Assister5controller is { } a5) assisters.Add(a5.PlayerName);
        return assisters;
    }
}
