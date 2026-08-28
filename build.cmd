@echo off
setlocal
echo Building CleanGeek...
dotnet build CleanGeek.sln -c Release || exit /b 1
echo.
echo Running checks...
dotnet run --project tests\CleanGeek.Tests -c Release --no-build || exit /b 1
echo.
echo Publishing the portable build...
dotnet publish src\CleanGeek\CleanGeek.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\portable || exit /b 1
echo.
echo Done. publish\portable\CleanGeek.exe
