using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>
/// Decides whether a path may be deleted. A path must sit under one of the caller's allowed roots
/// and must not be a system location; unknown cases are refused.
/// </summary>
public static class PathSafety
{
    /// <summary>Refused as the folder itself only, since real targets live beneath these.</summary>
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

    /// <summary>The refusal reason, or null when the path may be deleted. Shown to the user.</summary>
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

            // Roots are emptied, never removed.
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
    /// The refusal for one file against one spec. A spec naming a single file authorises only that
    /// file, so a broad root such as the Windows folder cannot approve everything beneath it.
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

        // These exist on every volume, so match them at any depth.
        foreach (var part in parts)
            if (Is(part, "$recycle.bin") || Is(part, "system volume information"))
                return "is a folder Windows owns and CleanGeek never deletes.";

        // Names are matched by position, not by suffix, so C:\Windows is refused but
        // C:\Windows.old\Windows is not. parts[0] is the drive; a UNC path spends two
        // segments on server and share, so index from the first real folder.
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
            // The profile root itself; caches beneath it are still allowed.
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

        // Windows ignores trailing dots and spaces ("System32." opens System32), so strip them
        // or the name checks can be stepped around. Components that are all dots are left alone
        // so the upwards-walk check can still see "..".
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
