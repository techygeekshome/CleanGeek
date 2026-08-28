namespace CleanGeek.Core.Services;

/// <summary>
/// The guard that stands between a target and the file system.
///
/// Every path CleanGeek is about to delete goes through here first, and the answer is no unless
/// the path is provably inside somewhere the application was told it may work. It refuses when it
/// is unsure rather than allowing when it cannot decide, because the failure mode on the other
/// side of this function is somebody's documents.
///
/// The system-folder rules are structural rather than a suffix match, and that is not fussiness:
/// a suffix match on "\windows" would refuse C:\Windows.old\Windows, which is the largest folder
/// inside a target the application legitimately cleans. Position in the path is what makes a
/// folder a system folder, so position is what is checked.
/// </summary>
public static class PathSafety
{
    /// <summary>Directly under a drive root, these are never touched.</summary>
    private static readonly string[] AtDriveRoot =
    [
        "windows", "program files", "program files (x86)", "programdata",
        "users", "$recycle.bin", "system volume information", "perflogs", "recovery"
    ];

    /// <summary>Directly under the Windows folder, these are never touched.</summary>
    private static readonly string[] UnderWindows =
    [
        "system32", "syswow64", "winsxs", "servicing", "boot", "fonts", "prefetch", "system"
    ];

    /// <summary>Directly under a user's profile, these are never touched.</summary>
    private static readonly string[] UnderProfile =
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

        if (HasSegment(p, ".."))
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

    /// <summary>Why this path is a system folder, or null when it is not one.</summary>
    private static string? SystemFolder(string p)
    {
        var parts = p.Split('\\', StringSplitOptions.RemoveEmptyEntries);

        // A volume's own hidden folders turn up at the root of every drive, so they are refused
        // wherever they appear rather than only on the system drive.
        foreach (var part in parts)
            if (Is(part, "$recycle.bin") || Is(part, "system volume information"))
                return "is a folder Windows owns and CleanGeek never deletes.";

        // parts[0] is the drive ("C:") or, on a UNC path, the server name.
        if (parts.Length == 2 && AtDriveRoot.Any(x => Is(parts[1], x)))
            return "is a system folder CleanGeek never deletes.";

        if (parts.Length == 3 && Is(parts[1], "windows") && UnderWindows.Any(x => Is(parts[2], x)))
            return "is part of Windows itself.";

        // C:\Users\Sam - the profile root. Its contents are fair game, the folder is not.
        if (parts.Length == 3 && Is(parts[1], "users"))
            return "is somebody's profile folder.";

        if (parts.Length == 4 && Is(parts[1], "users") && UnderProfile.Any(x => Is(parts[3], x)))
            return "is one of your own folders, not a cache.";

        return null;
    }

    private static bool Is(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool HasSegment(string p, string segment) =>
        p.Split('\\').Any(s => string.Equals(s, segment, StringComparison.Ordinal));

    private static string Normalise(string path)
    {
        var p = path.Trim().Replace('/', '\\');

        var unc = p.StartsWith(@"\\", StringComparison.Ordinal);
        var body = unc ? p[2..] : p;
        while (body.Contains(@"\\", StringComparison.Ordinal))
            body = body.Replace(@"\\", @"\", StringComparison.Ordinal);
        p = unc ? @"\\" + body : body;

        return p.Length > 3 ? p.TrimEnd('\\') : p;
    }

    private static bool IsRooted(string p) =>
        (p.Length >= 3 && char.IsLetter(p[0]) && p[1] == ':' && p[2] == '\\')
        || p.StartsWith(@"\\", StringComparison.Ordinal);

    private static bool IsDriveRoot(string p) =>
        p.Length is 2 or 3 && char.IsLetter(p[0]) && p[1] == ':';
}
