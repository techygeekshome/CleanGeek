namespace CleanGeek.Core.Models;

/// <summary>One cleanup target, described in the words shown to the user.</summary>
/// <param name="Id">Stable identifier, written to settings, so it does not change between versions.</param>
/// <param name="Category">Which group it appears under.</param>
/// <param name="Title">The on-screen name.</param>
/// <param name="What">One sentence describing what it is.</param>
/// <param name="Cost">One sentence describing what is lost. Empty when nothing is.</param>
/// <param name="Risk">Risk level, used for sorting and colouring.</param>
/// <param name="TickedByDefault">True only for caches and temporary files; nothing with a cost is ticked.</param>
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
