namespace CleanGeek.Core.Services;

/// <summary>A scheduled scan, turned into something schtasks.exe understands.</summary>
public sealed record ScanPlan(bool NeedsScheduledTask, string Describe, string Frequency, string StartTime, string Day)
{
    public static ScanPlan Manual() => new(false, "Only when you press Scan", "", "", "");
}

/// <summary>
/// The same shape as AppGeek's and DriverGeek's schedulers, with two deliberate differences.
///
/// First, the task runs "--scan" and there is no command line that cleans. A scheduled run
/// measures and writes a line to the log; DeleteGate refuses everything when the run is
/// unattended, so even a hand-edited task cannot turn a schedule into a deletion.
///
/// Second, no /RL HIGHEST. DriverGeek asks for it because its manifest requires administrator;
/// CleanGeek's does not, a scan of the places that matter needs no elevation, and a task that
/// asks for rights it does not need is a task that fails on a standard account for no reason.
///
/// The rest is the same, for the same reasons: no /RU or /RP (registers for the current account,
/// runs only when logged on, never prompts for a password) and a flat task name with no folder
/// (schtasks will not create one, and a path into a folder that does not exist registers nothing
/// at all).
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
    /// The schtasks command line. Returns empty when the choice needs no task, which is also the
    /// signal to REMOVE any task already registered - switching to "Manually only" must not leave
    /// an orphan behind.
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
