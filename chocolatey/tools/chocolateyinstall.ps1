$ErrorActionPreference = 'Stop'

# CleanGeek ships an Inno Setup installer. The package downloads it from the GitHub release for the
# matching tag and verifies it against a SHA-256 checksum rather than embedding the binary. Because
# nothing is embedded, this package must NOT contain a tools\VERIFICATION.txt - that file is only
# for packages that ship a binary inside the nupkg, and including one is what the USP 8.0.0
# submission was rejected for.
$packageArgs = @{
  packageName    = 'cleangeek'
  fileType       = 'exe'
  url            = 'https://github.com/techygeekshome/CleanGeek/releases/download/v1.0.5/CleanGeekSetup.exe'
  checksum       = '561e1e5bb0ce1a6dec8f84bf13eda3854909fbaccfbf300df9be6929f4305a3b'
  checksumType   = 'sha256'
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
  validExitCodes = @(0, 3010, 1641)
}

Install-ChocolateyPackage @packageArgs
