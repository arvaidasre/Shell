$ErrorActionPreference = 'Stop' # stop on all errors
# Community release assets. Update the version below with each release
# and refresh the checksum (choco install --download-checksum behavior).
$url        = 'https://github.com/arvaidasre/Shell/releases/download/v1.9.19/Shell-1.9.19-setup-x64.msi'
$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  fileType      = 'msi'
  url           = $url
  softwareName  = 'Nilesoft Shell'
  checksum      = '9EB31DA31E70A7DE930528359134BDDC25463ECF2F85638950B0052FB93661C9'
  checksumType  = 'sha256'
  silentArgs   = '/qn /norestart'
  validExitCodes= @(0, 1641, 3010)
}
Install-ChocolateyPackage @packageArgs