using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>
/// What CleanGeek says about a thing that starts with Windows.
///
/// Note what is missing: a "startup impact" score. Windows measures that itself over several
/// boots and CleanGeek cannot, so inventing one would be a guess dressed as a measurement -
/// which is the habit this range exists to avoid. What CleanGeek can honestly do is tell you
/// what an entry is, where it starts from, and when turning it off would be a bad idea.
/// </summary>
public static class StartupPolicy
{
    /// <summary>
    /// Things that should almost always be left alone. Matched loosely against the entry name,
    /// its publisher and its command, because the same thing appears under different names on
    /// different machines.
    /// </summary>
    private static readonly (string Needle, string Why)[] LeaveOn =
    [
        ("defender",     "Security software. Turning it off at startup leaves the machine unprotected."),
        ("antivirus",    "Security software. Turning it off at startup leaves the machine unprotected."),
        ("security",     "Security software. Turning it off at startup leaves the machine unprotected."),
        ("bitdefender",  "Security software. Turning it off at startup leaves the machine unprotected."),
        ("kaspersky",    "Security software. Turning it off at startup leaves the machine unprotected."),
        ("malwarebytes", "Security software. Turning it off at startup leaves the machine unprotected."),
        ("sophos",       "Security software. Turning it off at startup leaves the machine unprotected."),
        ("eset",         "Security software. Turning it off at startup leaves the machine unprotected."),
        ("realtek",      "Audio. Sound often stops working properly without it."),
        ("waves maxx",   "Audio. Sound often stops working properly without it."),
        ("synaptics",    "Touchpad or fingerprint hardware. Gestures and sign-in can stop working."),
        ("elan",         "Touchpad hardware. Gestures can stop working."),
        ("onedrive",     "File sync. Nothing will sync while it is off, and it will look like your files stopped updating."),
        ("dropbox",      "File sync. Nothing will sync while it is off."),
        ("google drive", "File sync. Nothing will sync while it is off."),
        ("intel(r) graphics", "Display hardware. Screen settings and brightness controls can stop working.")
    ];

    /// <summary>The reason to leave this entry on, or null when there is no particular reason.</summary>
    public static string? ReasonToKeep(StartupEntry entry)
    {
        var haystack = $"{entry.Name} {entry.Publisher} {entry.Command}".ToLowerInvariant();

        foreach (var (needle, why) in LeaveOn)
            if (haystack.Contains(needle, StringComparison.Ordinal))
                return why;

        return null;
    }

    public static bool ShouldWarnBeforeDisabling(StartupEntry entry) => ReasonToKeep(entry) is not null;

    /// <summary>
    /// Whether CleanGeek will disable this entry if asked. It never disables anything on its own,
    /// and a machine-wide entry needs administrator rights, because the alternative is a change
    /// that silently does not happen.
    /// </summary>
    public static string? RefuseDisable(StartupEntry entry, bool elevated, bool unattended)
    {
        if (unattended)
            return "A scheduled run never changes what starts with Windows.";

        if (!entry.Enabled)
            return $"{entry.Name} is already off.";

        if (entry.Scope == StartupScope.AllUsers && !elevated)
            return $"{entry.Name} starts for every user on this PC, which needs administrator rights.";

        if (entry.Location == StartupLocation.ScheduledTask && !elevated)
            return $"{entry.Name} is a scheduled task, and changing it needs administrator rights.";

        return null;
    }

    public static bool CanDisable(StartupEntry entry, bool elevated, bool unattended) =>
        RefuseDisable(entry, elevated, unattended) is null;

    public static string Describe(StartupEntry entry) => entry.Location switch
    {
        StartupLocation.RunKey => entry.Scope == StartupScope.AllUsers
            ? "Registry, all users"
            : "Registry, your account",
        StartupLocation.StartupFolder => entry.Scope == StartupScope.AllUsers
            ? "Startup folder, all users"
            : "Startup folder, your account",
        StartupLocation.ScheduledTask => "Scheduled task, at sign-in",
        _ => "Unknown"
    };
}
