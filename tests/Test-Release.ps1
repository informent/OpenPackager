$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$required = @(
    (Join-Path $root 'PackagingEngine.cs'),
    (Join-Path $root 'installer\Install-OpenPackager.ps1'),
    (Join-Path $root 'installer\Uninstall-OpenPackager.ps1'),
    (Join-Path $root 'tools\Sign-Release.ps1')
)
foreach ($file in $required) { if (-not (Test-Path $file)) { throw "Required release component is missing: $file" } }
$null = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $root 'installer\Install-OpenPackager.ps1'), [ref]$null, [ref]$null)
$null = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $root 'installer\Uninstall-OpenPackager.ps1'), [ref]$null, [ref]$null)
$null = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $root 'tools\Sign-Release.ps1'), [ref]$null, [ref]$null)
Write-Host 'PASS: packaging, installer, and signing sources are present'
$out = Join-Path $root 'test-output'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null
dotnet publish (Join-Path $root 'OpenPackager.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -o $out | Out-Host
$exe = Join-Path $out 'OpenPackager.exe'
if (-not (Test-Path $exe)) { throw 'Published executable is missing.' }
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$p = Start-Process $exe -PassThru
Start-Sleep -Seconds 3
if ($p.HasExited) { throw "Published GUI exited during smoke test with code $($p.ExitCode)." }
Stop-Process -Id $p.Id -Force
Write-Host "PASS: published GUI stayed alive"
Write-Host "PASS: executable SHA256 $hash"
for ($attempt = 1; $attempt -le 5; $attempt++) {
    try { Remove-Item $out -Recurse -Force -ErrorAction Stop; break }
    catch { if ($attempt -eq 5) { Write-Warning "Could not remove test output; it can be cleaned later." } else { Start-Sleep -Milliseconds 500 } }
}
