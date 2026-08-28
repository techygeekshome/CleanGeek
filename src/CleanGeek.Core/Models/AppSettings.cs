using System.Text.Json.Serialization;

namespace CleanGeek.Core.Models;

public sealed class AppSettings
{
    /// <summary>Target ids the person has ticked. Absent means "use the defaults".</summary>
    [JsonPropertyName("selected")] public List<string>? Selected { get; set; }

    [JsonPropertyName("scanSchedule")] public string ScanSchedule { get; set; } = "Manually only";
    [JsonPropertyName("confirmBeforeCleaning")] public bool ConfirmBeforeCleaning { get; set; } = true;
    [JsonPropertyName("includePackagedApps")] public bool IncludePackagedApps { get; set; } = true;
    [JsonPropertyName("hideSystemComponents")] public bool HideSystemComponents { get; set; } = true;

    /// <summary>
    /// Three things are deliberately not settings, because a setting is a thing that can be
    /// turned off: saved passwords are not a cleanup target at all, the Recycle Bin is emptied
    /// only when explicitly ticked, and a scheduled run may scan but may never clean. They live
    /// in the catalogue, in DeleteGate and in CleanSchedule respectively, with no switch on them.
    /// </summary>
    [JsonIgnore] public static string SafetyNote =>
        "Saved passwords are never a target, the Recycle Bin is never emptied for you, " +
        "and a scheduled run scans but never cleans.";
}
