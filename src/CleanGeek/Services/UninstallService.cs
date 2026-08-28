using System.Diagnostics;
using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Services;

/// <summary>
/// Runs the publisher's own uninstaller. CleanGeek does not write an uninstaller of its own and
/// does not delete an application's files itself: the people who wrote the software know what it
/// installed, and second-guessing them is how a "cleaner" removes the wrong folder.
///
/// What CleanGeek adds is the list, the sorting, and the refusals in UninstallGate.
/// </summary>
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
            // UseShellExecute so that an uninstaller which needs administrator rights raises its
            // own prompt, rather than failing silently because CleanGeek is not elevated.
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
    /// Splits a registered uninstall string into a file and its arguments. These are written by
    /// hundreds of different installers and are inconsistent: some quote the path, some do not,
    /// and some are an MsiExec line with no path at all.
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

        // Unquoted. The executable is everything up to the first ".exe", where there is one -
        // splitting on the first space breaks every path with a space in it, which is most of them.
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
