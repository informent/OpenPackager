param([string]$Source = (Split-Path -Parent $PSScriptRoot), [string]$InstallRoot = "$env:LOCALAPPDATA\OpenPackager")
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
Get-ChildItem -LiteralPath $Source -File | Where-Object { $_.Extension -in '.exe','.dll' } | Copy-Item -Destination $InstallRoot -Force
$shortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'OpenPackager.lnk'
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcut); $link.TargetPath = Join-Path $InstallRoot 'OpenPackager.exe'; $link.WorkingDirectory = $InstallRoot; $link.Save()
Write-Host "Installed OpenPackager to $InstallRoot"
