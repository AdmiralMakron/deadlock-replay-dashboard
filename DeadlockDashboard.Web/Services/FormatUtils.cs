namespace DeadlockDashboard.Web.Services;

public static class FormatUtils
{
    public static string HumanBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double val = bytes;
        int u = 0;
        while (val >= 1024 && u < units.Length - 1) { val /= 1024; u++; }
        return $"{val:0.##} {units[u]}";
    }

    public static string FormatDuration(double seconds)
    {
        if (seconds < 0) seconds = 0;
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    public static int GameTimeToMinutes(double seconds) => (int)Math.Floor(seconds / 60.0);

    public static string TeamColor(string team) => team switch
    {
        "Amber" => "#E8943A",
        "Sapphire" => "#3A7BD5",
        _ => "#9A9AAE",
    };
}
