using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Services;

/// <summary>What one target's clean actually did.</summary>
/// <param name="Refused">Why the target was not cleaned at all, or null when it was.</param>
/// <param name="InUse">Files something else had open. Expected, and not a failure.</param>
/// <param name="Blocked">Files the safety check refused. Not expected - worth telling someone about.</param>
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
/// The second step. Every file is checked against the specification that produced it, and every
/// target goes through DeleteGate, before anything is removed - and both of those live in
/// CleanGeek.Core where they are proven on every build. This class does the walking and the
/// counting; it makes none of the decisions.
/// </summary>
public sealed class CleanupRunService
{
    /// <param name="selectedIds">The ticked targets. Nothing outside this list is considered.</param>
    /// <param name="bulk">True when more than one target is going at once, rather than one chosen on its own.</param>
    public CleanReport Clean(IReadOnlyCollection<string> selectedIds, bool bulk)
    {
        var elevated = Elevation.IsElevated;
        var outcomes = new List<CleanOutcome>();

        foreach (var target in Catalogue.All)
        {
            var selected = selectedIds.Contains(target.Id, StringComparer.Ordinal);

            // PathAllowed and FileInUse are facts about individual files, so they are answered
            // per file inside CleanFiles rather than assumed here. At target level they are true
            // and false respectively, which is what "no reason yet to refuse" looks like.
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

        // SHQueryRecycleBin can fail while the empty succeeds. Reporting nothing removed after
        // permanently emptying somebody's bin would be the wrong way round, so say what happened
        // rather than quoting a size that was never measured.
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

                // The file is checked against THIS specification, not against the target's roots
                // pooled together. A target whose root is the Windows folder authorises the one
                // file it names and nothing else underneath it.
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
                    // Held open by something. Leave it alone - this is the expected outcome for a
                    // browser cache while the browser is running, and it is not a failure.
                    inUse++;
                }
                catch (UnauthorizedAccessException)
                {
                    inUse++;
                }
            }

            // A folder that could not be read is a folder whose contents are still there.
            inUse += unreadable.Count;

            RemoveEmptyFolders(spec);
        }

        return new CleanOutcome(target.Id, removed, files, inUse, blocked, null);
    }

    /// <summary>
    /// Tidies up the folders left behind once their files have gone. Only empty ones, only inside
    /// the specification's own root, and never the root itself - PathSafety enforces the last of
    /// those, so a bug here cannot remove somebody's Temp folder.
    ///
    /// It is skipped for a pattern-limited specification. Ticking "Cookies" removes cookie files;
    /// it has no business also removing every empty folder that happens to sit in the same profile.
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
                // Something is using it, or it stopped being empty. Leave it.
            }
        }
    }
}
