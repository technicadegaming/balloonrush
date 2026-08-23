param(
    [string]$ProjectPath = "C:\Projects\BalloonRushUnity6"
)

$ErrorActionPreference = "Stop"

$PatchRoot = Split-Path -Parent $PSScriptRoot
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupRoot = Join-Path $ProjectPath "Backups\UI-Cleanup-$Timestamp"

$Required = @("Assets", "Packages", "ProjectSettings")
foreach ($name in $Required) {
    if (-not (Test-Path (Join-Path $ProjectPath $name))) {
        throw "ProjectPath does not look like a Unity project: $ProjectPath (missing $name)"
    }
}

$Files = @(
    "Assets\BalloonRush\Scripts\UI\BalloonRushMainGameVisualRebuild.cs",
    "Assets\BalloonRush\Scripts\UI\RoundedSpriteFactory.cs",
    "Assets\BalloonRush\Editor\BalloonRushV15VisualRebuildInstaller.cs"
)

New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

foreach ($relative in $Files) {
    $source = Join-Path $PatchRoot $relative
    $destination = Join-Path $ProjectPath $relative

    if (-not (Test-Path $source)) {
        throw "Patch file missing: $source"
    }

    if (Test-Path $destination) {
        $backup = Join-Path $BackupRoot $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
        Copy-Item $destination $backup -Force
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item $source $destination -Force
    Write-Host "Updated: $relative"
}

Write-Host ""
Write-Host "Patch applied successfully." -ForegroundColor Green
Write-Host "Backup: $BackupRoot"
Write-Host ""
Write-Host "Next in Unity:"
Write-Host "1. Open C:\Projects\BalloonRushUnity6"
Write-Host "2. Wait for compile"
Write-Host "3. Open MainGame"
Write-Host "4. Tools > Balloon Rush > Install Single UI Visual System"
Write-Host "5. Tools > Balloon Rush > Check MainGame Visual Components"
Write-Host "6. Expected visual component count: 1"
Write-Host "7. Press Play"
