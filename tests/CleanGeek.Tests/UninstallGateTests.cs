using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Tests;

public static class UninstallGateTests
{
    private static readonly InstalledApp Normal =
        new("Notepad++", "Don Ho", "8.6.9", @"C:\Program Files\Notepad++\uninstall.exe", AppSource.Installed);

    private static UninstallContext Ok => new(
        Chosen: true, Elevated: true, Unattended: false,
        OthersAlsoChosen: false, PackagedAppsEnabled: true);

    public static void Run()
    {
        Check.Section("UninstallGate");

        Check.That("uninstalls a normal application", UninstallGate.CanUninstall(Normal, Ok));
        Check.That("says nothing when it allows", UninstallGate.Refuse(Normal, Ok) is null);

        Check.That("refuses a scheduled uninstall",
            !UninstallGate.CanUninstall(Normal, Ok with { Unattended = true }));
        Check.That("refuses one that was not chosen",
            !UninstallGate.CanUninstall(Normal, Ok with { Chosen = false }));
        Check.That("refuses more than one at a time",
            !UninstallGate.CanUninstall(Normal, Ok with { OthersAlsoChosen = true }));
        Check.That("explains why one at a time",
            UninstallGate.Refuse(Normal, Ok with { OthersAlsoChosen = true })!
                .Contains("one application at a time", StringComparison.Ordinal));

        var system = Normal with { Name = "Microsoft Visual C++ Runtime", IsSystemComponent = true };
        Check.That("refuses a system component", !UninstallGate.CanUninstall(system, Ok));
        Check.That("names it when it refuses",
            UninstallGate.Refuse(system, Ok)!.Contains("Microsoft Visual C++ Runtime", StringComparison.Ordinal));

        var noUninstaller = Normal with { UninstallCommand = "   " };
        Check.That("refuses one with no uninstaller", !UninstallGate.CanUninstall(noUninstaller, Ok));
        Check.That("points somewhere useful instead",
            UninstallGate.Refuse(noUninstaller, Ok)!.Contains("Settings", StringComparison.Ordinal));

        var packaged = Normal with { Source = AppSource.Packaged };
        Check.That("uninstalls a packaged application when they are enabled",
            UninstallGate.CanUninstall(packaged, Ok));
        Check.That("refuses a packaged application when they are switched off",
            !UninstallGate.CanUninstall(packaged, Ok with { PackagedAppsEnabled = false }));

        Check.Section("InstalledApp itself");

        Check.That("knows it can be uninstalled", Normal.CanUninstall);
        Check.That("knows it cannot when it is a system component", !system.CanUninstall);
        Check.That("knows it cannot without an uninstall command", !noUninstaller.CanUninstall);
        Check.That("treats a missing size as unknown, not as zero",
            (Normal with { EstimatedBytes = 0 }).SizeUnknown);
        Check.That("knows a real size is not unknown",
            !(Normal with { EstimatedBytes = 1024 }).SizeUnknown);
    }
}
