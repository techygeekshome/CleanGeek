using System.Collections.ObjectModel;
using CleanGeek.Core.Models;
using CleanGeek.Core.Services;
using CleanGeek.Services;

namespace CleanGeek.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private readonly SettingsService _settings = new();
    private readonly CleanupScanService _scanner = new();
    private readonly CleanupRunService _cleaner = new();
    private readonly InstalledAppsService _installed = new();
    private readonly StartupService _startup = new();

    private string _page = "Clean";
    private bool _busy;
    private bool _scanned;
    private string _statusLine = "Not scanned yet. Nothing is measured, and nothing is removed, until you press Scan.";
    private string _headline = "";
    private string _lastCleanLine = "";

    public ShellViewModel()
    {
        _settings.Load();

        ScanCommand = new RelayCommand(Scan, () => !Busy);
        CleanCommand = new RelayCommand(Clean, () => !Busy && Scanned && SelectedBytes > 0);
        ShowClean = new RelayCommand(() => Page = "Clean");
        ShowInstalled = new RelayCommand(() => { Page = "Installed"; LoadInstalled(); });
        ShowStartup = new RelayCommand(() => { Page = "Startup"; LoadStartup(); });
        ShowSettings = new RelayCommand(() => Page = "Settings");

        Settings = new SettingsViewModel(_settings);

        BuildTargetRows();
    }

    public ObservableCollection<TargetRowViewModel> Targets { get; } = [];
    public ObservableCollection<InstalledRowViewModel> Installed { get; } = [];
    public ObservableCollection<StartupRowViewModel> Startup { get; } = [];
    public SettingsViewModel Settings { get; }

    public RelayCommand ScanCommand { get; }
    public RelayCommand CleanCommand { get; }
    public RelayCommand ShowClean { get; }
    public RelayCommand ShowInstalled { get; }
    public RelayCommand ShowStartup { get; }
    public RelayCommand ShowSettings { get; }

    public string BrandName => AppInfo.Name;
    public string BrandBy => "by " + AppInfo.By;
    public string VersionText => AppInfo.Version + " · portable";
    public string NetworkPromise => AppInfo.NetworkPromise;
    public string SafetyNote => AppSettings.SafetyNote;

    public bool IsElevated => Elevation.IsElevated;

    public string ElevationNote => IsElevated
        ? "Running as administrator."
        : "Running as you, not as administrator. The machine-wide items are shown but cannot be cleaned.";

    public string Page
    {
        get => _page;
        set
        {
            if (!Set(ref _page, value)) return;
            Raise(nameof(IsClean));
            Raise(nameof(IsInstalled));
            Raise(nameof(IsStartup));
            Raise(nameof(IsSettings));
            Raise(nameof(PageTitle));
        }
    }

    public bool IsClean => Page == "Clean";
    public bool IsInstalled => Page == "Installed";
    public bool IsStartup => Page == "Startup";
    public bool IsSettings => Page == "Settings";

    public string PageTitle => Page;

    public bool Busy
    {
        get => _busy;
        private set
        {
            if (!Set(ref _busy, value)) return;
            ScanCommand.RaiseCanExecuteChanged();
            CleanCommand.RaiseCanExecuteChanged();
        }
    }

    public bool Scanned { get => _scanned; private set => Set(ref _scanned, value); }
    public string StatusLine { get => _statusLine; private set => Set(ref _statusLine, value); }
    public string Headline { get => _headline; private set => Set(ref _headline, value); }
    public string LastCleanLine { get => _lastCleanLine; private set => Set(ref _lastCleanLine, value); }
    public bool HasCleaned => LastCleanLine.Length > 0;

    public long SelectedBytes =>
        SizeReport.Selected(Targets.Select(t => t.Finding).ToList(), TickedIds());

    /// <summary>
    /// The list is built once and reused, so a scan updates the numbers on rows the person has
    /// already ticked rather than throwing their choices away.
    /// </summary>
    private void BuildTargetRows()
    {
        var selected = Catalogue.Resolve(_settings.Current.Selected);
        var elevated = IsElevated;

        Targets.Clear();
        foreach (var target in Catalogue.All)
        {
            var row = new TargetRowViewModel(target, ScanFinding.Empty(target.Id),
                                             selected.Contains(target.Id, StringComparer.Ordinal), elevated);
            row.TickChanged += OnTickChanged;
            Targets.Add(row);
        }
    }

    private void OnTickChanged()
    {
        _settings.Current.Selected = TickedIds().ToList();
        _settings.Save();

        Raise(nameof(SelectedBytes));
        Headline = SizeReport.Headline(Targets.Select(t => t.Finding).ToList(), TickedIds());
        CleanCommand.RaiseCanExecuteChanged();
    }

    private List<string> TickedIds() => Targets.Where(t => t.Ticked).Select(t => t.Id).ToList();

    private void Scan()
    {
        Busy = true;
        StatusLine = "Measuring. Nothing is being deleted.";

        try
        {
            var report = _scanner.Scan();

            foreach (var row in Targets)
                row.Update(report.For(row.Id));

            Scanned = true;
            Headline = SizeReport.Headline(report.Findings, TickedIds());
            StatusLine = report.Unreadable.Count == 0
                ? "Scan finished. Nothing has been removed - press Clean when you are ready."
                : $"Scan finished. {report.Unreadable.Count} folders could not be read and were left out of the total.";
        }
        finally
        {
            Busy = false;
            Raise(nameof(SelectedBytes));
            CleanCommand.RaiseCanExecuteChanged();
        }
    }

    private void Clean()
    {
        Busy = true;
        StatusLine = "Cleaning the items you ticked.";

        try
        {
            // Every target here was ticked individually on the Clean screen, so this is not a
            // clean-everything sweep - which is what keeps the Recycle Bin available rather than
            // refused. There is no button in CleanGeek that ticks everything for you.
            var report = _cleaner.Clean(TickedIds(), bulk: false);

            LastCleanLine = report.BytesRemoved > 0
                ? $"Removed {ByteSize.Format(report.BytesRemoved)} across {report.FilesRemoved:n0} files. " +
                  $"{report.Skipped:n0} were in use or protected and were left alone."
                : "Nothing was removed. Everything ticked was either already gone or in use.";

            foreach (var refusal in report.Refusals)
                LastCleanLine += $"  {Catalogue.ById(refusal.TargetId)?.Title}: {refusal.Refused}";

            Scan();
        }
        finally
        {
            Busy = false;
            Raise(nameof(HasCleaned));
        }
    }

    private void LoadInstalled()
    {
        if (Installed.Count > 0) return;

        Installed.Clear();
        foreach (var app in _installed.Read(_settings.Current.HideSystemComponents))
            Installed.Add(new InstalledRowViewModel(app));

        Raise(nameof(InstalledCount));
    }

    private void LoadStartup()
    {
        if (Startup.Count > 0) return;

        Startup.Clear();
        foreach (var entry in _startup.Read())
            Startup.Add(new StartupRowViewModel(entry));

        Raise(nameof(StartupCount));
        Raise(nameof(StartupOnCount));
    }

    public int InstalledCount => Installed.Count;
    public int StartupCount => Startup.Count;
    public int StartupOnCount => Startup.Count(s => s.Enabled);
}
