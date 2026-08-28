using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>
/// The guard that stands between a target and the file system.
///
/// Every path CleanGeek is about to delete goes through here first, and the answer is no unless
/// the path is provably inside somewhere the application was told it may work. It refuses when it
/// is unsure rather than allowing when it cannot decide, because the failure mode on the other
/// side of this function is somebody's documents.
///
/// There are two independent halves, and the second exists because the first can be wrong:
///
///   The ALLOW half - the path must sit under one of the roots the caller passed in.
///   The REFUSE half - a list of places that are never deleted from, whatever roots were passed.
///
/// The refuse half is what catches a mistake in the roots, so it has to hold for a whole subtree
/// and not merely for the folder itself: refusing C:\Users\Sam\Documents while allowing
/// C:\Users\Sam\Documents\tax.pdf would be no protection at all.
///
/// Two names are deliberately refused as the folder ONLY, not as a subtree: the Windows folder
/// and Users. Real cleanup targets live underneath both of them - Windows\Temp, the update
/// download cache, the memory dump, and every per-user cache - so a subtree refusal there would
/// refuse the application's own work. Underneath them the specific dangerous children are named
/// instead, and those ARE subtree refusals.
///
/// The name checks are positional rather than a suffix match, which is why C:\Windows is refused
/// while C:\Windows.old\Windows - the largest folder inside a target the application legitimately
/// cleans - is not.
/// </summary>
public static class PathSafety
{
    /// <summary>Refused as the folder itself. Legitimate targets live underneath these.</summary>
    private static readonly string[] DriveRootFolderOnly = ["windows", "users"];

    /// <summary>Refused along with everything underneath them, at the root of any drive.</summary>
    private static readonly string[] DriveRootSubtree =
    [
        "program files", "program files (x86)", "programdata",
        "$recycle.bin", "system volume information", "perflogs", "recovery"
    ];

    /// <summary>Refused along with everything underneath them, directly under the Windows folder.</summary>
    private static readonly string[] WindowsSubtree =
    [
        "system32", "syswow64", "winsxs", "servicing", "boot", "fonts", "prefetch", "system"
    ];

    /// <summary>Refused along with everything underneath them, directly under a user's profile.</summary>
    private static readonly string[] ProfileSubtree =
    [
        "documents", "desktop", "downloads", "pictures", "videos", "music",
        "onedrive", "favorites", "links", "searches", "contacts", "saved games"
    ];

    /// <summary>
    /// The refusal reason, or null when the path may be deleted. Callers show this text, so it is
    /// written to be read by a person rather than logged and forgotten.
    /// </summary>
    public static string? Refuse(string? path, IReadOnlyList<string> allowedRoots)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "There is no path to delete.";

        var p = Normalise(path);

        if (Segments(p).Any(s => s == ".."))
            return "The path walks upwards out of its folder.";

        if (!IsRooted(p))
            return "The path is not an absolute path.";

        if (IsDriveRoot(p))
            return "That is the root of a drive.";

        if (SystemFolder(p) is { } reason)
            return $"{path} {reason}";

        if (allowedRoots.Count == 0)
            return "No folder has been allowed for this target.";

        foreach (var root in allowedRoots)
        {
            var r = Normalise(root);
            if (r.Length == 0 || !IsRooted(r)) continue;

            // Equal to the root means "delete the folder itself", which is never what is wanted:
            // CleanGeek empties folders, it does not remove them.
            if (string.Equals(p, r, StringComparison.OrdinalIgnoreCase))
                return "That is the folder itself, not something inside it.";

            var prefix = r.EndsWith('\\') ? r : r + '\\';
            if (p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;
        }

        return $"{path} is outside every folder this target is allowed to touch.";
    }

    public static bool IsSafeToDelete(string? path, IReadOnlyList<string> allowedRoots) =>
        Refuse(path, allowedRoots) is null;

    /// <summary>
    /// The refusal for one file against one specification - the form the cleaner actually uses.
    ///
    /// This exists because handing PathSafety a target's roots all at once is too generous. The
    /// memory dump lives at C:\Windows\MEMORY.DMP, so its root is the Windows folder; approving
    /// everything under that root would approve the whole operating system, and only the narrowness
    /// of the enumeration would be keeping it safe. A specification that names one file authorises
    /// exactly that file.
    /// </summary>
    public static string? RefuseForSpec(string? path, CleanupPath spec)
    {
        if (Refuse(path, [spec.Root]) is { } reason) return reason;

        if (!spec.IsSingleNamedFile) return null;

        var name = Segments(Normalise(path!)).LastOrDefault() ?? "";
        return string.Equals(name, spec.Pattern, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"{path} is not the file this target names ({spec.Pattern}).";
    }

    public static bool IsSafeForSpec(string? path, CleanupPath spec) => RefuseForSpec(path, spec) is null;

    /// <summary>Why this path is a system folder, or null when it is not one.</summary>
    private static string? SystemFolder(string p)
    {
        var parts = Segments(p);

        // A volume's own hidden folders turn up at the root of every drive, so they are refused
        // wherever they appear rather than only on the system drive.
        foreach (var part in parts)
            if (Is(part, "$recycle.bin") || Is(part, "system volume information"))
                return "is a folder Windows owns and CleanGeek never deletes.";

        // parts[0] is the drive ("C:"); on a UNC path the server and the share take two slots,
        // so everything below is indexed from the first real folder rather than from zero.
        var first = p.StartsWith(@"\\", StringComparison.Ordinal) ? 2 : 1;
        if (parts.Length <= first) return null;

        if (DriveRootSubtree.Any(x => Is(parts[first], x)))
            return "is inside a system folder CleanGeek never deletes.";

        if (parts.Length == first + 1 && DriveRootFolderOnly.Any(x => Is(parts[first], x)))
            return "is a system folder CleanGeek never deletes.";

        if (parts.Length > first + 1 && Is(parts[first], "windows")
            && WindowsSubtree.Any(x => Is(parts[first + 1], x)))
            return "is part of Windows itself.";

        if (Is(parts[first], "users"))
        {
            // C:\Users\Sam - the profile root. Its caches are fair game, the folder is not.
            if (parts.Length == first + 2)
                return "is somebody's profile folder.";

            if (parts.Length > first + 2 && ProfileSubtree.Any(x => Is(parts[first + 2], x)))
                return "is one of your own folders, not a cache.";
        }

        return null;
    }

    private static bool Is(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string[] Segments(string p) => p.Split('\\', StringSplitOptions.RemoveEmptyEntries);

    private static string Normalise(string path)
    {
        var p = path.Trim().Replace('/', '\\');

        var unc = p.StartsWith(@"\\", StringComparison.Ordinal);
        var body = unc ? p[2..] : p;
        while (body.Contains(@"\\", StringComparison.Ordinal))
            body = body.Replace(@"\\", @"\", StringComparison.Ordinal);

        // Windows silently drops trailing dots and spaces from every component, so "System32."
        // opens System32. Dropping them here too means the name checks below cannot be stepped
        // around by adding one. A component that is nothing but dots is left alone, because that
        // is "." or ".." and the upwards-walk check still has to be able to see it.
        body = string.Join('\\', body.Split('\\')
            .Select(s => s.Length > 0 && s.All(c => c == '.') ? s : s.TrimEnd(' ', '.')));

        p = unc ? @"\\" + body : body;

        return p.Length > 3 ? p.TrimEnd('\\') : p;
    }

    private static bool IsRooted(string p) =>
        (p.Length >= 3 && char.IsLetter(p[0]) && p[1] == ':' && p[2] == '\\')
        || p.StartsWith(@"\\", StringComparison.Ordinal);

    private static bool IsDriveRoot(string p) =>
        p.Length is 2 or 3 && char.IsLetter(p[0]) && p[1] == ':';
}
