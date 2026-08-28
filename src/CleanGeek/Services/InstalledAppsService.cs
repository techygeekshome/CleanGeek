using CleanGeek.Core.Models;
using Microsoft.Win32;

namespace CleanGeek.Services;

/// <summary>
/// The Installed screen's inventory. This is what would have been UninstallGeek: CleanGeek
/// absorbed it rather than shipping two applications that both enumerate installed software and
/// both hunt for the same leftovers.
///
/// It reads the same Uninstall keys Windows itself reads, and it never writes to them.
///
/// Packaged (Store) applications are not listed in 1.0. Enumerating them properly needs the
/// packaging APIs and a Windows-version-specific target framework, and guessing at them from
/// half-documented registry keys would be exactly the sort of approximation this range does not
/// ship. The Installed screen says so on screen rather than quietly showing a short list.
/// </summary>
public sealed class InstalledAppsService
{
    private const string Uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string Uninstall32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    public IReadOnlyList<InstalledApp> Read(bool hideSystemComponents)
    {
        var found = new Dictionary<string, InstalledApp>(StringComparer.OrdinalIgnoreCase);

        Harvest(Registry.LocalMachine, Uninstall, found);
        Harvest(Registry.LocalMachine, Uninstall32, found);
        Harvest(Registry.CurrentUser, Uninstall, found);

        var list = found.Values
            .Where(a => !hideSystemComponents || !a.IsSystemComponent)
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Log.Write($"Installed: {list.Count} applications listed.");
        return list;
    }

    private static void Harvest(RegistryKey hive, string path, Dictionary<string, InstalledApp> into)
    {
        try
        {
            using var key = hive.OpenSubKey(path);
            if (key is null) return;

            foreach (var name in key.GetSubKeyNames())
            {
                try
                {
                    using var sub = key.OpenSubKey(name);
                    if (sub is null) continue;

                    var app = ReadOne(sub);
                    if (app is null) continue;

                    // The same application turns up under more than one hive on a machine where it
                    // was installed for everyone and later repaired per user. Keep the first.
                    into.TryAdd(app.Name + " " + app.Version, app);
                }
                catch (System.Security.SecurityException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
        catch (System.Security.SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static InstalledApp? ReadOne(RegistryKey key)
    {
        var name = key.GetValue("DisplayName") as string;
        if (string.IsNullOrWhiteSpace(name)) return null;

        // Windows hides these from Settings, Apps, and so does CleanGeek by default: patches,
        // runtimes, and the child entries of things that are already in the list.
        var systemComponent = Number(key, "SystemComponent") == 1
                              || key.GetValue("ParentKeyName") is string { Length: > 0 }
                              || key.GetValue("ReleaseType") is "Security Update" or "Update Rollup" or "Hotfix";

        var uninstall = key.GetValue("QuietUninstallString") as string
                        ?? key.GetValue("UninstallString") as string
                        ?? "";

        return new InstalledApp(
            Name: name.Trim(),
            Publisher: (key.GetValue("Publisher") as string ?? "").Trim(),
            Version: (key.GetValue("DisplayVersion") as string ?? "").Trim(),
            UninstallCommand: uninstall.Trim(),
            Source: AppSource.Installed,
            InstalledOn: ParseInstallDate(key.GetValue("InstallDate") as string),
            // EstimatedSize is in kilobytes, and is missing more often than it is present.
            EstimatedBytes: Number(key, "EstimatedSize") * 1024L,
            InstallLocation: (key.GetValue("InstallLocation") as string ?? "").Trim(),
            IsSystemComponent: systemComponent);
    }

    private static long Number(RegistryKey key, string value) =>
        key.GetValue(value) switch
        {
            int i when i > 0 => i,
            long l when l > 0 => l,
            _ => 0
        };

    /// <summary>InstallDate is yyyyMMdd when it is there at all, and is frequently malformed.</summary>
    private static DateTime? ParseInstallDate(string? raw) =>
        DateTime.TryParseExact(raw, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                               System.Globalization.DateTimeStyles.None, out var date)
            ? date
            : null;
}
