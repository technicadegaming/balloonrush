$ErrorActionPreference = "Stop"

function Write-Info($message) {
    Write-Host "[Balloon Rush v1.8.5] $message" -ForegroundColor Cyan
}

function Write-Ok($message) {
    Write-Host "[OK] $message" -ForegroundColor Green
}

function Write-Warn($message) {
    Write-Host "[WARN] $message" -ForegroundColor Yellow
}

function Normalize-LF([string]$text) {
    return $text.Replace("`r`n", "`n").Replace("`r", "`n")
}

function Write-Utf8NoBom([string]$path, [string]$text) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $text.Replace("`n", "`r`n"), $encoding)
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$candidates = @(
    (Get-Location).Path,
    $scriptDir,
    (Split-Path -Parent $scriptDir)
) | Select-Object -Unique

$projectRoot = $null
foreach ($candidate in $candidates) {
    if (Test-Path (Join-Path $candidate "Assets\BalloonRush\Scripts")) {
        $projectRoot = $candidate
        break
    }
}

if (-not $projectRoot) {
    Write-Host ""
    Write-Host "Unity project not found." -ForegroundColor Red
    Write-Host "Extract this patch into the ROOT of your Balloon Rush Unity project" -ForegroundColor Yellow
    Write-Host "(the folder that contains Assets, Packages and ProjectSettings), then run APPLY_PATCH.bat again."
    exit 1
}

Write-Info "Project root: $projectRoot"

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = Join-Path $projectRoot "Backups\BalloonRush_v1.8.5_$timestamp"
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null

$audioManagerPath = Join-Path $projectRoot "Assets\BalloonRush\Scripts\Audio\AudioManager.cs"
$balloonManagerPath = Join-Path $projectRoot "Assets\BalloonRush\Scripts\Gameplay\BalloonManager.cs"

if (-not (Test-Path $audioManagerPath)) {
    throw "AudioManager.cs not found at expected path: $audioManagerPath"
}

if (-not (Test-Path $balloonManagerPath)) {
    throw "BalloonManager.cs not found at expected path: $balloonManagerPath"
}

# Backup the two files that this patch edits.
$backupAudio = Join-Path $backupRoot "AudioManager.cs"
$backupBalloon = Join-Path $backupRoot "BalloonManager.cs"
Copy-Item $audioManagerPath $backupAudio -Force
Copy-Item $balloonManagerPath $backupBalloon -Force
Write-Ok "Backed up AudioManager.cs and BalloonManager.cs"

# Copy additive patch scripts.
$sourcePatchFiles = Join-Path $scriptDir "PatchFiles"
$visualSource = Join-Path $sourcePatchFiles "Assets\BalloonRush\Scripts\UI\BalloonRushArcadeJuiceV185.cs"
$gateSource = Join-Path $sourcePatchFiles "Assets\BalloonRush\Scripts\Audio\BalloonRushAudioGateV185.cs"

$visualDest = Join-Path $projectRoot "Assets\BalloonRush\Scripts\UI\BalloonRushArcadeJuiceV185.cs"
$gateDest = Join-Path $projectRoot "Assets\BalloonRush\Scripts\Audio\BalloonRushAudioGateV185.cs"

Copy-Item $visualSource $visualDest -Force
Copy-Item $gateSource $gateDest -Force
Write-Ok "Installed new arcade visual and audio gate scripts"

# --------------------------------------------------------------------------
# Patch AudioManager.cs
# --------------------------------------------------------------------------
$audio = Normalize-LF (Get-Content -Raw $audioManagerPath)

if ($audio.Contains("BalloonRushAudioGateV185.Allow(cue)")) {
    Write-Warn "AudioManager.cs already contains the v1.8.5 audio gate. Skipping audio method edits."
}
else {
    $newPlaySfx = @'
        public void PlaySfx(AudioCue cue, float pitch = 1f, float volumeScale = 1f)
        {
            EnsureSources();

            if (!BalloonRushAudioGateV185.Allow(cue))
            {
                return;
            }

            AudioClip clip = GetSfxClip(cue);
            if (clip == null)
            {
                return;
            }

            AudioSource destination = cue == AudioCue.Jackpot ? jackpotSource : sfxSource;

            // Important one-off events should replace their previous instance rather
            // than piling another copy on top.
            if (cue == AudioCue.Jackpot ||
                cue == AudioCue.BombExplosion ||
                cue == AudioCue.BonusStart ||
                cue == AudioCue.GoldenBalloonPop)
            {
                destination.Stop();
            }

            destination.pitch = Mathf.Clamp(pitch, 0.5f, 2f);

            float cleanVolume = Mathf.Clamp01(
                volumeScale * BalloonRushAudioGateV185.GetVolumeScale(cue));

            destination.PlayOneShot(clip, cleanVolume);
            destination.pitch = 1f;
        }
'@

    $patternSfx = '(?s)        public void PlaySfx\(AudioCue cue, float pitch = 1f, float volumeScale = 1f\)\n        \{.*?\n        \}\n\n        public void PlayUi'
    $replacementSfx = $newPlaySfx + "`n        public void PlayUi"

    $updated = [regex]::Replace($audio, $patternSfx, $replacementSfx, 1)
    if ($updated -eq $audio) {
        throw "Could not patch AudioManager.PlaySfx(). The file differs from the expected Balloon Rush structure."
    }
    $audio = $updated

    $newPlayUi = @'
        public void PlayUi(AudioCue cue)
        {
            EnsureSources();

            if (!BalloonRushAudioGateV185.Allow(cue))
            {
                return;
            }

            AudioClip clip = GetSfxClip(cue);
            if (clip != null)
            {
                // UI clicks should never create a stack of overlapping clicks.
                uiSource.Stop();
                uiSource.PlayOneShot(
                    clip,
                    BalloonRushAudioGateV185.GetVolumeScale(cue));
            }
        }
'@

    $patternUi = '(?s)        public void PlayUi\(AudioCue cue\)\n        \{.*?\n        \}\n\n        public void PlayVoice'
    $replacementUi = $newPlayUi + "`n        public void PlayVoice"

    $updated = [regex]::Replace($audio, $patternUi, $replacementUi, 1)
    if ($updated -eq $audio) {
        throw "Could not patch AudioManager.PlayUi()."
    }
    $audio = $updated

    $newPlayVoice = @'
        public void PlayVoice(AudioClip clip, float volumeScale = 1f)
        {
            EnsureSources();
            if (clip != null)
            {
                // Voice lines replace the prior voice line instead of talking over it.
                voiceSource.Stop();
                voiceSource.PlayOneShot(
                    clip,
                    Mathf.Clamp01(volumeScale * 0.80f));
            }
        }
'@

    $patternVoice = '(?s)        public void PlayVoice\(AudioClip clip, float volumeScale = 1f\)\n        \{.*?\n        \}\n\n        public void PlayMusic'
    $replacementVoice = $newPlayVoice + "`n        public void PlayMusic"

    $updated = [regex]::Replace($audio, $patternVoice, $replacementVoice, 1)
    if ($updated -eq $audio) {
        throw "Could not patch AudioManager.PlayVoice()."
    }
    $audio = $updated

    Write-Utf8NoBom $audioManagerPath $audio
    Write-Ok "Patched AudioManager: burst limiting, event replacement, quieter cue balance"
}

# --------------------------------------------------------------------------
# Patch BalloonManager.cs
# --------------------------------------------------------------------------
$balloon = Normalize-LF (Get-Content -Raw $balloonManagerPath)

if ($balloon.Contains("v1.8.5 audio ownership")) {
    Write-Warn "BalloonManager.cs already contains the v1.8.5 audio changes. Skipping gameplay audio edits."
}
else {
    $desiredPopBlock = @'
            effectsManager?.PlaySuccessfulPop(target.transform.position, definition.VisualColor, rating, ticketAward);

            // v1.8.5 audio ownership:
            // Normal pops get ONE timing sound. Combo milestones and special
            // golden/jackpot events own their own sound so they do not stack.
            bool specialOwnsAudio =
                definition.SpecialBehavior == BalloonSpecialBehavior.StartGoldenRound ||
                definition.SpecialBehavior == BalloonSpecialBehavior.ResolveJackpot;

            bool milestoneOwnsAudio =
                comboManager != null &&
                comboManager.CurrentCombo >= 5 &&
                comboManager.CurrentCombo % 5 == 0;

            if (!specialOwnsAudio && !milestoneOwnsAudio)
            {
                PlayTimingAudio(rating);
            }

            ApplySpecialBehavior(target, definition, rating);
'@

    $patternPopBlock = '(?s)            effectsManager\?\.PlaySuccessfulPop\(target\.transform\.position, definition\.VisualColor, rating, ticketAward\);\n.*?            ApplySpecialBehavior\(target, definition, rating\);\n'
    $updated = [regex]::Replace($balloon, $patternPopBlock, $desiredPopBlock, 1)

    if ($updated -eq $balloon) {
        throw "Could not patch BalloonManager successful-pop audio block."
    }
    $balloon = $updated

    $desiredGoldenCase = @'
                case BalloonSpecialBehavior.StartGoldenRound:
                    scoreManager?.MarkGoldenBalloon();
                    effectsManager?.PlayGoldenBalloon(target.transform.position);
                    GameEvents.RaiseGoldenBalloonPopped();

                    bool goldenStarted =
                        goldenRoundManager != null &&
                        goldenRoundManager.StartGoldenRound();

                    // One golden event cue, not GoldenPop + BonusStart together.
                    GameServices.Audio?.PlaySfx(
                        goldenStarted
                            ? AudioCue.BonusStart
                            : AudioCue.GoldenBalloonPop);
                    break;

                case BalloonSpecialBehavior.ResolveJackpot:
'@

    $patternGolden = '(?s)                case BalloonSpecialBehavior\.StartGoldenRound:\n.*?                    break;\n\n                case BalloonSpecialBehavior\.ResolveJackpot:\n'
    $updated = [regex]::Replace($balloon, $patternGolden, $desiredGoldenCase, 1)

    if ($updated -eq $balloon) {
        throw "Could not patch BalloonManager golden-round audio block."
    }
    $balloon = $updated

    $desiredJackpotCase = @'
                case BalloonSpecialBehavior.ResolveJackpot:
                    goldenRoundManager?.ResolveFinalBalloon(rating);

                    if (rating == TimingRating.Perfect)
                    {
                        effectsManager?.PlayJackpot(target.transform.position);
                        GameServices.Audio?.PlaySfx(AudioCue.Jackpot);
                    }
                    else
                    {
                        // Non-jackpot final-balloon hits still get one timing cue.
                        PlayTimingAudio(rating);
                    }
                    break;
'@

    $patternJackpot = '(?s)                case BalloonSpecialBehavior\.ResolveJackpot:\n.*?                    break;\n            \}'
    $replacementJackpot = $desiredJackpotCase + "`n            }"

    $updated = [regex]::Replace($balloon, $patternJackpot, $replacementJackpot, 1)

    if ($updated -eq $balloon) {
        throw "Could not patch BalloonManager jackpot audio block."
    }
    $balloon = $updated

    Write-Utf8NoBom $balloonManagerPath $balloon
    Write-Ok "Patched BalloonManager: one primary sound per pop/event"
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor DarkCyan
Write-Host " Balloon Rush v1.8.5 enhancement patch applied successfully " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor DarkCyan
Write-Host ""
Write-Host "Backup created at:"
Write-Host "  $backupRoot" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next:"
Write-Host "  1. Open/return to Unity."
Write-Host "  2. Let scripts compile."
Write-Host "  3. Open MainGame and press Play."
Write-Host ""
Write-Host "No scene wiring is required."
