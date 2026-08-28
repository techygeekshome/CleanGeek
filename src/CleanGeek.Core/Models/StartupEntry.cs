namespace CleanGeek.Core.Models;

public enum StartupLocation
{
    /// <summary>HKCU or HKLM ...\CurrentVersion\Run.</summary>
    RunKey,

    /// <summary>The Startup folder, for this user or all users.</summary>
    StartupFolder,

    /// <summary>A logon-triggered scheduled task.</summary>
    ScheduledTask
}

public enum StartupScope
{
    ThisUser,
    AllUsers
}

/// <summary>One thing that starts itself when Windows starts.</summary>
public sealed record StartupEntry(
    string Name,
    string Command,
    StartupLocation Location,
    StartupScope Scope,
    string Publisher = "",
    bool Enabled = true);
