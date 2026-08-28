using System.Runtime.InteropServices;

namespace CleanGeek.Services;

/// <summary>Recycle Bin size and emptying, via the shell API. $Recycle.Bin is never walked directly.</summary>
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

    /// <summary>How much is in the bin across every drive. Zero when the call fails.</summary>
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
    /// Empties the bin. The shell's own confirmation is suppressed because DeleteGate has already
    /// required an explicit, non-bulk selection.
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
