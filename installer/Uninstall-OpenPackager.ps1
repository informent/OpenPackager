param([string]$InstallRoot = "$env:LOCALAPPDATA\OpenPackager")
$ErrorActionPreference = 'Stop'
$shortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'OpenPackager.lnk'
if (Test-Path $shortcut) { Remove-Item $shortcut -Force }
if (Test-Path $InstallRoot) { Remove-Item $InstallRoot -Recurse -Force }
Write-Host 'OpenPackager removed.'
