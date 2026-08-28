namespace CleanGeek.Core.Models;

/// <summary>
/// What deleting a thing actually costs the person sitting in front of the machine. This is the
/// axis the whole application is organised around, because the difference between "a cache" and
/// "your cookies" is not a difference of size - it is the difference between a clean-up and a
/// bad morning.
/// </summary>
public enum CleanupRisk
{
    /// <summary>Rebuilds itself. Caches, temporary files, thumbnails.</summary>
    Rebuilds,

    /// <summary>Gone for good, but it was disposable. Crash dumps, old logs, update leftovers.</summary>
    Disposable,

    /// <summary>Costs the person something they will notice. Cookies, history, saved form data.</summary>
    Costly,

    /// <summary>Cannot be undone and can break a rollback. Old Windows installations, Recycle Bin.</summary>
    Irreversible
}
