using Avalonia;

namespace CleanGeek;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // --scan is what the scheduled task runs. There is no --clean switch, and DeleteGate
        // refuses every delete on an unattended run.
        if (args.Contains("--scan", StringComparer.OrdinalIgnoreCase))
            return HeadlessScan.Run();

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
