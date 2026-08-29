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
    private bool _confirming;
    private string _statusLine = "Not scanned yet. Nothing is measured, and nothing is removed, until you press Scan.";
    private string _headline = "";
    private string _lastCleanLine = "";

    public ShellViewModel()
    {
        _settings.Load();

        ScanCommand = new RelayCommand(() => _ = ScanAsync(), () => !Busy);
        CleanCommand = new RelayCommand(StartClean, () => !Busy && Scanned && SelectedBytes > 0);
        ConfirmCleanCommand = new RelayCommand(() => _ = CleanAsync());
        CancelCleanCommand = new RelayCommand(() => Confirming = false);

        ShowClean = new RelayCommand(() => Page = "Clean");
        ShowInstalled = new RelayCommand(() => { Page = "Installed"; LoadInstalled(); });
        ShowStartup = new RelayCommand(() => { Page = "Startup"; LoadStartup(); });
        ShowSettings = new RelayCommand(() => Page = "Settings");

        Settings = new SettingsViewModel(_settings);
        Settings.ListsAffected += () => { Installed.Clear(); Startup.Clear(); };

        BuildTargetRows();
    }

    public ObservableCollection<TargetRowViewModel> Targets { get; } = [];
    public ObservableCollection<InstalledRowViewModel> Installed { get; } = [];
    public ObservableCollection<StartupRowViewModel> Startup { get; } = [];
    public SettingsViewModel Settings { get; }

    public RelayCommand ScanCommand { get; }
    public RelayCommand CleanCommand { get; }
    public RelayCommand ConfirmCleanCommand { get; }
    public RelayCommand CancelCleanCommand { get; }
    public RelayCommand ShowClean { get; }
    public RelayCommand ShowInstalled { get; }
    public RelayCommand ShowStartup { get; }
    public RelayCommand ShowSettings { get; }

    public string BrandName => AppInfo.Name;
    public string BrandBy => "by " + AppInfo.By;
    public string VersionText => AppInfo.Version + " · portable";
    public string SafetyNote => AppSettings.SafetyNote;

    public bool IsElevated => Elevation.IsElevated;

    public string ElevationNote => IsElevated
        ? "Running as administrator."
        : "Running as you, not as administrator. The machine-wide items are shown and measured, but they cannot be cleaned and are not counted in what is going.";

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

    /// <summary>True while the confirmation strip is showing.</summary>
    public bool Confirming { get => _confirming; private set => Set(ref _confirming, value); }

    public string StatusLine { get => _statusLine; private set => Set(ref _statusLine, value); }

    /// <summary>
    /// Lets the window put something in the status line that did not come from a scan - the
    /// update check is the only caller. The setter stays private so nothing else can.
    /// </summary>
    public void SetStatus(string message) => StatusLine = message;
    public string Headline { get => _headline; private set => Set(ref _headline, value); }
    public string LastCleanLine { get => _lastCleanLine; private set => Set(ref _lastCleanLine, value); }
    public bool HasCleaned => LastCleanLine.Length > 0;

    public string ConfirmLine =>
        $"About to remove {ByteSize.Format(SelectedBytes)} across " +
        $"{string.Join(", ", Actionable().Select(t => t.Title))}. This cannot be undone.";

    public long SelectedBytes =>
        SizeReport.Selected(Targets.Select(t => t.Finding).ToList(), ActionableIds());

    /// <summary>Builds the row list once; a scan updates the rows in place so ticks are kept.</summary>
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
        // Save every tick, including machine-wide rows this run cannot touch, so the choice
        // survives a restart as administrator. Only actionable rows are cleaned.
        _settings.Current.Selected = Targets.Where(t => t.Ticked).Select(t => t.Id).ToList();
        _settings.Save();

        Confirming = false;
        Raise(nameof(SelectedBytes));
        Headline = SizeReport.Headline(Targets.Select(t => t.Finding).ToList(), ActionableIds());
        CleanCommand.RaiseCanExecuteChanged();
    }

    private IEnumerable<TargetRowViewModel> Actionable() => Targets.Where(t => t.Ticked && !t.Blocked);

    private List<string> ActionableIds() => Actionable().Select(t => t.Id).ToList();

    private async Task ScanAsync()
    {
        Busy = true;
        Confirming = false;
        StatusLine = "Measuring. Nothing is being deleted.";

        try
        {
            // Off the UI thread: a full scan can take minutes.
            var report = await Task.Run(() => _scanner.Scan());

            foreach (var row in Targets)
                row.Update(report.For(row.Id));

            Scanned = true;
            Headline = SizeReport.Headline(report.Findings, ActionableIds());
            StatusLine = report.Unreadable.Count == 0
                ? "Scan finished. Nothing has been removed - press Clean when you are ready."
                : $"Scan finished. {report.Unreadable.Count} folders could not be read and were left out of the total.";
        }
        catch (Exception ex)
        {
            Log.Write("Scan failed: " + ex);
            StatusLine = "The scan could not finish. Nothing was removed. See cleangeek.log for the reason.";
        }
        finally
        {
            Busy = false;
            Raise(nameof(SelectedBytes));
            CleanCommand.RaiseCanExecuteChanged();
        }
    }

    private void StartClean()
    {
        if (_settings.Current.ConfirmBeforeCleaning)
        {
            Confirming = true;
            Raise(nameof(ConfirmLine));
            return;
        }

        _ = CleanAsync();
    }

    private async Task CleanAsync()
    {
        Confirming = false;
        Busy = true;
        StatusLine = "Cleaning the items you ticked.";

        try
        {
            var ticked = ActionableIds();

            // Bulk keeps the Recycle Bin out of a sweep; it may only be emptied on its own.
            var bulk = ticked.Count > 1;
            var report = await Task.Run(() => _cleaner.Clean(ticked, bulk));

            var line = report.BytesRemoved > 0
                ? $"Removed {ByteSize.Format(report.BytesRemoved)} across {report.FilesRemoved:n0} files. " +
                  $"{report.InUse:n0} were in use and were left alone."
                : "Nothing was removed. Everything ticked was either already gone or in use.";

            if (report.Blocked > 0)
                line += $" {report.Blocked:n0} were refused by the safety check and are listed in the log.";

            foreach (var refusal in report.Refusals)
                line += $"  {Catalogue.ById(refusal.TargetId)?.Title}: {refusal.Refused}";

            LastCleanLine = line;
        }
        catch (Exception ex)
        {
            Log.Write("Clean failed: " + ex);
            LastCleanLine = "The clean stopped early. What had already been removed is gone; " +
                            "see cleangeek.log for the reason.";
        }
        finally
        {
            Busy = false;
            Raise(nameof(HasCleaned));
        }

        await ScanAsync();
    }

    private void LoadInstalled()
    {
        if (Installed.Count > 0) return;

        try
        {
            foreach (var app in _installed.Read(_settings.Current.HideSystemComponents))
                Installed.Add(new InstalledRowViewModel(app));
        }
        catch (Exception ex)
        {
            Log.Write("Installed list failed: " + ex);
            StatusLine = "The installed list could not be read. See cleangeek.log for the reason.";
        }

        Raise(nameof(InstalledCount));
    }

    private void LoadStartup()
    {
        if (Startup.Count > 0) return;

        try
        {
            foreach (var entry in _startup.Read())
                Startup.Add(new StartupRowViewModel(entry));
        }
        catch (Exception ex)
        {
            Log.Write("Startup list failed: " + ex);
            StatusLine = "The startup list could not be read. See cleangeek.log for the reason.";
        }

        Raise(nameof(StartupCount));
        Raise(nameof(StartupOnCount));
    }

    public int InstalledCount => Installed.Count;
    public int StartupCount => Startup.Count;
    public int StartupOnCount => Startup.Count(s => s.Enabled);
}
