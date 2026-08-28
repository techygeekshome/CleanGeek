namespace CleanGeek.Core.Models;

/// <summary>
/// One folder a target may work in, and what inside it counts.
/// </summary>
/// <param name="Root">The folder. Everything CleanGeek touches must be underneath it.</param>
/// <param name="Pattern">A file pattern, or "*" for everything in the folder.</param>
/// <param name="Recursive">Whether to go into subfolders.</param>
public sealed record CleanupPath(string Root, string Pattern = "*", bool Recursive = true)
{
    /// <summary>True when the pattern names exactly one file rather than matching a set.</summary>
    public bool IsSingleNamedFile => !Pattern.Contains('*') && !Pattern.Contains('?');
}
