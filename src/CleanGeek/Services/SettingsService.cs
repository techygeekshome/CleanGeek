using System.Text.Json;
using CleanGeek.Core.Models;

namespace CleanGeek.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        try
        {
            var path = AppPaths.SettingsFile;
            if (!File.Exists(path)) return;
            Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            // A corrupt settings file falls back to defaults rather than failing to start.
            Log.Write($"settings.json could not be read, using defaults: {ex.Message}");
            Current = new AppSettings();
        }
        catch (IOException) { Current = new AppSettings(); }
        catch (UnauthorizedAccessException) { Current = new AppSettings(); }
    }

    public void Save()
    {
        try
        {
            // Write to a temp file and move over the original. A truncated settings.json would
            // load as defaults, silently re-ticking targets the user turned off.
            var path = AppPaths.SettingsFile;
            var temp = path + ".tmp";

            File.WriteAllText(temp, JsonSerializer.Serialize(Current, Options));
            File.Move(temp, path, overwrite: true);
        }
        catch (IOException ex)
        {
            Log.Write($"settings.json could not be written: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Write($"settings.json could not be written: {ex.Message}");
        }
    }
}
