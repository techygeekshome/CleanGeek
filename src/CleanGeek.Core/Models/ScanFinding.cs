namespace CleanGeek.Core.Models;

/// <summary>
/// What a scan found for one target. Bytes and files, and nothing that resembles a score, a
/// grade or a "health percentage" - those exist to make an empty result look like a problem.
/// </summary>
public sealed record ScanFinding(string TargetId, long Bytes, int Files)
{
    public static ScanFinding Empty(string targetId) => new(targetId, 0, 0);

    public bool FoundAnything => Bytes > 0 || Files > 0;
}
