param([Parameter(Mandatory)][string]$Project, [Parameter(Mandatory)][string]$Output)
$ErrorActionPreference = 'Stop'
$entry = Get-ChildItem -LiteralPath $Project -Filter '*.py' -File | Where-Object Name -in @('main.py','app.py') | Select-Object -First 1
if (-not $entry) { throw 'Python adapter needs main.py or app.py at the project root.' }
if (-not (Get-Command pyinstaller -ErrorAction SilentlyContinue)) { throw 'PyInstaller is not installed. Install it with: python -m pip install pyinstaller' }
New-Item -ItemType Directory -Path $Output -Force | Out-Null
& pyinstaller --noconfirm --clean --onefile --name $entry.BaseName --distpath $Output --workpath (Join-Path $Output '.build') --specpath (Join-Path $Output '.build') $entry.FullName
if ($LASTEXITCODE -ne 0) { throw "PyInstaller failed with exit code $LASTEXITCODE." }
Remove-Item (Join-Path $Output '.build') -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Python executable created in $Output"
