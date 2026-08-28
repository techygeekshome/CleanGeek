using System.Runtime.InteropServices;

namespace CleanGeek.Services;

/// <summary>
/// The Recycle Bin, through the shell rather than the file system. CleanGeek never walks
/// $Recycle.Bin itself - PathSafety refuses it on every drive - because Windows owns that folder
/// and has an API for both questions.
/// </summary>
public static class RecycleBin
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint NoConfirmation = 0x00000001;
    private const uint NoProgressUi = 0x00000002;
    private const uint NoSound = 0x00000004;

    /// <summary>How much is in the bin, across every drive. Zero when it cannot be asked.</summary>
    public static (long Bytes, int Items) Measure()
    {
        try
        {
            var info = new SHQUERYRBINFO();
            info.cbSize = Marshal.SizeOf(info);

            // A null root means every drive on the machine.
            return SHQueryRecycleBin(null, ref info) == 0
                ? (info.i64Size, (int)Math.Min(info.i64NumItems, int.MaxValue))
                : (0, 0);
        }
        catch (DllNotFoundException)
        {
            return (0, 0);
        }
        catch (EntryPointNotFoundException)
        {
            return (0, 0);
        }
    }

    /// <summary>
    /// Empties it. CleanGeek suppresses the shell's own confirmation because it has already asked
    /// its own question - DeleteGate will not get this far unless the Recycle Bin was ticked on
    /// its own, deliberately, rather than swept in by a bulk action.
    /// </summary>
    public static bool Empty()
    {
        try
        {
            return SHEmptyRecycleBin(IntPtr.Zero, null, NoConfirmation | NoProgressUi | NoSound) == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
