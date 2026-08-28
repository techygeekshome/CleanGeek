using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Services;

/// <summary>
/// Where each catalogue target lives on this machine. A fixed list: no folder wildcards and
/// nothing built from user input. Everything here still goes through PathSafety before a delete.
/// </summary>
public static class KnownPaths
{
    private static string Local => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string Roaming => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string WinDir => Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static string SystemDrive => Path.GetPathRoot(WinDir) ?? @"C:\";

    /// <summary>
    /// The user's Temp folder. Path.GetTempPath() falls back to the user profile when TMP and TEMP
    /// are both unset, which would give a recursive delete root over the whole profile, so the
    /// result is only used when it looks like a Temp folder.
    /// </summary>
    private static string UserTemp
    {
        get
        {
            var reported = Path.GetTempPath().TrimEnd('\\');
            var parts = reported.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            var looksRight = parts.Length >= 3
                             && string.Equals(parts[^1], "Temp", StringComparison.OrdinalIgnoreCase);

            return looksRight ? reported : Path.Combine(Local, "Temp");
        }
    }

    public static IReadOnlyList<CleanupPath> For(string targetId) => targetId switch
    {
        "temp-user" =>
        [
            new CleanupPath(UserTemp)
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

        // The bin is not walked as a folder; the shell API handles size and emptying. See RecycleBin.cs.
        Catalogue.RecycleBinId => [],

        _ => []
    };

    /// <summary>
    /// Every distinct folder a target may touch. For reporting only; deletes are authorised per
    /// file against a single spec.
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
    /// Per-browser profile paths. Chromium stores the download list inside the History file, so
    /// DownloadList is empty and clearing history takes both.
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
