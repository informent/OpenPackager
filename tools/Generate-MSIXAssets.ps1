param([string]$Output = (Join-Path $PSScriptRoot '..\Assets'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path $Output -Force | Out-Null
foreach ($size in @(44, 150, 310)) {
    $bmp = New-Object Drawing.Bitmap $size, $size
    $g = [Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode = 'AntiAlias'
    $g.Clear([Drawing.Color]::FromArgb(37, 99, 217))
    $pen = New-Object Drawing.Pen ([Drawing.Color]::White), ([Math]::Max(3, [int]($size / 10)))
    $margin = [int]($size * .2); $g.DrawEllipse($pen, $margin, $margin, $size - ($margin * 2), $size - ($margin * 2))
    $g.Dispose(); $bmp.Save((Join-Path $Output "$size.png"), [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}
Copy-Item (Join-Path $Output '44.png') (Join-Path $Output 'StoreLogo.png') -Force
Write-Host "Generated MSIX assets in $Output"
