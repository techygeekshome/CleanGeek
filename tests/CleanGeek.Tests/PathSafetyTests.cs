using CleanGeek.Core.Services;

namespace CleanGeek.Tests;

public static class PathSafetyTests
{
    private static readonly string[] TempRoot = [@"C:\Users\Sam\AppData\Local\Temp"];

    public static void Run()
    {
        Check.Section("PathSafety - what may be deleted");

        Check.That("allows a file inside the allowed root",
            PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temp\setup.log", TempRoot));
        Check.That("allows a file nested deeper inside it",
            PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temp\abc\def\x.tmp", TempRoot));
        Check.That("ignores the case of the path",
            PathSafety.IsSafeToDelete(@"c:\users\sam\appdata\local\temp\X.TMP", TempRoot));
        Check.That("copes with forward slashes",
            PathSafety.IsSafeToDelete("C:/Users/Sam/AppData/Local/Temp/x.tmp", TempRoot));
        Check.That("copes with a trailing slash on the root",
            PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temp\x.tmp",
                [@"C:\Users\Sam\AppData\Local\Temp\"]));

        Check.Section("PathSafety - what it refuses");

        Check.That("refuses the allowed root itself",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temp", TempRoot));
        Check.That("refuses a path outside the root",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\Documents\tax.pdf", TempRoot));
        Check.That("refuses a sibling folder with the same prefix",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temporary\x.tmp", TempRoot));
        Check.That("refuses a path that walks upwards",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temp\..\..\Documents\tax.pdf", TempRoot));
        Check.That("refuses a relative path",
            !PathSafety.IsSafeToDelete(@"Temp\x.tmp", TempRoot));
        Check.That("refuses an empty path", !PathSafety.IsSafeToDelete("", TempRoot));
        Check.That("refuses a null path", !PathSafety.IsSafeToDelete(null, TempRoot));
        Check.That("refuses whitespace", !PathSafety.IsSafeToDelete("   ", TempRoot));
        Check.That("refuses a drive root", !PathSafety.IsSafeToDelete(@"C:\", [@"C:\"]));
        Check.That("refuses everything when no root is allowed",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temp\x.tmp", []));

        Check.Section("PathSafety - the folders it will never touch, whatever it is told");

        string[] anything = [@"C:\"];
        Check.That("refuses the Windows folder", !PathSafety.IsSafeToDelete(@"C:\Windows", anything));
        Check.That("refuses System32", !PathSafety.IsSafeToDelete(@"C:\Windows\System32", anything));
        Check.That("refuses SysWOW64", !PathSafety.IsSafeToDelete(@"C:\Windows\SysWOW64", anything));
        Check.That("refuses Prefetch", !PathSafety.IsSafeToDelete(@"C:\Windows\Prefetch", anything));
        Check.That("refuses Program Files", !PathSafety.IsSafeToDelete(@"C:\Program Files", anything));
        Check.That("refuses Program Files (x86)", !PathSafety.IsSafeToDelete(@"C:\Program Files (x86)", anything));
        Check.That("refuses ProgramData", !PathSafety.IsSafeToDelete(@"C:\ProgramData", anything));
        Check.That("refuses the Users folder", !PathSafety.IsSafeToDelete(@"C:\Users", anything));
        Check.That("refuses Documents", !PathSafety.IsSafeToDelete(@"C:\Users\Sam\Documents", anything));
        Check.That("refuses Desktop", !PathSafety.IsSafeToDelete(@"C:\Users\Sam\Desktop", anything));
        Check.That("refuses Downloads", !PathSafety.IsSafeToDelete(@"C:\Users\Sam\Downloads", anything));
        Check.That("refuses Pictures", !PathSafety.IsSafeToDelete(@"C:\Users\Sam\Pictures", anything));
        Check.That("refuses OneDrive", !PathSafety.IsSafeToDelete(@"C:\Users\Sam\OneDrive", anything));
        Check.That("refuses System Volume Information",
            !PathSafety.IsSafeToDelete(@"D:\System Volume Information", [@"D:\"]));
        Check.That("refuses the recycle bin's own folder",
            !PathSafety.IsSafeToDelete(@"C:\$Recycle.Bin", anything));
        Check.That("refuses them on any drive",
            !PathSafety.IsSafeToDelete(@"E:\Windows\System32", [@"E:\"]));


        Check.Section("PathSafety - system folders are refused by position, not by name");

        Check.That("allows the Windows folder inside a previous installation",
            PathSafety.IsSafeToDelete(@"C:\Windows.old\Windows\System32\shell32.dll",
                                      [@"C:\Windows.old"]));
        Check.That("allows a Documents folder inside a previous installation",
            PathSafety.IsSafeToDelete(@"C:\Windows.old\Users\Sam\Documents\old.txt",
                                      [@"C:\Windows.old"]));
        Check.That("still refuses the live profile folder",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam", anything));
        Check.That("allows a cache inside the live profile",
            PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temp\x.tmp", TempRoot));
        Check.That("refuses WinSxS", !PathSafety.IsSafeToDelete(@"C:\Windows\WinSxS", anything));
        Check.That("refuses the servicing folder",
            !PathSafety.IsSafeToDelete(@"C:\Windows\Servicing", anything));
        Check.That("refuses a recycle bin on any drive",
            !PathSafety.IsSafeToDelete(@"D:\$Recycle.Bin\S-1-5-21\file.txt", [@"D:\"]));
        Check.That("refuses Recovery", !PathSafety.IsSafeToDelete(@"C:\Recovery", anything));

        Check.Section("PathSafety - the refusal is written for a person");

        var why = PathSafety.Refuse(@"C:\Users\Sam\Documents\tax.pdf", TempRoot);
        Check.That("says why it refused", why is { Length: > 20 });
        Check.That("says nothing when it allows",
            PathSafety.Refuse(@"C:\Users\Sam\AppData\Local\Temp\x.tmp", TempRoot) is null);
    }
}
