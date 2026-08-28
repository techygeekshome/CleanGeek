using CleanGeek.Core.Services;
using CleanGeek.Services;

namespace CleanGeek;

/// <summary>
/// The scheduled scan. No window, no interaction, and nothing removed. It measures, writes a line
/// to the log, and exits.
/// </summary>
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
            // A scheduled task that throws leaves a red entry in Task Scheduler and no
            // explanation. Write the reason down where a person can find it.
            Log.Write("Scheduled scan failed: " + ex);
            return 1;
        }
    }
}
