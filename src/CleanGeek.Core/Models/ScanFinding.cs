namespace CleanGeek.Core.Models;

/// <summary>What a scan found for one target.</summary>
public sealed record ScanFinding(string TargetId, long Bytes, int Files)
{
    public static ScanFinding Empty(string targetId) => new(targetId, 0, 0);

    public bool FoundAnything => Bytes > 0 || Files > 0;
}
