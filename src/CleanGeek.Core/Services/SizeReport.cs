using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>
/// Totals for the Clean screen: what was found, and what is selected for removal. Selected never
/// counts an unticked target, so it can never exceed Found.
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

    /// <summary>The summary line under the Clean button.</summary>
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
