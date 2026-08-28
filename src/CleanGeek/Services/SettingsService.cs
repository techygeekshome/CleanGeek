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
            // A corrupt settings file is not a reason to refuse to start.
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
            // Written beside the real file and then moved over it. A power cut during a direct
            // write would leave a truncated settings.json, which Load recovers from by falling
            // back to the defaults - silently re-ticking things somebody had deliberately turned
            // off. A move is atomic; a write is not.
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
