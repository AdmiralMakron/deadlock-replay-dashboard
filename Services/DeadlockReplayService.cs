using System.Collections.Concurrent;
using DeadlockDashboard.Models;
using DemoFile;
using DemoFile.Game.Deadlock;
using DemoFile.Sdk;

namespace DeadlockDashboard.Services;

/// <summary>
/// Parses Deadlock .dem files using DeadlockDemoParser and exposes the
/// extracted data through flattened MatchData snapshots. Holds every parsed
/// match in memory keyed by source file name so the dashboard can switch
/// between them.
/// </summary>
public sealed class DeadlockReplayService
{
    /// <summary>How often (in game seconds) to sample player net worth during parse.</summary>
    private const float SnapshotIntervalSeconds = 30f;

    private readonly string[] _demoDirectories;
    private readonly ILogger<DeadlockReplayService> _logger;
    private readonly SemaphoreSlim _parseLock = new(1, 1);
    private readonly ConcurrentDictionary<string, MatchData> _loadedMatches =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Fired whenever the set of loaded matches changes.</summary>
    public event Action? MatchesChanged;

    public DeadlockReplayService(IConfiguration config, ILogger<DeadlockReplayService> logger)
    {
        _logger = logger;
        // DemoDirectories: comma-separated list (env or appsettings). Earlier entries win on
        // duplicate file names — so user-supplied /app/demos shadows /app/bundled-demos.
        var raw = config["DemoDirectories"] ?? "/app/demos,/app/bundled-demos";
        _demoDirectories = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    /// <summary>All matches that have been parsed in this session.</summary>
    public IReadOnlyCollection<MatchData> LoadedMatches =>
        _loadedMatches.Values.OrderBy(m => m.SourceFileName, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Look up a parsed match by file name. Returns null if not loaded.</summary>
    public MatchData? GetMatch(string fileName) =>
        fileName is null ? null :
        _loadedMatches.TryGetValue(fileName, out var m) ? m : null;

    /// <summary>True if a parse is currently in progress.</summary>
    public bool IsParsing { get; private set; }

    /// <summary>List .dem files across all configured demo directories (deduped by file name).</summary>
    public IReadOnlyList<DemoFileInfo> ListDemos()
    {
        var seen = new Dictionary<string, DemoFileInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in _demoDirectories)
        {
            if (!Directory.Exists(dir))
            {
                _logger.LogDebug("Demo directory does not exist: {Dir}", dir);
                continue;
            }
            foreach (var path in Directory.EnumerateFiles(dir, "*.dem", SearchOption.TopDirectoryOnly))
            {
                var fi = new FileInfo(path);
                seen.TryAdd(fi.Name, new DemoFileInfo(fi.Name, fi.Length));
            }
        }
        return seen.Values.OrderBy(d => d.FileName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Resolve a bare file name to a full path by searching the configured directories in order.</summary>
    private string? ResolveDemoPath(string fileName)
    {
        foreach (var dir in _demoDirectories)
        {
            var candidate = Path.Combine(dir, fileName);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>
    /// Parse the named .dem file and store the result, keyed by file name.
    /// </summary>
    public async Task<MatchData> ParseAsync(string fileName, Action<float>? onProgress = null, CancellationToken ct = default)
    {
        // Defend against path traversal — only allow plain file names from the demo dir.
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Contains("..") ||
            fileName.Contains('/') ||
            fileName.Contains('\\'))
        {
            throw new ArgumentException("Invalid demo file name.", nameof(fileName));
        }

        // Already loaded? Return cached result instead of re-parsing.
        if (_loadedMatches.TryGetValue(fileName, out var cached))
        {
            onProgress?.Invoke(1f);
            return cached;
        }

        var fullPath = ResolveDemoPath(fileName)
            ?? throw new FileNotFoundException(
                $"Demo file '{fileName}' not found in any of: {string.Join(", ", _demoDirectories)}");

        await _parseLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            IsParsing = true;
            _logger.LogInformation("Parsing demo {File}", fullPath);
            var match = await Task.Run(() => ParseInternal(fileName, fullPath, onProgress, ct), ct)
                .ConfigureAwait(false);
            _loadedMatches[fileName] = match;
            _logger.LogInformation(
                "Parsed {File}: {Players} players, {Deaths} death events, {Snaps} timeline snapshots",
                fileName, match.Players.Count, match.Deaths.Count, match.NetWorthTimeline.Count);
            MatchesChanged?.Invoke();
            return match;
        }
        finally
        {
            IsParsing = false;
            _parseLock.Release();
        }
    }

    private MatchData ParseInternal(string fileName, string fullPath, Action<float>? onProgress, CancellationToken ct)
    {
        var demo = new DeadlockDemoParser();
        var deathBuilders = new List<DeathBuilder>();
        var snapshots = new List<NetWorthSnapshot>();

        // Map of (tick, victim pawn entindex) -> index into deathBuilders for the in-progress death.
        // Used so the HeroKilled user message can attach the inflictor's class name onto the death
        // record built by the corresponding Source1 player_death event.
        var deathIndexByVictim = new Dictionary<(int tick, uint victimEntindex), int>();

        // PlayerDeath gives us the rich player metadata (name, steam id, attacker name).
        demo.Source1GameEvents.PlayerDeath += e =>
        {
            var t = demo.CurrentGameTime.Value;
            if (float.IsNaN(t) || t < 0f) return;

            var attacker = e.Attacker;
            var victim = e.Player;
            var builder = new DeathBuilder
            {
                GameTimeSeconds = t,
                VictimSteamId = SafeSteamId(victim),
                VictimName = victim?.PlayerName ?? e.Victimname ?? "?",
                VictimTeam = ToTeamSide(victim),
                AttackerSteamId = SafeSteamId(attacker),
                AttackerName = attacker?.PlayerName ?? e.Attackername ?? "?",
                AttackerTeam = ToTeamSide(attacker),
                KillSource = "", // populated by HeroKilled below if it fires
            };

            // Capture the victim pawn entindex so HeroKilled can correlate.
            uint victimPawnIdx = e.PlayerPawnHandle.Index.Value;
            int idx = deathBuilders.Count;
            deathBuilders.Add(builder);
            deathIndexByVictim[(demo.CurrentDemoTick.Value, victimPawnIdx)] = idx;
        };

        // HeroKilled user message contains entindex_inflictor — the entity that did the damage
        // (an ability, item, weapon, projectile…). Resolve to its server class name and prettify.
        demo.UserMessageEvents.HeroKilled += e =>
        {
            // Try to find the death record for this victim at this tick.
            var key = (demo.CurrentDemoTick.Value, (uint)e.EntindexVictim);
            if (!deathIndexByVictim.TryGetValue(key, out var idx)) return;
            if (e.EntindexInflictor < 0) return;

            var inflictorEntity = demo.GetEntityByIndex<CEntityInstance<DeadlockDemoParser>>(
                new CEntityIndex((uint)e.EntindexInflictor));
            if (inflictorEntity is null) return;

            var pretty = PrettifyClassName(inflictorEntity.ServerClass.Name);
            // Mutate the in-progress death record (struct held in list — replace by index).
            var b = deathBuilders[idx];
            b.KillSource = pretty;
            deathBuilders[idx] = b;
        };

        // Sample net worth periodically. Hook DemoPacket and throttle by game time.
        // DemoPacket subscribers are invoked AFTER the parser's internal handler updates entity
        // state, so the snapshot reads the post-update values for this packet.
        float lastSampleTime = float.NegativeInfinity;
        demo.DemoEvents.DemoPacket += _ =>
        {
            var t = demo.CurrentGameTime.Value;
            if (float.IsNaN(t) || t < 0f) return;
            if (t - lastSampleTime < SnapshotIntervalSeconds) return;
            lastSampleTime = t;

            var byPlayer = new Dictionary<ulong, int>();
            int amberTotal = 0;
            int sapphireTotal = 0;
            foreach (var p in demo.PlayersIncludingDisconnected)
            {
                var nw = p.PlayerDataGlobal.GoldNetWorth + p.PlayerDataGlobal.APNetWorth;
                var sid = p.SteamID;
                if (sid == 0) continue;
                byPlayer[sid] = nw;
                var side = ToTeamSide(p);
                if (side == TeamSide.Amber) amberTotal += nw;
                else if (side == TeamSide.Sapphire) sapphireTotal += nw;
            }

            snapshots.Add(new NetWorthSnapshot
            {
                GameTimeSeconds = t,
                NetWorthBySteamId = byPlayer,
                AmberTotal = amberTotal,
                SapphireTotal = sapphireTotal,
            });
        };

        if (onProgress is not null)
        {
            demo.DemoEvents.DemoFileHeader += _ => onProgress(0f);
        }

        using var stream = File.OpenRead(fullPath);
        var reader = DemoFileReader.Create(demo, stream);

        if (onProgress is not null)
        {
            reader.OnProgress = e => onProgress(e.ProgressRatio);
        }

        // ReadAllAsync returns a Task — block here because we're already on a Task.Run thread.
        reader.ReadAllAsync(ct).GetAwaiter().GetResult();

        // Materialise the death events from builders.
        var deaths = deathBuilders
            .Select(b => new DeathEvent
            {
                GameTimeSeconds = b.GameTimeSeconds,
                VictimSteamId = b.VictimSteamId,
                VictimName = b.VictimName,
                VictimTeam = b.VictimTeam,
                AttackerSteamId = b.AttackerSteamId,
                AttackerName = b.AttackerName,
                AttackerTeam = b.AttackerTeam,
                KillSource = b.KillSource,
            })
            .ToList();

        // Pull final per-player snapshot from the end-state of the demo.
        var players = new List<PlayerSummary>();
        foreach (var p in demo.PlayersIncludingDisconnected)
        {
            if (p.SteamID == 0) continue;
            var d = p.PlayerDataGlobal;
            // Item upgrades are stored as CUtlStringToken (uint hash) without a reverse-lookup
            // table — we keep the hashes as raw strings for now and the UI suppresses the section.
            var items = new List<string>();
            for (int i = 0; i < d.Upgrades.Count; i++)
                items.Add(d.Upgrades[i].Value.ToString());

            players.Add(new PlayerSummary
            {
                SteamId = p.SteamID,
                Name = p.PlayerName ?? $"Player {p.SteamID}",
                Team = ToTeamSide(p),
                HeroId = d.HeroID.Value,
                Kills = d.PlayerKills,
                Deaths = d.Deaths,
                Assists = d.PlayerAssists,
                LastHits = d.LastHits,
                Denies = d.Denies,
                NetWorth = d.GoldNetWorth + d.APNetWorth,
                HeroDamage = d.HeroDamage,
                ObjectiveDamage = d.ObjectiveDamage,
                HeroHealing = d.HeroHealing,
                Level = d.Level,
                Items = items,
            });
        }

        // Match-level info pulled from GameRules at end of demo.
        uint matchId = 0;
        int amberScore = 0;
        int sapphireScore = 0;
        TeamSide? winner = null;
        try
        {
            var gr = demo.GameRules;
            matchId = (uint)gr.MatchID.Value;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read GameRules.MatchID");
        }
        try
        {
            amberScore = demo.TeamAmber.Score;
            sapphireScore = demo.TeamSapphire.Score;
            if (amberScore != sapphireScore)
                winner = amberScore > sapphireScore ? TeamSide.Amber : TeamSide.Sapphire;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read team scores");
        }

        var info = new MatchInfo
        {
            MatchId = matchId,
            DurationSeconds = demo.CurrentGameTime.Value,
            Winner = winner,
            AmberScore = amberScore,
            SapphireScore = sapphireScore,
        };

        return new MatchData
        {
            SourceFileName = fileName,
            Info = info,
            Players = players.OrderBy(p => p.Team).ThenByDescending(p => p.Kills).ToList(),
            Deaths = deaths,
            NetWorthTimeline = snapshots,
        };
    }

    /// <summary>
    /// Convert an internal Source 2 server-class name to something readable.
    /// e.g. "CCitadel_Ability_LashUlt" -> "Lash Ult", "CCitadel_BulletDamage" -> "Bullet Damage".
    /// </summary>
    private static string PrettifyClassName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var s = raw;
        // Strip common Citadel-entity prefixes.
        string[] prefixes =
        {
            "CCitadel_Ability_", "CCitadel_Item_", "CCitadel_Cast_", "CCitadel_",
            "CCitadel", "Ability_", "Item_", "C_",
        };
        foreach (var p in prefixes)
        {
            if (s.StartsWith(p, StringComparison.Ordinal))
            {
                s = s[p.Length..];
                break;
            }
        }
        // _ → space, then split camelCase.
        s = s.Replace('_', ' ');
        var sb = new System.Text.StringBuilder(s.Length + 8);
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (i > 0 && char.IsUpper(c) && char.IsLower(s[i - 1]))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    private static ulong SafeSteamId(CCitadelPlayerController? p) => p?.SteamID ?? 0UL;

    private static TeamSide ToTeamSide(CCitadelPlayerController? p) =>
        p is null ? TeamSide.Unknown : (TeamSide)p.TeamNum;

    /// <summary>Mutable accumulator used while parsing — promoted to immutable DeathEvent at end.</summary>
    private struct DeathBuilder
    {
        public float GameTimeSeconds;
        public ulong VictimSteamId;
        public string VictimName;
        public TeamSide VictimTeam;
        public ulong AttackerSteamId;
        public string AttackerName;
        public TeamSide AttackerTeam;
        public string KillSource;
    }
}

public sealed record DemoFileInfo(string FileName, long SizeBytes)
{
    public string DisplaySize => SizeBytes switch
    {
        >= 1L << 30 => $"{SizeBytes / (double)(1L << 30):0.##} GB",
        >= 1L << 20 => $"{SizeBytes / (double)(1L << 20):0.##} MB",
        >= 1L << 10 => $"{SizeBytes / (double)(1L << 10):0.##} KB",
        _ => $"{SizeBytes} B",
    };
}
