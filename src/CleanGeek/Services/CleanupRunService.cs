using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Services;

/// <summary>What one target's clean actually did.</summary>
public sealed record CleanOutcome(string TargetId, long BytesRemoved, int FilesRemoved, int Skipped, string? Refused);

public sealed record CleanReport(IReadOnlyList<CleanOutcome> Outcomes)
{
    public long BytesRemoved => Outcomes.Sum(o => o.BytesRemoved);
    public int FilesRemoved => Outcomes.Sum(o => o.FilesRemoved);
    public int Skipped => Outcomes.Sum(o => o.Skipped);
    public IReadOnlyList<CleanOutcome> Refusals => Outcomes.Where(o => o.Refused is not null).ToList();
}

/// <summary>
/// The second step. Every file goes through PathSafety, and every target goes through DeleteGate,
/// before anything is removed - and both of those live in CleanGeek.Core where they are proven on
/// every build. This class does the walking and the counting; it makes none of the decisions.
/// </summary>
public sealed class CleanupRunService
{
    /// <param name="selectedIds">The ticked targets. Nothing outside this list is considered.</param>
    /// <param name="bulk">True when the person pressed a clean-everything button rather than choosing one target.</param>
    public CleanReport Clean(IReadOnlyCollection<string> selectedIds, bool bulk)
    {
        var elevated = Elevation.IsElevated;
        var outcomes = new List<CleanOutcome>();

        foreach (var target in Catalogue.All)
        {
            var selected = selectedIds.Contains(target.Id, StringComparer.Ordinal);

            var gate = new DeleteContext(
                Selected: selected,
                Elevated: elevated,
                Unattended: false,
                PartOfCleanEverything: bulk,
                PathAllowed: true,
                FileInUse: false);

            if (DeleteGate.Refuse(target, gate) is { } refusal)
            {
                // Only worth reporting when the person actually asked for this target.
                if (selected) outcomes.Add(new CleanOutcome(target.Id, 0, 0, 0, refusal));
                continue;
            }

            outcomes.Add(target.Id == Catalogue.RecycleBinId
                ? EmptyBin(target)
                : CleanFiles(target, gate));
        }

        var report = new CleanReport(outcomes);
        Log.Write($"Clean: {ByteSize.Format(report.BytesRemoved)} removed, {report.FilesRemoved} files, " +
                  $"{report.Skipped} left alone.");

        return report;
    }

    private static CleanOutcome EmptyBin(CleanupTarget target)
    {
        var (bytes, items) = RecycleBin.Measure();

        return RecycleBin.Empty()
            ? new CleanOutcome(target.Id, bytes, items, 0, null)
            : new CleanOutcome(target.Id, 0, 0, items, "Windows would not empty the Recycle Bin.");
    }

    private static CleanOutcome CleanFiles(CleanupTarget target, DeleteContext gate)
    {
        var roots = KnownPaths.RootsFor(target.Id);
        long removed = 0;
        var files = 0;
        var skipped = 0;
        var unreadable = new List<string>();

        foreach (var spec in KnownPaths.For(target.Id))
        {
            foreach (var file in CleanupScanService.Enumerate(spec, unreadable))
            {
                var path = file.FullName;

                if (!PathSafety.IsSafeToDelete(path, roots))
                {
                    skipped++;
                    continue;
                }

                long size;
                try
                {
                    size = file.Length;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    skipped++;
                    continue;
                }

                // The gate is asked again for this file, because whether it is in use is a fact
                // about the file rather than about the target.
                if (!DeleteGate.CanDelete(target, gate with { FileInUse = false }))
                {
                    skipped++;
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
                    // Held open by something. Leave it alone - this is the expected outcome for a
                    // browser cache while the browser is running, and it is not a failure.
                    skipped++;
                }
                catch (UnauthorizedAccessException)
                {
                    skipped++;
                }
            }

            RemoveEmptyFolders(spec, roots, ref skipped);
        }

        return new CleanOutcome(target.Id, removed, files, skipped, null);
    }

    /// <summary>
    /// Tidies up the folders that are left behind once their files have gone. Only empty ones,
    /// only inside the target's own roots, and never the root itself - PathSafety enforces the
    /// last of those, so a bug here cannot remove somebody's Temp folder.
    /// </summary>
    private static void RemoveEmptyFolders(CleanupPath spec, IReadOnlyList<string> roots, ref int skipped)
    {
        if (!spec.Recursive) return;

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
            if (!PathSafety.IsSafeToDelete(folder.FullName, roots)) continue;

            try
            {
                if (folder.EnumerateFileSystemInfos().Any()) continue;
                folder.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                skipped++;
            }
        }
    }
}
