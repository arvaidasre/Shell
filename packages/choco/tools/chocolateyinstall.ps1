$ErrorActionPreference = 'Stop' # stop on all errors
# Community release assets. Update the version below with each release
# and refresh the checksum (choco install --download-checksum behavior).
$url        = 'https://github.com/arvaidasre/Shell/releases/download/v1.9.21/Shell-1.9.21-setup-x64.msi'
$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  fileType      = 'msi'
  url           = $url
  softwareName  = 'Nilesoft Shell'
  checksum      = '5EFF5C4DFA44C0F083E944868F0B5512C42947211BBA1FD0F6B57ED70EC64B23'
  checksumType  = 'sha256'
  silentArgs   = '/qn /norestart'
  validExitCodes= @(0, 1641, 3010)
}
Install-ChocolateyPackage @packageArgs