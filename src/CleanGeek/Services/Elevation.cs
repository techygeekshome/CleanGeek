using System.Security.Principal;

namespace CleanGeek.Services;

public static class Elevation
{
    /// <summary>Whether this process is running as administrator.</summary>
    public static bool IsElevated
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException)
            {
                return false;
            }
        }
    }
}
