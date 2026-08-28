using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>
/// The arithmetic behind the two numbers on the Clean screen: what was found, and what is
/// actually going to be removed.
///
/// This exists as its own tested thing because getting it wrong is the classic cleaner lie. A
/// screen that says "28.3 GB found" and leaves that number sitting next to a Clean button when
/// only 1.2 GB is ticked has told the user something untrue, and it is the exact trick the paid
/// tools use. Selected never counts a target that is not ticked, and never exceeds Found.
/// </summary>
public static class SizeReport
{
    public static long Found(IEnumerable<ScanFinding> findings) =>
        findings.Sum(f => Math.Max(0, f.Bytes));

    public static long Selected(IEnumerable<ScanFinding> findings, IReadOnlyCollection<string> selectedIds) =>
        findings
            .Where(f => selectedIds.Contains(f.TargetId, StringComparer.Ordinal))
            .Sum(f => Math.Max(0, f.Bytes));

    public static int SelectedCategories(IEnumerable<ScanFinding> findings, IReadOnlyCollection<string> selectedIds) =>
        findings
            .Where(f => f.Bytes > 0 && selectedIds.Contains(f.TargetId, StringComparer.Ordinal))
            .Select(f => f.TargetId)
            .Distinct(StringComparer.Ordinal)
            .Count();

    public static long ForCategory(IEnumerable<ScanFinding> findings, CleanupCategory category) =>
        findings
            .Where(f => Catalogue.ById(f.TargetId)?.Category == category)
            .Sum(f => Math.Max(0, f.Bytes));

    /// <summary>
    /// The line under the Clean button. When nothing is ticked it says so, rather than showing a
    /// big number with a button next to it.
    /// </summary>
    public static string Headline(IEnumerable<ScanFinding> findings, IReadOnlyCollection<string> selectedIds)
    {
        var list = findings as IList<ScanFinding> ?? findings.ToList();
        var found = Found(list);
        var selected = Selected(list, selectedIds);

        if (found == 0)
            return "Nothing to clean. That is a real answer - this machine is tidy.";

        if (selected == 0)
            return $"{ByteSize.Format(found)} found. Nothing is ticked, so nothing will be removed.";

        var groups = SelectedCategories(list, selectedIds);
        return $"{ByteSize.Format(found)} found · {ByteSize.Format(selected)} selected " +
               $"across {groups} {(groups == 1 ? "item" : "items")}";
    }
}
