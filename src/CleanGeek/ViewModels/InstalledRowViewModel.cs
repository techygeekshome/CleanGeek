using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.ViewModels;

public sealed class InstalledRowViewModel(InstalledApp app) : ObservableObject
{
    public InstalledApp App { get; } = app;

    public string Name => App.Name;
    public string Publisher => App.Publisher.Length > 0 ? App.Publisher : "Publisher not recorded";
    public string Version => App.Version.Length > 0 ? App.Version : "";

    /// <summary>The installer's claimed size, which Windows often does not record at all.</summary>
    public string Size => App.SizeUnknown ? "size not recorded" : ByteSize.Format(App.EstimatedBytes);

    public string Installed => App.InstalledOn is { } d ? d.ToString("d MMM yyyy") : "";

    public bool CanUninstall => App.CanUninstall;

    public string Note => App.IsSystemComponent
        ? "Part of Windows or a shared runtime."
        : App.UninstallCommand.Length == 0
            ? "No uninstaller registered. Remove it from Settings, Apps."
            : "";
}
