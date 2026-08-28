using CleanGeek.Core.Models;
using Microsoft.Win32;

namespace CleanGeek.Services;

/// <summary>
/// Reads installed applications from the Uninstall registry keys. Read-only. Packaged (Store)
/// applications are not listed; that needs the packaging APIs and a Windows-specific TFM.
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

                    // The same application can appear in more than one hive. Keep the first.
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

        // Patches, runtimes and child entries; hidden by default, as in Settings > Apps.
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
            // EstimatedSize is in kilobytes and is often absent.
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

    /// <summary>InstallDate is yyyyMMdd when present, and is often malformed.</summary>
    private static DateTime? ParseInstallDate(string? raw) =>
        DateTime.TryParseExact(raw, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                               System.Globalization.DateTimeStyles.None, out var date)
            ? date
            : null;
}
