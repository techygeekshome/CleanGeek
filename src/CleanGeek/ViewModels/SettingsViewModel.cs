using CleanGeek.Core.Models;
using CleanGeek.Core.Services;
using CleanGeek.Services;

namespace CleanGeek.ViewModels;

public sealed class SettingsViewModel(SettingsService settings) : ObservableObject
{
    public IReadOnlyList<string> ScheduleOptions => CleanSchedule.Options;

    public string ScanSchedule
    {
        get => settings.Current.ScanSchedule;
        set
        {
            if (settings.Current.ScanSchedule == value) return;
            settings.Current.ScanSchedule = value;
            settings.Save();
            Raise();
            Raise(nameof(SchedulePreview));
        }
    }

    public string SchedulePreview
    {
        get
        {
            var plan = CleanSchedule.Parse(settings.Current.ScanSchedule);
            return plan.NeedsScheduledTask
                ? $"{plan.Describe}, registered with Windows Task Scheduler as \"{CleanSchedule.TaskName}\". " +
                  "It only measures; it can never delete anything."
                : $"{plan.Describe}.";
        }
    }

    public bool ConfirmBeforeCleaning
    {
        get => settings.Current.ConfirmBeforeCleaning;
        set
        {
            if (settings.Current.ConfirmBeforeCleaning == value) return;
            settings.Current.ConfirmBeforeCleaning = value;
            settings.Save();
            Raise();
        }
    }

    public bool HideSystemComponents
    {
        get => settings.Current.HideSystemComponents;
        set
        {
            if (settings.Current.HideSystemComponents == value) return;
            settings.Current.HideSystemComponents = value;
            settings.Save();
            Raise();

            // The Installed list is cached, so the cache has to be dropped for the change to show.
            ListsAffected?.Invoke();
        }
    }

    /// <summary>Raised when a setting invalidates a list the shell has already loaded.</summary>
    public event Action? ListsAffected;

    public string SafetyNote => AppSettings.SafetyNote;

    /// <summary>The never-cleaned list, read from the catalogue so the screen cannot drift from it.</summary>
    public IReadOnlyList<RefusalViewModel> NeverCleaned =>
        Catalogue.NeverCleaned.Select(n => new RefusalViewModel(n.Thing, n.Why)).ToList();
}

public sealed record RefusalViewModel(string Thing, string Why);
