using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>The conditions a delete is checked against.</summary>
/// <param name="Selected">The target is ticked in the current session.</param>
/// <param name="Elevated">Running with administrator rights.</param>
/// <param name="Unattended">A scheduled run rather than a user action.</param>
/// <param name="PartOfCleanEverything">The target was included by a bulk action.</param>
/// <param name="PathAllowed">PathSafety has approved the path.</param>
/// <param name="FileInUse">Another process holds the file open.</param>
public readonly record struct DeleteContext(
    bool Selected,
    bool Elevated,
    bool Unattended,
    bool PartOfCleanEverything,
    bool PathAllowed,
    bool FileInUse);

/// <summary>
/// The single decision point for deletes. The checks below run in a deliberate order and are not
/// settings; nothing in the application turns them off.
/// </summary>
public static class DeleteGate
{
    /// <summary>The reason this may not be deleted, or null when it may.</summary>
    public static string? Refuse(CleanupTarget target, DeleteContext ctx)
    {
        // First, because it outranks everything else including an explicit tick.
        if (ctx.Unattended)
            return "A scheduled run scans and reports. It never deletes anything.";

        if (!ctx.Selected)
            return $"{target.Title} was not selected.";

        // Emptying the bin is not recoverable, so it is never a side effect of a bulk action.
        if (target.Id == Catalogue.RecycleBinId && ctx.PartOfCleanEverything)
            return "The Recycle Bin is only ever emptied when you choose it on its own.";

        if (target.NeedsAdmin && !ctx.Elevated)
            return $"{target.Title} lives outside your profile and needs administrator rights.";

        if (!ctx.PathAllowed)
            return "The path failed the safety check.";

        if (ctx.FileInUse)
            return "Something has this file open. CleanGeek leaves files that are in use alone.";

        return null;
    }

    public static bool CanDelete(CleanupTarget target, DeleteContext ctx) => Refuse(target, ctx) is null;
}
