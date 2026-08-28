using CleanGeek.Core.Services;
using CleanGeek.Services;

namespace CleanGeek;

/// <summary>The scheduled scan: no window, nothing removed, one line written to the log.</summary>
internal static class HeadlessScan
{
    public static int Run()
    {
        try
        {
            var settings = new SettingsService();
            settings.Load();

            var report = new CleanupScanService().Scan();
            var selected = Catalogue.Resolve(settings.Current.Selected);

            var found = SizeReport.Found(report.Findings);
            var wouldGo = SizeReport.Selected(report.Findings, selected);

            Log.Write($"Scheduled scan: {ByteSize.Format(found)} found, " +
                      $"{ByteSize.Format(wouldGo)} of it currently ticked. Nothing was deleted - " +
                      $"a scheduled run never cleans.");

            return 0;
        }
        catch (Exception ex)
        {
            // Task Scheduler reports a failure with no detail, so record the reason.
            Log.Write("Scheduled scan failed: " + ex);
            return 1;
        }
    }
}
