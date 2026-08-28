using Avalonia;
using CleanGeek.Services;

namespace CleanGeek;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // Anything that gets this far would otherwise close the window with no explanation.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Write("Unhandled: " + e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Write("Unobserved: " + e.Exception);
            e.SetObserved();
        };

        // --scan is what the scheduled task runs. There is no --clean switch, and DeleteGate
        // refuses every delete on an unattended run.
        if (args.Contains("--scan", StringComparer.OrdinalIgnoreCase))
            return HeadlessScan.Run();

        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Write("CleanGeek stopped: " + ex);
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
