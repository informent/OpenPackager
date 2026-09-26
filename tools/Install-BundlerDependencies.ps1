param([switch]$InstallPython, [switch]$InstallNode)
$ErrorActionPreference = 'Stop'
if (-not $InstallPython -and -not $InstallNode) { Write-Host 'Specify -InstallPython and/or -InstallNode. No changes were made.'; exit 2 }
if ($InstallPython) { if (-not (Get-Command py -ErrorAction SilentlyContinue)) { throw 'Python launcher (py.exe) was not found.' }; & py -m pip install --upgrade pyinstaller; if ($LASTEXITCODE -ne 0) { throw 'PyInstaller installation failed.' } }
if ($InstallNode) { if (-not (Get-Command npm -ErrorAction SilentlyContinue)) { throw 'npm was not found.' }; & npm install --global @yao-pkg/pkg; if ($LASTEXITCODE -ne 0) { throw 'Node bundler installation failed.' } }
Write-Host 'Requested bundler dependencies are installed.'
