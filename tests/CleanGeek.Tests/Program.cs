using CleanGeek.Tests;

// CleanGeek's test harness. Run it with:
//     dotnet run --project tests/CleanGeek.Tests -c Release
// Exit code 0 means everything passed; 1 means something did not, and CI fails the build.
//
// Everything under test here is in CleanGeek.Core, which targets plain net8.0 and touches no
// Windows API. For an application whose whole job is deleting files, that split earns its keep:
// the rules about what may be deleted, what is ticked by default, and what the numbers on screen
// are allowed to claim are all proven on every push, on a runner that has no files to lose.

CatalogueTests.Run();
PathSafetyTests.Run();
DeleteGateTests.Run();
SizeReportTests.Run();
UninstallGateTests.Run();
StartupPolicyTests.Run();
ScheduleTests.Run();
ByteSizeTests.Run();

return Check.Report();
