param(
    [string]$ProjectRoot = "C:\Projects\BalloonRushUnity6"
)

$ErrorActionPreference = "Stop"
$PatchRoot = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path (Join-Path $ProjectRoot "Assets"))) {
    throw "Unity project Assets folder was not found at $ProjectRoot"
}

Write-Host "Applying Balloon Rush v1.9 cabinet diagnostics to:" -ForegroundColor Cyan
Write-Host "  $ProjectRoot"

$patchFull = [System.IO.Path]::GetFullPath($PatchRoot).TrimEnd('\')
$projectFull = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\')
if ($patchFull -ieq $projectFull) {
    Write-Host "Patch is already extracted directly into the Unity project. No file copy is needed." -ForegroundColor Yellow
} else {
    $folders = @("Assets", "Documentation")
    foreach ($folder in $folders) {
        $source = Join-Path $PatchRoot $folder
        if (Test-Path $source) {
            Copy-Item $source $ProjectRoot -Recurse -Force
        }
    }
    Write-Host "v1.9 files copied successfully." -ForegroundColor Green
}

Write-Host "Open Unity, then run:" -ForegroundColor Yellow
Write-Host "  Tools > Balloon Rush > v1.9 - Install Cabinet Diagnostics"
Write-Host "  Tools > Balloon Rush > v1.9 - Verify Cabinet Diagnostics"
