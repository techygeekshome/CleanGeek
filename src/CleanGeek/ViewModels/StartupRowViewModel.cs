using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.ViewModels;

public sealed class StartupRowViewModel(StartupEntry entry) : ObservableObject
{
    public StartupEntry Entry { get; } = entry;

    public string Name => Entry.Name;
    public string Command => Entry.Command;
    public string Where => StartupPolicy.Describe(Entry);
    public bool Enabled => Entry.Enabled;
    public string State => Entry.Enabled ? "ON" : "OFF";

    public string? KeepReason => StartupPolicy.ReasonToKeep(Entry);
    public bool ShouldKeep => KeepReason is not null;
    public string KeepTag => ShouldKeep ? "LEAVE ON" : "";
}
