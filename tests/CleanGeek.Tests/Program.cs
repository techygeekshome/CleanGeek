using CleanGeek.Tests;

// Run with: dotnet run --project tests/CleanGeek.Tests -c Release
// Exit code 0 means everything passed, 1 means something failed.
// Everything under test is in CleanGeek.Core, which is plain net8.0 and calls no Windows API.

CatalogueTests.Run();
PathSafetyTests.Run();
DeleteGateTests.Run();
SizeReportTests.Run();
UninstallGateTests.Run();
StartupPolicyTests.Run();
ScheduleTests.Run();
ByteSizeTests.Run();

return Check.Report();
