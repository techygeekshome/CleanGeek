using CleanGeek.Core.Models;
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


        Check.Section("PathSafety - a system folder protects its CONTENTS, not just its name");

        // A refusal has to cover the subtree, not only the folder name itself.
        Check.That("refuses a file inside System32",
            !PathSafety.IsSafeToDelete(@"C:\Windows\System32\ntoskrnl.exe", anything));
        Check.That("refuses a file deep inside System32",
            !PathSafety.IsSafeToDelete(@"C:\Windows\System32\config\SAM", anything));
        Check.That("refuses a file inside Documents",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\Documents\tax.pdf", anything));
        Check.That("refuses a file inside Desktop",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\Desktop\cv.docx", anything));
        Check.That("refuses a file inside OneDrive",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\OneDrive\Photos\wedding.jpg", anything));
        Check.That("refuses a file inside Program Files",
            !PathSafety.IsSafeToDelete(@"C:\Program Files\Office\winword.exe", anything));
        Check.That("refuses a file inside ProgramData",
            !PathSafety.IsSafeToDelete(@"C:\ProgramData\Vendor\licence.dat", anything));
        Check.That("refuses a file inside Prefetch",
            !PathSafety.IsSafeToDelete(@"C:\Windows\Prefetch\NOTEPAD.pf", anything));
        Check.That("refuses a file inside WinSxS",
            !PathSafety.IsSafeToDelete(@"C:\Windows\WinSxS\amd64_x\foo.dll", anything));
        Check.That("refuses a file inside Recovery",
            !PathSafety.IsSafeToDelete(@"C:\Recovery\WindowsRE\winre.wim", anything));

        Check.Section("PathSafety - but the real targets underneath Windows still work");

        // Windows and Users are refused as folders only, since real targets live beneath them.
        Check.That("allows the Windows Temp folder's contents",
            PathSafety.IsSafeToDelete(@"C:\Windows\Temp\x.tmp", [@"C:\Windows\Temp"]));
        Check.That("allows the update download cache",
            PathSafety.IsSafeToDelete(@"C:\Windows\SoftwareDistribution\Download\a\b.esd",
                                      [@"C:\Windows\SoftwareDistribution\Download"]));
        Check.That("allows the memory dump",
            PathSafety.IsSafeToDelete(@"C:\Windows\MEMORY.DMP", [@"C:\Windows"]));
        Check.That("allows a minidump",
            PathSafety.IsSafeToDelete(@"C:\Windows\Minidump\010101-1.dmp", [@"C:\Windows\Minidump"]));
        Check.That("allows the delivery optimisation cache",
            PathSafety.IsSafeToDelete(
                @"C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\x",
                [@"C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization"]));
        Check.That("allows a per-user cache under a profile",
            PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Google\Chrome\User Data\Default\Cache\f",
                                      [@"C:\Users\Sam\AppData\Local\Google\Chrome\User Data\Default\Cache"]));

        Check.Section("PathSafety - a specification authorises only what it names");

        var dump = new CleanupPath(@"C:\Windows", "MEMORY.DMP", Recursive: false);
        Check.That("allows the file the specification names",
            PathSafety.IsSafeForSpec(@"C:\Windows\MEMORY.DMP", dump));
        Check.That("refuses anything else under the same root",
            !PathSafety.IsSafeForSpec(@"C:\Windows\explorer.exe", dump));
        Check.That("refuses a file in a subfolder of the same root",
            !PathSafety.IsSafeForSpec(@"C:\Windows\Temp\x.tmp", dump));
        Check.That("says which file it wanted",
            PathSafety.RefuseForSpec(@"C:\Windows\explorer.exe", dump)!
                .Contains("MEMORY.DMP", StringComparison.Ordinal));

        var everything = new CleanupPath(@"C:\Users\Sam\AppData\Local\Temp");
        Check.That("a wildcard specification allows anything under its root",
            PathSafety.IsSafeForSpec(@"C:\Users\Sam\AppData\Local\Temp\a\b.tmp", everything));
        Check.That("a wildcard specification still refuses outside its root",
            !PathSafety.IsSafeForSpec(@"C:\Users\Sam\Documents\tax.pdf", everything));

        Check.Section("PathSafety - trailing dots and spaces cannot step around the names");

        Check.That("refuses System32 with a trailing dot",
            !PathSafety.IsSafeToDelete(@"C:\Windows\System32.", anything));
        Check.That("refuses Documents with a trailing space",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\Documents \tax.pdf", anything));
        Check.That("still sees an upwards walk",
            !PathSafety.IsSafeToDelete(@"C:\Users\Sam\AppData\Local\Temp\..\..\x", TempRoot));

        Check.Section("PathSafety - UNC paths are indexed past the share");

        Check.That("refuses the Windows folder on a share",
            !PathSafety.IsSafeToDelete(@"\\srv\C$\Windows", [@"\\srv\C$"]));
        Check.That("refuses System32 on a share",
            !PathSafety.IsSafeToDelete(@"\\srv\C$\Windows\System32\ntoskrnl.exe", [@"\\srv\C$"]));
        Check.That("refuses a profile folder on a share",
            !PathSafety.IsSafeToDelete(@"\\srv\C$\Users\Sam", [@"\\srv\C$"]));
        Check.That("refuses somebody's documents on a share",
            !PathSafety.IsSafeToDelete(@"\\srv\C$\Users\Sam\Documents\tax.pdf", [@"\\srv\C$"]));
        Check.That("allows a redirected cache on a share",
            PathSafety.IsSafeToDelete(@"\\srv\redirect\Sam\AppData\Local\Temp\x.tmp",
                                      [@"\\srv\redirect\Sam\AppData\Local\Temp"]));

        Check.Section("PathSafety - the refusal is written for a person");

        var why = PathSafety.Refuse(@"C:\Users\Sam\Documents\tax.pdf", TempRoot);
        Check.That("says why it refused", why is { Length: > 20 });
        Check.That("says nothing when it allows",
            PathSafety.Refuse(@"C:\Users\Sam\AppData\Local\Temp\x.tmp", TempRoot) is null);
    }
}
