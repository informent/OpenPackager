param([Parameter(Mandatory)][string]$Project, [Parameter(Mandatory)][string]$Output)
$ErrorActionPreference = 'Stop'
$package = Join-Path $Project 'package.json'
if (-not (Test-Path $package)) { throw 'Node adapter requires package.json.' }
$pkg = Get-Command pkg -ErrorAction SilentlyContinue
if (-not $pkg) { throw 'Node executable bundler is not installed. Install @yao-pkg/pkg or pkg before using this adapter.' }
$json = Get-Content $package -Raw | ConvertFrom-Json
$entry = if ($json.bin -is [string]) { Join-Path $Project $json.bin } elseif ($json.main) { Join-Path $Project $json.main } else { throw 'package.json needs bin or main.' }
New-Item -ItemType Directory -Path $Output -Force | Out-Null
& $pkg.Source $entry --targets node24-win-x64 --output (Join-Path $Output $json.name)
if ($LASTEXITCODE -ne 0) { throw "Node bundler failed with exit code $LASTEXITCODE." }
Write-Host "Node executable created in $Output"
