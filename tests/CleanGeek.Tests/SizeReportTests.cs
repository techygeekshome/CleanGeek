using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Tests;

public static class SizeReportTests
{
    private const long Gb = 1024L * 1024 * 1024;

    private static readonly ScanFinding[] Findings =
    [
        new("temp-user", 2 * Gb, 4100),
        new("browser-cache", 3 * Gb, 22000),
        new("browser-cookies", 4 * 1024 * 1024, 12),
        new(Catalogue.RecycleBinId, 5 * Gb, 90)
    ];

    public static void Run()
    {
        Check.Section("SizeReport - found");

        Check.Equal("adds up everything found", 10L * Gb + 4 * 1024 * 1024, SizeReport.Found(Findings));
        Check.Equal("an empty scan found nothing", 0L, SizeReport.Found([]));
        Check.Equal("a negative size counts as nothing",
            0L, SizeReport.Found([new ScanFinding("temp-user", -5, 0)]));

        Check.Section("SizeReport - selected never counts what is not ticked");

        Check.Equal("counts only the ticked targets",
            2L * Gb, SizeReport.Selected(Findings, ["temp-user"]));
        Check.Equal("nothing ticked is nothing selected",
            0L, SizeReport.Selected(Findings, []));
        Check.Equal("an id that was not found contributes nothing",
            0L, SizeReport.Selected(Findings, ["windows-old"]));
        Check.That("selected never exceeds found",
            SizeReport.Selected(Findings, Catalogue.All.Select(t => t.Id).ToList()) <= SizeReport.Found(Findings));
        Check.Equal("ticking everything found equals found",
            SizeReport.Found(Findings),
            SizeReport.Selected(Findings, Findings.Select(f => f.TargetId).ToList()));

        Check.Section("SizeReport - how many items are actually going");

        Check.Equal("counts the ticked items that found something",
            2, SizeReport.SelectedCategories(Findings, ["temp-user", "browser-cache"]));
        Check.Equal("does not count a ticked item that found nothing",
            1, SizeReport.SelectedCategories(
                [new ScanFinding("temp-user", Gb, 1), new ScanFinding("browser-cache", 0, 0)],
                ["temp-user", "browser-cache"]));

        Check.Section("SizeReport - by category");

        Check.Equal("adds up a category",
            3 * Gb + 4 * 1024 * 1024, SizeReport.ForCategory(Findings, CleanupCategory.Browsers));
        Check.Equal("the bin is its own category", 5L * Gb, SizeReport.ForCategory(Findings, CleanupCategory.Bin));

        Check.Section("SizeReport - the headline does not lie");

        var nothingTicked = SizeReport.Headline(Findings, []);
        Check.That("says nothing will be removed when nothing is ticked",
            nothingTicked.Contains("nothing will be removed", StringComparison.OrdinalIgnoreCase));
        Check.That("still says what was found", nothingTicked.Contains("found", StringComparison.Ordinal));

        var empty = SizeReport.Headline([], ["temp-user"]);
        Check.That("calls an empty result a real answer",
            empty.Contains("real answer", StringComparison.OrdinalIgnoreCase));

        var some = SizeReport.Headline(Findings, ["temp-user", "browser-cache"]);
        Check.That("reports found and selected separately",
            some.Contains("found", StringComparison.Ordinal) && some.Contains("selected", StringComparison.Ordinal));
        Check.That("counts the items in the headline", some.Contains("2 items", StringComparison.Ordinal));
        Check.That("uses the singular for one item",
            SizeReport.Headline(Findings, ["temp-user"]).Contains("1 item", StringComparison.Ordinal));
    }
}
