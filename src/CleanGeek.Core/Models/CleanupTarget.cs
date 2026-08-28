namespace CleanGeek.Core.Models;

/// <summary>
/// One thing CleanGeek is willing to delete, described in the words the user sees.
/// </summary>
/// <param name="Id">Stable identifier. Written to settings, so it does not change between versions.</param>
/// <param name="Category">Which group it appears under.</param>
/// <param name="Title">What it is called on screen.</param>
/// <param name="What">One sentence: what this actually is.</param>
/// <param name="Cost">One sentence: what you lose. Empty when the honest answer is "nothing".</param>
/// <param name="Risk">How much it costs, as a value the UI can sort and colour by.</param>
/// <param name="TickedByDefault">
/// True only for caches and temporary files. Everything with a cost is off until the person
/// turns it on, which is the single most important default in the application.
/// </param>
/// <param name="NeedsAdmin">True when the target lives outside the user profile.</param>
public sealed record CleanupTarget(
    string Id,
    CleanupCategory Category,
    string Title,
    string What,
    string Cost,
    CleanupRisk Risk,
    bool TickedByDefault,
    bool NeedsAdmin = false)
{
    public bool HasCost => Cost.Length > 0;
}
