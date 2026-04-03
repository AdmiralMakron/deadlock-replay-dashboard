namespace DeadlockDashboard.Models;

public class MatchData
{
    public List<PlayerStats> Players { get; set; } = [];
    public List<KillEvent> KillEvents { get; set; } = [];
    public List<TimelineSnapshot> Timeline { get; set; } = [];
    public double MatchDurationSeconds { get; set; }
}

public class PlayerStats
{
    public string Name { get; set; } = "";
    public int HeroId { get; set; }
    public string Team { get; set; } = ""; // "Amber" or "Sapphire"
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int HeroDamage { get; set; }
    public int ObjectiveDamage { get; set; }
    public int Healing { get; set; }
    public int GoldNetWorth { get; set; }
    public int LastHits { get; set; }
    public int Denies { get; set; }
    public int Level { get; set; }
}

public class KillEvent
{
    public double GameTimeSeconds { get; set; }
    public string AttackerName { get; set; } = "";
    public string VictimName { get; set; } = "";
    public string Weapon { get; set; } = "";
    public bool IsHeadshot { get; set; }
    public List<string> AssisterNames { get; set; } = [];
}

public class TimelineSnapshot
{
    public double GameTimeSeconds { get; set; }
    public Dictionary<string, int> PlayerSouls { get; set; } = new();
}
