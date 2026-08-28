using CleanGeek.Core.Models;

namespace CleanGeek.Core.Services;

/// <summary>The conditions an uninstall is checked against.</summary>
/// <param name="Chosen">This application is selected.</param>
/// <param name="Elevated">Running with administrator rights.</param>
/// <param name="Unattended">A scheduled run.</param>
/// <param name="OthersAlsoChosen">More than one application is selected.</param>
/// <param name="PackagedAppsEnabled">Packaged (Store) applications are enabled in settings.</param>
public readonly record struct UninstallContext(
    bool Chosen,
    bool Elevated,
    bool Unattended,
    bool OthersAlsoChosen,
    bool PackagedAppsEnabled);

/// <summary>Decides which registered uninstaller may be run, and when.</summary>
public static class UninstallGate
{
    public static string? Refuse(InstalledApp app, UninstallContext ctx)
    {
        if (ctx.Unattended)
            return "A scheduled run never uninstalls anything.";

        if (!ctx.Chosen)
            return $"{app.Name} was not selected.";

        // One at a time: uninstallers show their own dialogs and need answers.
        if (ctx.OthersAlsoChosen)
            return "Uninstall one application at a time - their own uninstallers need your answers.";

        if (app.IsSystemComponent)
            return $"{app.Name} is part of Windows or a runtime something else depends on.";

        if (app.UninstallCommand.Trim().Length == 0)
            return $"{app.Name} did not register an uninstaller. Remove it from Settings, Apps instead.";

        if (app.Source == AppSource.Packaged && !ctx.PackagedAppsEnabled)
            return "Packaged applications are switched off in Settings.";

        return null;
    }

    public static bool CanUninstall(InstalledApp app, UninstallContext ctx) => Refuse(app, ctx) is null;
}
