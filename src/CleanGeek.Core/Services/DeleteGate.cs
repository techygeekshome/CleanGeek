using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>The state of the world at the moment something is about to be deleted.</summary>
/// <param name="Selected">The person ticked this target on this screen, in this session.</param>
/// <param name="Elevated">CleanGeek is running with administrator rights.</param>
/// <param name="Unattended">This is the scheduled run rather than a person pressing a button.</param>
/// <param name="PartOfCleanEverything">The target was swept in by a bulk action rather than chosen on its own.</param>
/// <param name="PathAllowed">PathSafety has already approved the path.</param>
/// <param name="FileInUse">A running process is holding the file open.</param>
public readonly record struct DeleteContext(
    bool Selected,
    bool Elevated,
    bool Unattended,
    bool PartOfCleanEverything,
    bool PathAllowed,
    bool FileInUse);

/// <summary>
/// The one place that decides whether anything may be deleted. Same idea as DriverGeek's
/// InstallGate, and the same rule: the refusals are not settings. There is no flag anywhere in
/// CleanGeek that turns any of these off.
/// </summary>
public static class DeleteGate
{
    /// <summary>The reason this may not be deleted, or null when it may.</summary>
    public static string? Refuse(CleanupTarget target, DeleteContext ctx)
    {
        // A scheduled run measures. It never deletes. This is first because it outranks
        // everything else, including an explicit tick: the person is not at the machine.
        if (ctx.Unattended)
            return "A scheduled run scans and reports. It never deletes anything.";

        if (!ctx.Selected)
            return $"{target.Title} was not selected.";

        // The Recycle Bin is emptied when somebody asks for that specifically, and never as a
        // side effect of a bulk action, because it is the one target with nothing behind it.
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
