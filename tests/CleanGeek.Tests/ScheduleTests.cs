using CleanGeek.Core.Services;

namespace CleanGeek.Tests;

public static class ScheduleTests
{
    private const string Exe = @"C:\Tools\CleanGeek\CleanGeek.exe";

    public static void Run()
    {
        Check.Section("CleanSchedule - reading the dropdown");

        Check.That("daily needs a task", CleanSchedule.Parse("Daily at 03:00").NeedsScheduledTask);
        Check.Equal("daily at three", "03:00", CleanSchedule.Parse("Daily at 03:00").StartTime);
        Check.Equal("daily at noon", "12:00", CleanSchedule.Parse("Daily at 12:00").StartTime);
        Check.Equal("weekly is weekly", "WEEKLY", CleanSchedule.Parse("Weekly on Sunday").Frequency);
        Check.Equal("weekly on Sunday", "SUN", CleanSchedule.Parse("Weekly on Sunday").Day);
        Check.That("on startup needs no task",
            !CleanSchedule.Parse("Every time CleanGeek starts").NeedsScheduledTask);
        Check.That("manual needs no task", !CleanSchedule.Parse("Manually only").NeedsScheduledTask);
        Check.That("an unknown choice falls back to manual", !CleanSchedule.Parse("whenever").NeedsScheduledTask);
        Check.That("null falls back to manual", !CleanSchedule.Parse(null).NeedsScheduledTask);
        Check.That("surrounding spaces do not change the answer",
            CleanSchedule.Parse("  Daily at 03:00  ").NeedsScheduledTask);
        Check.That("every option parses to something",
            CleanSchedule.Options.All(o => CleanSchedule.Parse(o).Describe.Length > 0));

        Check.Section("CleanSchedule - the command line");

        var daily = CleanSchedule.CreateCommand(CleanSchedule.Parse("Daily at 03:00"), Exe);
        Check.That("creates a task", daily.Contains("/create", StringComparison.Ordinal));
        Check.That("names it", daily.Contains(CleanSchedule.TaskName, StringComparison.Ordinal));
        Check.That("quotes the executable path", daily.Contains("\\\"" + Exe + "\\\"", StringComparison.Ordinal));
        Check.That("runs a scan", daily.Contains("--scan", StringComparison.Ordinal));
        Check.That("has no clean switch at all", !daily.Contains("--clean", StringComparison.Ordinal));
        Check.That("overwrites an existing task", daily.Contains("/f", StringComparison.Ordinal));

        // Deliberately different from DriverGeek: a scan needs no elevation, and a task that asks
        // for rights it does not need is a task that fails on a standard account for no reason.
        Check.That("does not ask for the highest run level",
            !daily.Contains("/rl", StringComparison.OrdinalIgnoreCase));
        Check.That("never supplies a password", !daily.Contains("/rp", StringComparison.OrdinalIgnoreCase));
        Check.That("never registers as another user", !daily.Contains("/ru", StringComparison.OrdinalIgnoreCase));
        Check.That("uses a flat task name with no folder",
            !CleanSchedule.TaskName.Contains('\\', StringComparison.Ordinal));

        var weekly = CleanSchedule.CreateCommand(CleanSchedule.Parse("Weekly on Sunday"), Exe);
        Check.That("passes the day for a weekly task", weekly.Contains("/d SUN", StringComparison.Ordinal));
        Check.That("a daily task has no day", !daily.Contains("/d ", StringComparison.Ordinal));

        Check.Equal("manual produces no command", "",
            CleanSchedule.CreateCommand(CleanSchedule.Parse("Manually only"), Exe));
        Check.Equal("on-startup produces no command", "",
            CleanSchedule.CreateCommand(CleanSchedule.Parse("Every time CleanGeek starts"), Exe));

        Check.That("the delete command names the same task",
            CleanSchedule.DeleteCommand().Contains(CleanSchedule.TaskName, StringComparison.Ordinal));
        Check.That("the delete command is forced",
            CleanSchedule.DeleteCommand().Contains("/f", StringComparison.Ordinal));
    }
}
