using CleanGeek.Core.Models;
using Microsoft.Win32;

namespace CleanGeek.Services;

/// <summary>
/// Reads startup entries from the Run keys and the Startup folders. Read-only; nothing is
/// disabled here. Logon-triggered scheduled tasks are not read, as that needs the Task
/// Scheduler API.
/// </summary>
public sealed class StartupService
{
    private const string Run = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string Approved = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public IReadOnlyList<StartupEntry> Read()
    {
        var entries = new List<StartupEntry>();

        entries.AddRange(FromRunKey(Registry.CurrentUser, StartupScope.ThisUser));
        entries.AddRange(FromRunKey(Registry.LocalMachine, StartupScope.AllUsers));

        entries.AddRange(FromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                                    StartupScope.ThisUser));
        entries.AddRange(FromFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                                    StartupScope.AllUsers));

        Log.Write($"Startup: {entries.Count} entries, {entries.Count(e => e.Enabled)} of them on.");

        return entries
            .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static IEnumerable<StartupEntry> FromRunKey(RegistryKey hive, StartupScope scope)
    {
        var results = new List<StartupEntry>();

        try
        {
            using var key = hive.OpenSubKey(Run);
            if (key is null) return results;

            using var approved = hive.OpenSubKey(Approved);

            foreach (var name in key.GetValueNames())
            {
                var command = key.GetValue(name) as string ?? "";
                results.Add(new StartupEntry(
                    Name: name,
                    Command: command,
                    Location: StartupLocation.RunKey,
                    Scope: scope,
                    Publisher: "",
                    Enabled: IsApproved(approved, name)));
            }
        }
        catch (System.Security.SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return results;
    }

    /// <summary>
    /// StartupApproved records a disabled entry as a blob whose first byte is odd (2 on, 3 off).
    /// An unreadable value is reported as enabled.
    /// </summary>
    private static bool IsApproved(RegistryKey? approved, string name)
    {
        if (approved?.GetValue(name) is not byte[] { Length: > 0 } blob) return true;
        return (blob[0] & 1) == 0;
    }

    private static IEnumerable<StartupEntry> FromFolder(string folder, StartupScope scope)
    {
        var results = new List<StartupEntry>();
        if (string.IsNullOrWhiteSpace(folder)) return results;

        try
        {
            var dir = new DirectoryInfo(folder);
            if (!dir.Exists) return results;

            foreach (var file in dir.EnumerateFiles())
            {
                if (file.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;

                results.Add(new StartupEntry(
                    Name: Path.GetFileNameWithoutExtension(file.Name),
                    Command: file.FullName,
                    Location: StartupLocation.StartupFolder,
                    Scope: scope));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        return results;
    }
}
