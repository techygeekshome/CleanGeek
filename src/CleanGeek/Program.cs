using Avalonia;

namespace CleanGeek;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // --scan is what the scheduled task runs. It measures, writes a line to the log, and
        // exits without ever showing a window. There is deliberately no --clean: DeleteGate
        // refuses everything when the run is unattended, so a schedule cannot be turned into a
        // deletion even by hand-editing the task.
        if (args.Contains("--scan", StringComparer.OrdinalIgnoreCase))
            return HeadlessScan.Run();

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
