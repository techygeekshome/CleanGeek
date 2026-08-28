using System.Text.Json.Serialization;

namespace CleanGeek.Core.Models;

public sealed class AppSettings
{
    /// <summary>Ticked target ids. Null means the defaults apply.</summary>
    [JsonPropertyName("selected")] public List<string>? Selected { get; set; }

    [JsonPropertyName("scanSchedule")] public string ScanSchedule { get; set; } = "Manually only";
    [JsonPropertyName("confirmBeforeCleaning")] public bool ConfirmBeforeCleaning { get; set; } = true;
    [JsonPropertyName("includePackagedApps")] public bool IncludePackagedApps { get; set; } = true;
    [JsonPropertyName("hideSystemComponents")] public bool HideSystemComponents { get; set; } = true;

    /// <summary>Text shown in Settings describing the fixed rules that are not configurable.</summary>
    [JsonIgnore] public static string SafetyNote =>
        "Saved passwords are never a target, the Recycle Bin is never emptied for you, " +
        "and a scheduled run scans but never cleans.";
}
