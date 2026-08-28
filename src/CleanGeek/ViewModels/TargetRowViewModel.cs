using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.ViewModels;

/// <summary>One row on the Clean screen: a target, what it found, and whether it is ticked.</summary>
public sealed class TargetRowViewModel(CleanupTarget target, ScanFinding finding, bool ticked, bool elevated)
    : ObservableObject
{
    private bool _ticked = ticked;

    public CleanupTarget Target { get; } = target;
    public ScanFinding Finding { get; private set; } = finding;

    public string Id => Target.Id;
    public string Title => Target.Title;
    public string What => Target.What;
    public string Cost => Target.Cost;
    public bool HasCost => Target.HasCost;

    public bool Ticked
    {
        get => _ticked;
        set
        {
            if (Set(ref _ticked, value)) TickChanged?.Invoke();
        }
    }

    public event Action? TickChanged;

    public string Size => Finding.Bytes > 0 ? ByteSize.Format(Finding.Bytes) : "";

    public string Detail => Finding.Bytes > 0
        ? $"{Finding.Files:n0} {(Finding.Files == 1 ? "item" : "items")}"
        : "Nothing found";

    /// <summary>The short tag on the right of the row. Says what it costs, never how urgent it is.</summary>
    public string RiskTag => Target.Risk switch
    {
        CleanupRisk.Rebuilds => "REBUILDS ITSELF",
        CleanupRisk.Disposable => "SAFE TO REMOVE",
        CleanupRisk.Costly => "COSTS YOU SOMETHING",
        CleanupRisk.Irreversible => "CANNOT BE UNDONE",
        _ => ""
    };

    public bool NeedsAdmin => Target.NeedsAdmin;

    public bool Blocked => Target.NeedsAdmin && !elevated;

    public string BlockedNote => Blocked
        ? "Needs administrator rights. Restart CleanGeek as administrator to include this."
        : "";

    public void Update(ScanFinding updated)
    {
        Finding = updated;
        Raise(nameof(Size));
        Raise(nameof(Detail));
    }
}
