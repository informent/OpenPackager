param([Parameter(Mandatory)][string]$File, [Parameter(Mandatory)][string]$CertificateThumbprint, [string]$TimestampServer = 'http://timestamp.digicert.com')
$ErrorActionPreference = 'Stop'
$signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter signtool.exe | Where-Object FullName -match '\\x64\\' | Select-Object -First 1
if (-not $signtool) { throw 'Windows SDK signtool.exe was not found.' }
if (-not (Test-Path $File)) { throw "File not found: $File" }
& $signtool.FullName sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampServer /td SHA256 $File
if ($LASTEXITCODE -ne 0) { throw "Signing failed with exit code $LASTEXITCODE." }
& $signtool.FullName verify /pa /all $File
if ($LASTEXITCODE -ne 0) { throw 'Signature verification failed.' }
Write-Host "Signature verified: $File"
