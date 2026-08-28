namespace CleanGeek.Core.Services;

/// <summary>A scheduled scan expressed in schtasks.exe terms.</summary>
public sealed record ScanPlan(bool NeedsScheduledTask, string Describe, string Frequency, string StartTime, string Day)
{
    public static ScanPlan Manual() => new(false, "Only when you press Scan", "", "", "");
}

/// <summary>
/// Builds the schtasks command lines for the scheduled scan. The task runs "--scan"; there is no
/// command line that cleans.
///
/// No /RL HIGHEST: a scan needs no elevation, and asking for rights it does not need makes the
/// task fail on a standard account. No /RU or /RP, so it registers for the current account and
/// never prompts for a password. The task name is flat, because schtasks will not create a
/// folder and a path into one that does not exist registers nothing.
/// </summary>
public static class CleanSchedule
{
    public const string TaskName = "CleanGeek Scheduled Scan";

    public static ScanPlan Parse(string? choice)
    {
        var text = (choice ?? "").Trim();

        return text switch
        {
            "Daily at 03:00" => new ScanPlan(true, "Every day at 03:00", "DAILY", "03:00", ""),
            "Daily at 12:00" => new ScanPlan(true, "Every day at 12:00", "DAILY", "12:00", ""),
            "Weekly on Sunday" => new ScanPlan(true, "Every Sunday at 03:00", "WEEKLY", "03:00", "SUN"),
            "Every time CleanGeek starts" => new ScanPlan(false, "Every time you open CleanGeek", "", "", ""),
            "Manually only" => ScanPlan.Manual(),
            _ => ScanPlan.Manual()
        };
    }

    /// <summary>
    /// The schtasks create command line. Empty when no task is needed, which also signals that any
    /// already-registered task should be removed.
    /// </summary>
    public static string CreateCommand(ScanPlan plan, string exePath)
    {
        if (!plan.NeedsScheduledTask) return "";

        var day = plan.Frequency == "WEEKLY" ? $" /d {plan.Day}" : "";
        return $"/create /f /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --scan\" " +
               $"/sc {plan.Frequency}{day} /st {plan.StartTime}";
    }

    public static string DeleteCommand() => $"/delete /f /tn \"{TaskName}\"";

    public static IReadOnlyList<string> Options =>
    [
        "Daily at 03:00",
        "Daily at 12:00",
        "Weekly on Sunday",
        "Every time CleanGeek starts",
        "Manually only"
    ];
}
