using CleanGeek.Core.Models;
using Microsoft.Win32;

namespace CleanGeek.Services;

/// <summary>
/// What starts when Windows starts, read from the same places Task Manager reads.
///
/// CleanGeek 1.0 reports. It does not switch anything off. StartupPolicy already holds the rules
/// for that and they are tested, but the write itself waits for 1.1, for the same reason
/// DriverGeek 1.0 reports driver updates without installing them: the reading half is most of the
/// value and none of the risk, and shipping it first means the risky half lands on a codebase
/// people have already been running.
///
/// Logon-triggered scheduled tasks are the fourth place things start from. They need the Task
/// Scheduler API and they arrive with 1.1; until then the Startup screen says they are not listed
/// rather than implying the list is complete.
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
    /// Task Manager records a disabled entry in StartupApproved as a blob whose first byte is odd:
    /// 2 means on, 3 means off. Anything CleanGeek cannot read is reported as ON, because showing
    /// something as disabled when it is not is the more misleading of the two mistakes.
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
