namespace CleanGeek.Services;

public static class AppPaths
{
    public static string DataFolder
    {
        get
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(root, "CleanGeek");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsFile => Path.Combine(DataFolder, "settings.json");
    public static string LogFile => Path.Combine(DataFolder, "cleangeek.log");
}
