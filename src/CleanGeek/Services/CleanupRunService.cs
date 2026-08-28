using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Services;

/// <summary>The result of cleaning one target.</summary>
/// <param name="Refused">Why the target was not cleaned, or null when it was.</param>
/// <param name="InUse">Files another process had open. Expected, and not a failure.</param>
/// <param name="Blocked">Files the safety check refused. Not expected.</param>
public sealed record CleanOutcome(
    string TargetId,
    long BytesRemoved,
    int FilesRemoved,
    int InUse,
    int Blocked,
    string? Refused);

public sealed record CleanReport(IReadOnlyList<CleanOutcome> Outcomes)
{
    public long BytesRemoved => Outcomes.Sum(o => o.BytesRemoved);
    public int FilesRemoved => Outcomes.Sum(o => o.FilesRemoved);
    public int InUse => Outcomes.Sum(o => o.InUse);
    public int Blocked => Outcomes.Sum(o => o.Blocked);
    public IReadOnlyList<CleanOutcome> Refusals => Outcomes.Where(o => o.Refused is not null).ToList();
}

/// <summary>
/// Removes what a scan found. Every file is checked against the spec that produced it and every
/// target goes through DeleteGate; this class only walks and counts.
/// </summary>
public sealed class CleanupRunService
{
    /// <param name="selectedIds">The ticked targets. Nothing outside this list is considered.</param>
    /// <param name="bulk">True when more than one target is being cleaned at once.</param>
    public CleanReport Clean(IReadOnlyCollection<string> selectedIds, bool bulk)
    {
        var elevated = Elevation.IsElevated;
        var outcomes = new List<CleanOutcome>();

        foreach (var target in Catalogue.All)
        {
            var selected = selectedIds.Contains(target.Id, StringComparer.Ordinal);

            // PathAllowed and FileInUse are per file and are answered inside CleanFiles; at
            // target level they carry the neutral values.
            var gate = new DeleteContext(
                Selected: selected,
                Elevated: elevated,
                Unattended: false,
                PartOfCleanEverything: bulk,
                PathAllowed: true,
                FileInUse: false);

            if (DeleteGate.Refuse(target, gate) is { } refusal)
            {
                // Only report a refusal for targets that were actually selected.
                if (selected) outcomes.Add(new CleanOutcome(target.Id, 0, 0, 0, 0, refusal));
                continue;
            }

            outcomes.Add(target.Id == Catalogue.RecycleBinId
                ? EmptyBin(target)
                : CleanFiles(target, gate));
        }

        var report = new CleanReport(outcomes);
        Log.Write($"Clean: {ByteSize.Format(report.BytesRemoved)} removed, {report.FilesRemoved} files, " +
                  $"{report.InUse} in use, {report.Blocked} refused by the safety check.");

        return report;
    }

    private static CleanOutcome EmptyBin(CleanupTarget target)
    {
        var (bytes, items) = RecycleBin.Measure();

        if (!RecycleBin.Empty())
            return new CleanOutcome(target.Id, 0, 0, items, 0, "Windows would not empty the Recycle Bin.");

        // SHQueryRecycleBin can fail while the empty succeeds, so report zero rather than a size
        // that was never measured.
        return bytes > 0 || items > 0
            ? new CleanOutcome(target.Id, bytes, items, 0, 0, null)
            : new CleanOutcome(target.Id, 0, 0, 0, 0, null);
    }

    private static CleanOutcome CleanFiles(CleanupTarget target, DeleteContext gate)
    {
        long removed = 0;
        var files = 0;
        var inUse = 0;
        var blocked = 0;

        foreach (var spec in KnownPaths.For(target.Id))
        {
            var unreadable = new List<string>();

            foreach (var file in CleanupScanService.Enumerate(spec, unreadable))
            {
                var path = file.FullName;

                // Checked against this spec alone, not the target's roots pooled together.
                var pathAllowed = PathSafety.IsSafeForSpec(path, spec);

                long size;
                try
                {
                    size = file.Length;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    inUse++;
                    continue;
                }

                if (DeleteGate.Refuse(target, gate with { PathAllowed = pathAllowed }) is { } refusal)
                {
                    blocked++;
                    Log.Write($"Left alone: {path} - {refusal}");
                    continue;
                }

                try
                {
                    file.Delete();
                    removed += size;
                    files++;
                }
                catch (IOException)
                {
                    // Expected while the owning application is running.
                    inUse++;
                }
                catch (UnauthorizedAccessException)
                {
                    inUse++;
                }
            }

            // Unreadable folders still hold their contents.
            inUse += unreadable.Count;

            RemoveEmptyFolders(spec);
        }

        return new CleanOutcome(target.Id, removed, files, inUse, blocked, null);
    }

    /// <summary>
    /// Removes empty folders left behind inside the spec's root. PathSafety keeps the root itself
    /// out of reach. Skipped for pattern-limited specs, which have no claim on unrelated folders
    /// in the same tree.
    /// </summary>
    private static void RemoveEmptyFolders(CleanupPath spec)
    {
        if (!spec.Recursive || spec.Pattern != "*") return;

        DirectoryInfo root;
        try
        {
            root = new DirectoryInfo(spec.Root);
            if (!root.Exists) return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return;
        }

        List<DirectoryInfo> folders;
        try
        {
            folders = root
                .EnumerateDirectories("*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint
                })
                .OrderByDescending(d => d.FullName.Length)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var folder in folders)
        {
            if (!PathSafety.IsSafeToDelete(folder.FullName, [spec.Root])) continue;

            try
            {
                if (folder.EnumerateFileSystemInfos().Any()) continue;
                folder.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // In use, or no longer empty.
            }
        }
    }
}
