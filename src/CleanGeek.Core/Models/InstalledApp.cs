namespace CleanGeek.Core.Models;

public enum AppSource
{
    /// <summary>A classic installer, found under the Uninstall keys.</summary>
    Installed,

    /// <summary>A packaged (Store / MSIX) application.</summary>
    Packaged
}

/// <summary>
/// One entry on the Installed screen. CleanGeek absorbed what would have been UninstallGeek, so
/// this is that product's model: it lists, it sorts, it hands the uninstall back to the
/// publisher's own uninstaller, and it never invents an uninstall of its own.
/// </summary>
public sealed record InstalledApp(
    string Name,
    string Publisher,
    string Version,
    string UninstallCommand,
    AppSource Source,
    DateTime? InstalledOn = null,
    long EstimatedBytes = 0,
    string InstallLocation = "",
    bool IsSystemComponent = false)
{
    public bool CanUninstall => UninstallCommand.Trim().Length > 0 && !IsSystemComponent;

    /// <summary>True when Windows reported no size at all - which is common, and not an error.</summary>
    public bool SizeUnknown => EstimatedBytes <= 0;
}
