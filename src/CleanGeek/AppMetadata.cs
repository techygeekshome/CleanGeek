using TechyGeeksHome.Common;

namespace CleanGeek;

/// <summary>
/// Everything the shared About window and update check need to know about this app. One place
/// to edit when the product page moves or a dependency changes.
///
/// Note the alias: this app already has its own <c>CleanGeek.Services.AppInfo</c> for the strings
/// the main window binds to, so the shared type is named in full rather than imported.
/// </summary>
public static class AppMetadata
{
    public static readonly TechyGeeksHome.Common.AppInfo Info = new()
    {
        Name = "CleanGeek",
        Tagline = "Free disk cleaner for Windows",
        Description =
            "Clears the caches, temporary files, crash dumps and update leftovers Windows hangs on to, shows what is installed and what starts with Windows, and reports the number before anything is removed.",
        GitHubOwner = "techygeekshome",
        GitHubRepo = "CleanGeek",
        ProductUrl = "https://techygeekshome.info/cleangeek/",
        WebsiteUrl = "https://techygeekshome.info",
        DonateUrl = "https://ko-fi.com/techygeekshome",
        IconUri = "avares://CleanGeek/Assets/cleangeek.png",
        LicenceLine =
            "GPL-3.0. Free to use, including at work. No paid tier, no subscription, no upsell.",
        Credits = new[]
        {
            new Credit("Avalonia", "MIT", "https://avaloniaui.net/")
        }
    };
}
