using CleanGeek.Core.Models;
using CleanGeek.Core.Services;

namespace CleanGeek.Tests;

public static class DeleteGateTests
{
    private static readonly CleanupTarget Temp = Catalogue.ById("temp-user")!;
    private static readonly CleanupTarget WindowsTemp = Catalogue.ById("temp-windows")!;
    private static readonly CleanupTarget Bin = Catalogue.ById(Catalogue.RecycleBinId)!;

    private static DeleteContext Ok => new(
        Selected: true, Elevated: true, Unattended: false,
        PartOfCleanEverything: false, PathAllowed: true, FileInUse: false);

    public static void Run()
    {
        Check.Section("DeleteGate - the happy path");

        Check.That("deletes a ticked target with an allowed path", DeleteGate.CanDelete(Temp, Ok));
        Check.That("says nothing when it allows", DeleteGate.Refuse(Temp, Ok) is null);

        Check.Section("DeleteGate - a scheduled run never deletes");

        var scheduled = Ok with { Unattended = true };
        Check.That("refuses a scheduled deletion", !DeleteGate.CanDelete(Temp, scheduled));
        Check.That("refuses it even when everything else is in order",
            !DeleteGate.CanDelete(Temp, new DeleteContext(true, true, true, false, true, false)));
        Check.That("says so in plain words",
            DeleteGate.Refuse(Temp, scheduled)!.Contains("never deletes", StringComparison.Ordinal));

        Check.Section("DeleteGate - nothing happens without a tick");

        Check.That("refuses an unticked target", !DeleteGate.CanDelete(Temp, Ok with { Selected = false }));
        Check.That("names the target when it refuses",
            DeleteGate.Refuse(Temp, Ok with { Selected = false })!.Contains(Temp.Title, StringComparison.Ordinal));

        Check.Section("DeleteGate - the Recycle Bin");

        Check.That("empties the bin when it is chosen on its own", DeleteGate.CanDelete(Bin, Ok));
        Check.That("refuses the bin as part of a bulk clean",
            !DeleteGate.CanDelete(Bin, Ok with { PartOfCleanEverything = true }));
        Check.That("still cleans other targets in a bulk clean",
            DeleteGate.CanDelete(Temp, Ok with { PartOfCleanEverything = true }));

        Check.Section("DeleteGate - administrator rights");

        Check.That("refuses a machine-wide target without elevation",
            !DeleteGate.CanDelete(WindowsTemp, Ok with { Elevated = false }));
        Check.That("allows a profile target without elevation",
            DeleteGate.CanDelete(Temp, Ok with { Elevated = false }));

        Check.Section("DeleteGate - the file system's own answers");

        Check.That("refuses a path the safety check rejected",
            !DeleteGate.CanDelete(Temp, Ok with { PathAllowed = false }));
        Check.That("leaves a file that is in use alone",
            !DeleteGate.CanDelete(Temp, Ok with { FileInUse = true }));

        Check.Section("DeleteGate - refusal order");

        // All the conditions fail at once; the order decides which reason is reported.
        var everythingWrong = new DeleteContext(false, false, true, true, false, true);
        Check.That("reports the scheduled run first",
            DeleteGate.Refuse(Temp, everythingWrong)!.Contains("scheduled", StringComparison.OrdinalIgnoreCase));

        var notTickedAndNotElevated = new DeleteContext(false, false, false, false, true, false);
        Check.That("reports the missing tick before the missing rights",
            DeleteGate.Refuse(WindowsTemp, notTickedAndNotElevated)!
                .Contains("not selected", StringComparison.Ordinal));
    }
}
