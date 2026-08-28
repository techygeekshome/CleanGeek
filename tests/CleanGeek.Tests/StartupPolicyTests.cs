using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Tests;

public static class StartupPolicyTests
{
    private static StartupEntry Entry(string name, string publisher = "", string command = "",
        StartupScope scope = StartupScope.ThisUser,
        StartupLocation location = StartupLocation.RunKey,
        bool enabled = true) =>
        new(name, command, location, scope, publisher, enabled);

    public static void Run()
    {
        Check.Section("StartupPolicy - what to leave on");

        Check.That("warns about security software",
            StartupPolicy.ShouldWarnBeforeDisabling(Entry("Windows Defender notification icon")));
        Check.That("warns about a third-party antivirus",
            StartupPolicy.ShouldWarnBeforeDisabling(Entry("Bitdefender Agent")));
        Check.That("warns about audio",
            StartupPolicy.ShouldWarnBeforeDisabling(Entry("RtkAudUService", publisher: "Realtek")));
        Check.That("warns about the touchpad",
            StartupPolicy.ShouldWarnBeforeDisabling(Entry("SynTPEnh", publisher: "Synaptics")));
        Check.That("warns about file sync",
            StartupPolicy.ShouldWarnBeforeDisabling(Entry("OneDrive")));
        Check.That("matches on the command as well as the name",
            StartupPolicy.ShouldWarnBeforeDisabling(
                Entry("Updater", command: @"C:\Program Files\Dropbox\dropbox.exe")));
        Check.That("ignores case",
            StartupPolicy.ShouldWarnBeforeDisabling(Entry("ONEDRIVE")));

        Check.That("has nothing to say about an ordinary application",
            !StartupPolicy.ShouldWarnBeforeDisabling(Entry("Spotify")));
        Check.That("has nothing to say about a game launcher",
            !StartupPolicy.ShouldWarnBeforeDisabling(Entry("Steam Client Bootstrapper", "Valve")));
        Check.That("the reason is written for a person",
            StartupPolicy.ReasonToKeep(Entry("OneDrive")) is { Length: > 20 });
        Check.That("returns no reason when there is none",
            StartupPolicy.ReasonToKeep(Entry("Spotify")) is null);

        Check.Section("StartupPolicy - disabling");

        Check.That("disables an ordinary entry for this user",
            StartupPolicy.CanDisable(Entry("Spotify"), elevated: false, unattended: false));
        Check.That("still allows a warned entry to be turned off if the person insists",
            StartupPolicy.CanDisable(Entry("OneDrive"), elevated: false, unattended: false));

        Check.That("never changes anything on a scheduled run",
            !StartupPolicy.CanDisable(Entry("Spotify"), elevated: true, unattended: true));
        Check.That("says why on a scheduled run",
            StartupPolicy.RefuseDisable(Entry("Spotify"), true, true)!
                .Contains("never changes", StringComparison.Ordinal));

        Check.That("refuses a machine-wide entry without administrator rights",
            !StartupPolicy.CanDisable(Entry("Agent", scope: StartupScope.AllUsers), false, false));
        Check.That("allows a machine-wide entry with them",
            StartupPolicy.CanDisable(Entry("Agent", scope: StartupScope.AllUsers), true, false));
        Check.That("refuses a scheduled task without administrator rights",
            !StartupPolicy.CanDisable(Entry("Agent", location: StartupLocation.ScheduledTask), false, false));
        Check.That("does nothing to an entry that is already off",
            !StartupPolicy.CanDisable(Entry("Spotify", enabled: false), true, false));

        Check.Section("StartupPolicy - describing where it starts from");

        Check.Equal("registry, this user", "Registry, your account",
            StartupPolicy.Describe(Entry("x")));
        Check.Equal("registry, all users", "Registry, all users",
            StartupPolicy.Describe(Entry("x", scope: StartupScope.AllUsers)));
        Check.Equal("startup folder", "Startup folder, your account",
            StartupPolicy.Describe(Entry("x", location: StartupLocation.StartupFolder)));
        Check.Equal("scheduled task", "Scheduled task, at sign-in",
            StartupPolicy.Describe(Entry("x", location: StartupLocation.ScheduledTask)));
    }
}
