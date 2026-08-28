using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Services;

public sealed record ScanReport(IReadOnlyList<ScanFinding> Findings, IReadOnlyList<string> Unreadable)
{
    public ScanFinding For(string targetId) =>
        Findings.FirstOrDefault(f => f.TargetId == targetId) ?? ScanFinding.Empty(targetId);
}

/// <summary>Measures what each target would remove. Changes and deletes nothing.</summary>
public sealed class CleanupScanService
{
    public ScanReport Scan()
    {
        var findings = new List<ScanFinding>();
        var unreadable = new List<string>();

        foreach (var target in Catalogue.All)
        {
            if (target.Id == Catalogue.RecycleBinId)
            {
                var (bytes, items) = RecycleBin.Measure();
                findings.Add(new ScanFinding(target.Id, bytes, items));
                continue;
            }

            long total = 0;
            var files = 0;

            foreach (var spec in KnownPaths.For(target.Id))
            {
                foreach (var file in Enumerate(spec, unreadable))
                {
                    try
                    {
                        total += file.Length;
                        files++;
                    }
                    catch (FileNotFoundException)
                    {
                        // Removed between the listing and the size query.
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            findings.Add(new ScanFinding(target.Id, total, files));
        }

        Log.Write($"Scan: {ByteSize.Format(SizeReport.Found(findings))} found across " +
                  $"{findings.Count(f => f.FoundAnything)} of {findings.Count} targets. Nothing was deleted.");

        return new ScanReport(findings, unreadable);
    }

    /// <summary>Lists the files a target covers. A missing folder is the normal case, not an error.</summary>
    internal static IEnumerable<FileInfo> Enumerate(CleanupPath spec, List<string> unreadable)
    {
        DirectoryInfo dir;
        try
        {
            dir = new DirectoryInfo(spec.Root);
            if (!dir.Exists) yield break;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            unreadable.Add(spec.Root);
            yield break;
        }

        // Reparse points are skipped so a junction or symlink in a cache folder cannot walk the
        // enumeration out into the rest of the disk. Setting AttributesToSkip replaces the default
        // of Hidden | System, which is intended: thumbcache files and the memory dump are hidden.
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = spec.Recursive,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            MatchType = MatchType.Simple
        };

        IEnumerator<FileInfo> walker;
        try
        {
            walker = dir.EnumerateFiles(spec.Pattern, options).GetEnumerator();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            unreadable.Add(spec.Root);
            yield break;
        }

        try
        {
            while (true)
            {
                FileInfo current;
                try
                {
                    if (!walker.MoveNext()) break;
                    current = walker.Current;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    unreadable.Add(spec.Root);
                    break;
                }

                yield return current;
            }
        }
        finally
        {
            // A caller that stops early would otherwise leave the find handle open on a folder
            // the cleaner is about to remove.
            walker.Dispose();
        }
    }
}
