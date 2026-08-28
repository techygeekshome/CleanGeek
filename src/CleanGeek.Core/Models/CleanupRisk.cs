namespace CleanGeek.Core.Models;

/// <summary>What deleting a target costs the user.</summary>
public enum CleanupRisk
{
    /// <summary>Rebuilds itself. Caches, temporary files, thumbnails.</summary>
    Rebuilds,

    /// <summary>Not recoverable, but disposable. Crash dumps, old logs, update leftovers.</summary>
    Disposable,

    /// <summary>A loss the user will notice. Cookies, history, saved form data.</summary>
    Costly,

    /// <summary>Cannot be undone and can break a rollback. Old Windows installations, Recycle Bin.</summary>
    Irreversible
}
