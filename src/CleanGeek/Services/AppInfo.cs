using System.Reflection;

namespace CleanGeek.Services;

/// <summary>Names, links and the one thing this application promises about the network.</summary>
public static class AppInfo
{
    public const string Name = "CleanGeek";
    public const string By = "TechyGeeksHome";
    public const string ProductUrl = "https://techygeekshome.info/cleangeek/";
    public const string RepoUrl = "https://github.com/techygeekshome/CleanGeek";
    public const string DonateUrl = "https://ko-fi.com/techygeekshome";
    public const string LicenceName = "GNU General Public License v3.0";

    public static string Version =>
        Assembly.GetExecutingAssembly().GetName().Version is { } v
            ? $"{v.Major}.{v.Minor}.{v.Build}"
            : "1.0.0";

    /// <summary>
    /// CleanGeek reads and deletes files on this PC and makes no network calls at all. There is
    /// no telemetry, no analytics, no account, and no update check that phones home unasked.
    /// </summary>
    public const string NetworkPromise = "Nothing leaves this PC.";
}
