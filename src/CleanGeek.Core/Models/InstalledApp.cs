namespace CleanGeek.Core.Models;

public enum AppSource
{
    /// <summary>A classic installer, found under the Uninstall keys.</summary>
    Installed,

    /// <summary>A packaged (Store / MSIX) application.</summary>
    Packaged
}

/// <summary>One entry on the Installed screen.</summary>
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

    /// <summary>True when Windows reported no size. Common, and not an error.</summary>
    public bool SizeUnknown => EstimatedBytes <= 0;
}
