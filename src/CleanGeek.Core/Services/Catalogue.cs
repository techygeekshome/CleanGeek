using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>The fixed list of cleanup targets, and the list of things that are never cleaned.</summary>
public static class Catalogue
{
    public const string RecycleBinId = "recycle-bin";

    public static IReadOnlyList<CleanupTarget> All { get; } =
    [
        // Windows
        new("temp-user", CleanupCategory.Windows,
            "Your temporary files",
            "The TEMP folder in your own profile, where installers and applications leave working files behind.",
            "",
            CleanupRisk.Rebuilds, TickedByDefault: true),

        new("temp-windows", CleanupCategory.Windows,
            "Windows temporary files",
            "The machine-wide Windows Temp folder.",
            "",
            CleanupRisk.Disposable, TickedByDefault: true, NeedsAdmin: true),

        new("update-leftovers", CleanupCategory.Windows,
            "Windows Update leftovers",
            "Update packages Windows already installed and kept a copy of.",
            "Windows will download an update again if you ask it to reinstall one.",
            CleanupRisk.Disposable, TickedByDefault: true, NeedsAdmin: true),

        new("delivery-optimisation", CleanupCategory.Windows,
            "Delivery Optimisation cache",
            "Update files Windows keeps so it can share them with other PCs on your network.",
            "",
            CleanupRisk.Disposable, TickedByDefault: true, NeedsAdmin: true),

        new("crash-dumps", CleanupCategory.Windows,
            "Crash dumps and error reports",
            "What Windows wrote down the last time something stopped working.",
            "Nothing, unless a support desk asked you to send them one.",
            CleanupRisk.Disposable, TickedByDefault: true),

        new("thumbnail-cache", CleanupCategory.Windows,
            "Thumbnail cache",
            "The picture previews File Explorer draws in folders.",
            "Folders full of photos will redraw their previews once, then be as fast as before.",
            CleanupRisk.Rebuilds, TickedByDefault: true),

        new("icon-cache", CleanupCategory.Windows,
            "Icon cache",
            "The icons Windows keeps ready so the desktop draws quickly.",
            "",
            CleanupRisk.Rebuilds, TickedByDefault: true),

        new("memory-dump", CleanupCategory.Windows,
            "System memory dump",
            "The file Windows writes after a blue screen. Usually the single largest thing on this list.",
            "You will not be able to analyse your last blue screen afterwards.",
            CleanupRisk.Disposable, TickedByDefault: false, NeedsAdmin: true),

        new("windows-old", CleanupCategory.Windows,
            "Previous Windows installation",
            "The Windows.old folder left by a feature update or an upgrade.",
            "You will not be able to go back to your previous version of Windows - and it also holds a complete copy of the old user profiles, so anything you have not moved across yet goes with it. Windows removes this on its own after ten days.",
            CleanupRisk.Irreversible, TickedByDefault: false, NeedsAdmin: true),

        // Browsers
        new("browser-cache", CleanupCategory.Browsers,
            "Browser caches",
            "Copies of pages and images your browsers kept so they load faster the second time.",
            "Pages load from the network once, then they are quick again.",
            CleanupRisk.Rebuilds, TickedByDefault: true),

        new("browser-cookies", CleanupCategory.Browsers,
            "Cookies",
            "The small files that keep you signed in to websites.",
            "You will be signed out of almost every website you use, on every browser you clean.",
            CleanupRisk.Costly, TickedByDefault: false),

        new("browser-history", CleanupCategory.Browsers,
            "Browsing history",
            "The record of pages you have visited.",
            "The address bar stops suggesting pages you have been to before.",
            CleanupRisk.Costly, TickedByDefault: false),

        new("browser-form-data", CleanupCategory.Browsers,
            "Saved form data",
            "Names, addresses and other details your browser fills in for you.",
            "Gone for good. Your browser will not offer to fill anything in until you type it again.",
            CleanupRisk.Costly, TickedByDefault: false),

        new("browser-download-list", CleanupCategory.Browsers,
            "Download history",
            "The list of what you have downloaded.",
            "The list only. The downloaded files themselves are never touched.",
            CleanupRisk.Disposable, TickedByDefault: false),

        // Recycle Bin
        new(RecycleBinId, CleanupCategory.Bin,
            "Recycle Bin",
            "Everything you have deleted and not yet emptied.",
            "Emptied permanently. This is the one thing on the list you cannot get back.",
            CleanupRisk.Irreversible, TickedByDefault: false)
    ];

    /// <summary>Things this application deliberately never cleans, with the reason shown in the UI.</summary>
    public static IReadOnlyList<(string Thing, string Why)> NeverCleaned { get; } =
    [
        ("Saved passwords",
         "Not off by default - absent. There is no version of this application that deletes a password store."),

        ("The registry",
         "Registry cleaning has no measurable benefit on any supported version of Windows, and the downside is a machine that will not start. CleanGeek has no registry cleaner and will not be getting one."),

        ("The Prefetch folder",
         "Windows uses it to start your applications faster and rebuilds it by making everything slower first. Emptying it is a loss, not a gain."),

        ("System Restore points",
         "They are what you fall back on when something else goes wrong. Windows manages their disk budget itself."),

        ("The component store (WinSxS)",
         "It is not a folder to delete from - it is serviced. If it has genuinely grown too large, the supported tool is DISM /Online /Cleanup-Image /StartComponentCleanup, and that is a decision to make deliberately, not as part of a sweep.")
    ];

    public static CleanupTarget? ById(string id) =>
        All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.Ordinal));

    public static IReadOnlyList<CleanupTarget> InCategory(CleanupCategory category) =>
        All.Where(t => t.Category == category).ToList();

    /// <summary>The default tick set. Nothing with a cost to the user is ticked by default.</summary>
    public static IReadOnlyList<string> DefaultSelection() =>
        All.Where(t => t.TickedByDefault).Select(t => t.Id).ToList();

    /// <summary>
    /// Resolves the saved selection into ticked ids. Null means never chosen, so defaults apply;
    /// an empty list is a real choice and is not replaced by the defaults. Unknown ids are dropped.
    /// </summary>
    public static IReadOnlyList<string> Resolve(IReadOnlyList<string>? saved)
    {
        if (saved is null) return DefaultSelection();

        return saved
            .Where(id => ById(id) is not null)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
