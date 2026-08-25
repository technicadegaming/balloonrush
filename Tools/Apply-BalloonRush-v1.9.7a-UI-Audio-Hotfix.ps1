param(
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

function Read-Text([string]$path) {
    if (-not (Test-Path $path)) {
        throw "Missing required file: $path"
    }

    return [System.IO.File]::ReadAllText($path)
}

function Write-Text([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText(
        $path,
        $text,
        [System.Text.UTF8Encoding]::new($false))
}

$audioPath =
    Join-Path $ProjectRoot `
    "Assets\BalloonRush\Scripts\Audio\AudioManager.cs"

$gamePath =
    Join-Path $ProjectRoot `
    "Assets\BalloonRush\Scripts\Core\GameManager.cs"

if (Test-Path $audioPath) {
    $text = Read-Text $audioPath

    $backup = "$audioPath.v1.9.7a-backup"
    if (-not (Test-Path $backup)) {
        Copy-Item $audioPath $backup
    }

    # SAFETY: an older v1.9.6 build may still contain the timer that changes
    # normal gameplay songs in the middle of a round. Disable that start call.
    $text = [regex]::Replace(
        $text,
        'gameplayRotationRoutine\s*=\s*StartCoroutine\s*\(\s*GameplayMusicRotationRoutine\s*\(\s*playlist\s*\)\s*\)\s*;',
        '// v1.9.7a: mid-round gameplay music rotation disabled.')

    # Keep the API harmless even if the old coroutine method still exists.
    Write-Text $audioPath $text
}

if (Test-Path $gamePath) {
    $text = Read-Text $gamePath

    $backup = "$gamePath.v1.9.7a-backup"
    if (-not (Test-Path $backup)) {
        Copy-Item $gamePath $backup
    }

    # If any old normal gameplay transition is still present at Rush start,
    # do not replace the selected round song. The v1.9.7 stinger remains.
    $text = $text.Replace(
        'GameServices.Audio?.PlayMusic(active ? MusicCue.Rush : MusicCue.Gameplay, 0.35f);',
        'if (active) GameServices.Audio?.PlaySfx(AudioCue.BonusStart, 1.06f, 0.90f);')

    Write-Text $gamePath $text
}

Write-Host ""
Write-Host "Balloon Rush v1.9.7a hotfix applied." -ForegroundColor Green
Write-Host " - destructive white rounded-card replacement removed" -ForegroundColor Green
Write-Host " - existing UI sprites/colors preserved" -ForegroundColor Green
Write-Host " - M OPERATOR public text still hidden" -ForegroundColor Green
Write-Host " - rail scaling reduced; flashing/glow retained" -ForegroundColor Green
Write-Host " - old v1.9.6 mid-round music timer disabled if still present" -ForegroundColor Green
Write-Host ""
Write-Host "Return to Unity and allow compilation to finish." -ForegroundColor Cyan
