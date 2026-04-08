namespace DeadlockDashboard.Core.Services;

/// <summary>
/// Maps Deadlock hero IDs to friendly names. Based on the public Deadlock hero list.
/// Unknown IDs return null so the frontend can fall back to the numeric ID.
/// </summary>
public static class HeroNameLookup
{
    private static readonly Dictionary<int, string> _names = new()
    {
        [1] = "Infernus",
        [2] = "Seven",
        [3] = "Vindicta",
        [4] = "Lady Geist",
        [6] = "Abrams",
        [7] = "Wraith",
        [8] = "McGinnis",
        [10] = "Paradox",
        [11] = "Dynamo",
        [12] = "Kelvin",
        [13] = "Haze",
        [14] = "Holliday",
        [15] = "Bebop",
        [16] = "Calico",
        [17] = "Grey Talon",
        [18] = "Mo & Krill",
        [19] = "Shiv",
        [20] = "Ivy",
        [25] = "Warden",
        [27] = "Yamato",
        [31] = "Lash",
        [35] = "Viscous",
        [38] = "Sinclair",
        [39] = "Pocket",
        [47] = "Wrecker",
        [48] = "Mirage",
        [49] = "Fathom",
        [50] = "Trapper",
        [51] = "Rutger",
        [52] = "Viper",
        [53] = "Magician",
        [54] = "The Doorman",
        [55] = "Raven",
        [58] = "Victor",
        [59] = "Drifter",
        [60] = "Bookworm",
        [61] = "Vyper",
        [62] = "Paige",
    };

    public static string? Get(int heroId) =>
        _names.TryGetValue(heroId, out var name) ? name : null;
}
