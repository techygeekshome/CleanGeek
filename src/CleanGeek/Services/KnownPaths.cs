using CleanGeek.Core.Services;

namespace CleanGeek.Services;

/// <summary>One folder a target is allowed to work in, and what inside it counts.</summary>
/// <param name="Root">The folder. Everything CleanGeek touches must be underneath it.</param>
/// <param name="Pattern">A file pattern, or "*" for everything.</param>
/// <param name="Recursive">Whether to go into subfolders.</param>
public sealed record CleanupPath(string Root, string Pattern = "*", bool Recursive = true);

/// <summary>
/// Where each catalogue target actually lives on this machine.
///
/// This is the only place in CleanGeek that names a folder, and it is a fixed list. There are no
/// wildcards over folders, no sweeps of the profile looking for things that look like rubbish,
/// and nothing here is built from a pattern the user can type. Everything that comes out of here
/// still goes through PathSafety before anything is deleted.
/// </summary>
public static class KnownPaths
{
    private static string Local => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string Roaming => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string WinDir => Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static string SystemDrive => Path.GetPathRoot(WinDir) ?? @"C:\";

    public static IReadOnlyList<CleanupPath> For(string targetId) => targetId switch
    {
        "temp-user" =>
        [
            new CleanupPath(Path.GetTempPath().TrimEnd('\\'))
        ],

        "temp-windows" =>
        [
            new CleanupPath(Path.Combine(WinDir, "Temp"))
        ],

        "update-leftovers" =>
        [
            new CleanupPath(Path.Combine(WinDir, "SoftwareDistribution", "Download"))
        ],

        "delivery-optimisation" =>
        [
            new CleanupPath(Path.Combine(WinDir, "ServiceProfiles", "NetworkService", "AppData",
                                         "Local", "Microsoft", "Windows", "DeliveryOptimization"))
        ],

        "crash-dumps" =>
        [
            new CleanupPath(Path.Combine(Local, "CrashDumps")),
            new CleanupPath(Path.Combine(Local, "Microsoft", "Windows", "WER", "ReportQueue")),
            new CleanupPath(Path.Combine(Local, "Microsoft", "Windows", "WER", "ReportArchive"))
        ],

        "thumbnail-cache" =>
        [
            new CleanupPath(Path.Combine(Local, "Microsoft", "Windows", "Explorer"),
                            "thumbcache_*.db", Recursive: false)
        ],

        "icon-cache" =>
        [
            new CleanupPath(Path.Combine(Local, "Microsoft", "Windows", "Explorer"),
                            "iconcache_*.db", Recursive: false)
        ],

        "memory-dump" =>
        [
            new CleanupPath(WinDir, "MEMORY.DMP", Recursive: false),
            new CleanupPath(Path.Combine(WinDir, "Minidump"))
        ],

        "windows-old" =>
        [
            new CleanupPath(Path.Combine(SystemDrive, "Windows.old"))
        ],

        "browser-cache" => Browsers.SelectMany(b => b.Cache).ToList(),
        "browser-cookies" => Browsers.SelectMany(b => b.Cookies).ToList(),
        "browser-history" => Browsers.SelectMany(b => b.History).ToList(),
        "browser-form-data" => Browsers.SelectMany(b => b.FormData).ToList(),
        "browser-download-list" => Browsers.SelectMany(b => b.DownloadList).ToList(),

        // The Recycle Bin is not a folder CleanGeek walks. Windows owns it, and the shell has an
        // API for both the size and the emptying - see RecycleBin.cs.
        Catalogue.RecycleBinId => [],

        _ => []
    };

    /// <summary>
    /// MEMORY.DMP sits directly in the Windows folder, which PathSafety refuses as a root. The
    /// allowed root for a single named file is the file's own folder plus that exact name, so the
    /// scan hands PathSafety the file's parent and the run only ever asks about that one file.
    /// </summary>
    public static IReadOnlyList<string> RootsFor(string targetId) =>
        For(targetId).Select(p => p.Root).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private sealed record BrowserProfile(
        IReadOnlyList<CleanupPath> Cache,
        IReadOnlyList<CleanupPath> Cookies,
        IReadOnlyList<CleanupPath> History,
        IReadOnlyList<CleanupPath> FormData,
        IReadOnlyList<CleanupPath> DownloadList);

    /// <summary>
    /// Chromium keeps history, downloads and form data in the same SQLite files, so "download
    /// history" and "browsing history" overlap on disk. CleanGeek does not pretend otherwise: the
    /// download list is a row inside History, and deleting the file takes both. That is what the
    /// cost line on the target says, and it is why the two are separate ticks rather than one.
    /// </summary>
    private static IReadOnlyList<BrowserProfile> Browsers
    {
        get
        {
            var list = new List<BrowserProfile>();

            foreach (var chromium in new[]
                     {
                         Path.Combine(Local, "Google", "Chrome", "User Data", "Default"),
                         Path.Combine(Local, "Microsoft", "Edge", "User Data", "Default"),
                         Path.Combine(Local, "BraveSoftware", "Brave-Browser", "User Data", "Default"),
                         Path.Combine(Local, "Vivaldi", "User Data", "Default")
                     })
            {
                list.Add(new BrowserProfile(
                    Cache: [new CleanupPath(Path.Combine(chromium, "Cache")),
                            new CleanupPath(Path.Combine(chromium, "Code Cache")),
                            new CleanupPath(Path.Combine(chromium, "GPUCache"))],
                    Cookies: [new CleanupPath(Path.Combine(chromium, "Network"), "Cookies*", Recursive: false)],
                    History: [new CleanupPath(chromium, "History*", Recursive: false)],
                    FormData: [new CleanupPath(chromium, "Web Data*", Recursive: false)],
                    DownloadList: []));
            }

            var firefox = Path.Combine(Roaming, "Mozilla", "Firefox", "Profiles");
            var firefoxCache = Path.Combine(Local, "Mozilla", "Firefox", "Profiles");
            list.Add(new BrowserProfile(
                Cache: [new CleanupPath(firefoxCache)],
                Cookies: [new CleanupPath(firefox, "cookies.sqlite*")],
                History: [new CleanupPath(firefox, "places.sqlite*")],
                FormData: [new CleanupPath(firefox, "formhistory.sqlite*")],
                DownloadList: []));

            return list;
        }
    }
}
