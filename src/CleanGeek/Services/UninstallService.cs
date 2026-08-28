using System.Diagnostics;
using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Services;

/// <summary>Runs the publisher's registered uninstaller. No files are removed by this application.</summary>
public sealed class UninstallService
{
    /// <summary>Starts the uninstaller, or returns the reason it did not.</summary>
    public string? Start(InstalledApp app, bool othersAlsoChosen)
    {
        var ctx = new UninstallContext(
            Chosen: true,
            Elevated: Elevation.IsElevated,
            Unattended: false,
            OthersAlsoChosen: othersAlsoChosen,
            PackagedAppsEnabled: true);

        if (UninstallGate.Refuse(app, ctx) is { } refusal)
        {
            Log.Write($"Uninstall refused for {app.Name}: {refusal}");
            return refusal;
        }

        var (file, arguments) = Split(app.UninstallCommand);
        if (file.Length == 0)
            return $"{app.Name} did not register an uninstaller that can be run.";

        try
        {
            // UseShellExecute so an uninstaller needing elevation raises its own prompt.
            Process.Start(new ProcessStartInfo(file, arguments) { UseShellExecute = true });
            Log.Write($"Uninstall started for {app.Name} ({app.Version}).");
            return null;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log.Write($"Uninstall could not be started for {app.Name}: {ex.Message}");
            return $"Windows would not start the uninstaller: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            Log.Write($"Uninstall could not be started for {app.Name}: {ex.Message}");
            return "The uninstaller could not be started.";
        }
    }

    /// <summary>
    /// Splits a registered uninstall string into a file and its arguments. The format is
    /// inconsistent: the path may or may not be quoted, and may be an MsiExec line.
    /// </summary>
    internal static (string File, string Arguments) Split(string command)
    {
        var text = command.Trim();
        if (text.Length == 0) return ("", "");

        if (text[0] == '"')
        {
            var end = text.IndexOf('"', 1);
            if (end < 0) return (text.Trim('"'), "");
            return (text[1..end], text[(end + 1)..].Trim());
        }

        // Unquoted: cut at the first ".exe", since splitting on the first space breaks paths
        // containing spaces.
        var exe = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exe > 0)
        {
            var cut = exe + 4;
            return (text[..cut], text[cut..].Trim());
        }

        var space = text.IndexOf(' ');
        return space < 0 ? (text, "") : (text[..space], text[(space + 1)..].Trim());
    }
}
