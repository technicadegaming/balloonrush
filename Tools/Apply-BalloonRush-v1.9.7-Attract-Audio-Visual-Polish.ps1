param([string]$ProjectRoot = (Get-Location).Path)
$ErrorActionPreference = "Stop"

function Read-Text([string]$path) { if (-not (Test-Path $path)) { throw "Missing required file: $path" }; [System.IO.File]::ReadAllText($path) }
function Write-Text([string]$path,[string]$text) { [System.IO.File]::WriteAllText($path,$text,[System.Text.UTF8Encoding]::new($false)) }
function Backup-Once([string]$path) { $b="$path.v1.9.7-backup"; if (-not (Test-Path $b)) { Copy-Item $path $b } }

$settingsPath = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\SaveSystem\OperatorSettings.cs"
$menuPath = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\UI\OperatorMenuManager.cs"
$audioPath = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\Audio\AudioManager.cs"
$audioConfigPath = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\Audio\AudioConfig.cs"
$gamePath = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\Core\GameManager.cs"
foreach ($p in @($settingsPath,$menuPath,$audioPath,$audioConfigPath,$gamePath)) { Backup-Once $p }

# Ensure v1.9.6 playlist fields exist even if 1.9.7 is applied directly after an earlier build.
$text = Read-Text $audioConfigPath
if (-not $text.Contains("gameplayMusicAlt1")) {
    $text = $text.Replace("        public AudioClip gameplayMusic;", "        public AudioClip gameplayMusic;`r`n        public AudioClip gameplayMusicAlt1;`r`n        public AudioClip gameplayMusicAlt2;")
    Write-Text $audioConfigPath $text
}

# Operator settings: edge lighting and retained round-music fields.
$text = Read-Text $settingsPath
if (-not $text.Contains("cabinetEdgeLightsEnabled")) {
    $needle = "        public float sfxVolume = 0.9f;"
    $insert = @"
        public float sfxVolume = 0.9f;
        public bool cabinetEdgeLightsEnabled = true;
        public float attractEdgeFlickerIntensity = 0.85f;
        public float gameplayEdgePulseMinHz = 1.35f;
        public float gameplayEdgePulseMaxHz = 4.25f;
"@
    if (-not $text.Contains($needle)) { throw "Could not locate sfxVolume in OperatorSettings.cs" }
    $text = $text.Replace($needle,$insert.TrimEnd())
}
if (-not $text.Contains("gameplayMusicRotationEnabled")) {
    $needle = "        public bool cabinetEdgeLightsEnabled = true;"
    $insert = @"
        public bool gameplayMusicRotationEnabled = true;
        public float gameplayMusicRotateSeconds = 8f; // legacy serialized field; no longer changes songs mid-round
        public float gameplayMusicStartPitch = 0.98f;
        public float gameplayMusicEndPitch = 1.12f;
        public bool cabinetEdgeLightsEnabled = true;
"@
    $text = $text.Replace($needle,$insert.TrimEnd())
}
# Make future defaults more natural even if v1.9.6 already added the fields.
$text = $text.Replace("public float gameplayMusicStartPitch = 0.96f;","public float gameplayMusicStartPitch = 0.98f;")
$text = $text.Replace("public float gameplayMusicEndPitch = 1.18f;","public float gameplayMusicEndPitch = 1.12f;")
if (-not $text.Contains("attractEdgeFlickerIntensity = Mathf.Clamp01")) {
    $needle = "            sfxVolume = Mathf.Clamp01(sfxVolume);"
    if ($text.Contains($needle)) {
        $text = $text.Replace($needle, @"
            sfxVolume = Mathf.Clamp01(sfxVolume);
            attractEdgeFlickerIntensity = Mathf.Clamp01(attractEdgeFlickerIntensity);
            gameplayEdgePulseMinHz = Mathf.Clamp(gameplayEdgePulseMinHz, 0.4f, 3f);
            gameplayEdgePulseMaxHz = Mathf.Clamp(gameplayEdgePulseMaxHz, gameplayEdgePulseMinHz, 5f);
"@.TrimEnd())
    }
}
Write-Text $settingsPath $text

# Operator menu: lighting controls + change music wording; remove seconds-based mid-round row.
$text = Read-Text $menuPath
$text = $text.Replace('AddToggleField("Gameplay music rotation"','AddToggleField("Different gameplay song each round"')
$lines = $text -split "`r?`n"
$lines = $lines | Where-Object { $_ -notmatch 'Gameplay track rotate every \(seconds\)' }
$text = [string]::Join([Environment]::NewLine,$lines)
if (-not $text.Contains("Attract edge flicker intensity")) {
    $needle = '            AddFloatField("SFX volume (0-1)", () => editable.sfxVolume, value => editable.sfxVolume = value);'
    if ($text.Contains($needle)) {
        $insert = @'
            AddFloatField("SFX volume (0-1)", () => editable.sfxVolume, value => editable.sfxVolume = value);
            AddToggleField("Different gameplay song each round", () => editable.gameplayMusicRotationEnabled, value => editable.gameplayMusicRotationEnabled = value);
            AddFloatField("Gameplay music START pitch", () => editable.gameplayMusicStartPitch, value => editable.gameplayMusicStartPitch = value);
            AddFloatField("Gameplay music END pitch", () => editable.gameplayMusicEndPitch, value => editable.gameplayMusicEndPitch = value);
            AddToggleField("Cabinet edge lights", () => editable.cabinetEdgeLightsEnabled, value => editable.cabinetEdgeLightsEnabled = value);
            AddFloatField("Attract edge flicker intensity (0-1)", () => editable.attractEdgeFlickerIntensity, value => editable.attractEdgeFlickerIntensity = value);
            AddFloatField("Gameplay edge pulse MIN Hz", () => editable.gameplayEdgePulseMinHz, value => editable.gameplayEdgePulseMinHz = value);
            AddFloatField("Gameplay edge pulse MAX Hz", () => editable.gameplayEdgePulseMaxHz, value => editable.gameplayEdgePulseMaxHz = value);
'@
        $text = $text.Replace($needle,$insert.TrimEnd())
    }
}
# De-duplicate v1.9.6 audio rows if both old/new sections are present.
$seen=@{}
$out=New-Object System.Collections.Generic.List[string]
foreach($line in ($text -split "`r?`n")) {
    $key=$null
    if($line -match 'AddToggleField\("Different gameplay song each round"'){$key='songround'}
    elseif($line -match 'AddFloatField\("Gameplay music START pitch"'){$key='startpitch'}
    elseif($line -match 'AddFloatField\("Gameplay music END pitch"'){$key='endpitch'}
    if($key -and $seen.ContainsKey($key)){continue}
    if($key){$seen[$key]=$true}
    $out.Add($line)
}
Write-Text $menuPath ([string]::Join([Environment]::NewLine,$out))

# AudioManager: create/upgrade the round playlist API. This is self-contained and
# works whether or not the earlier v1.9.6 music patch was applied.
$text = Read-Text $audioPath
if (-not $text.Contains("gameplayRotationIndex")) {
    $text = $text.Replace("        private Coroutine crossfadeRoutine;", "        private Coroutine crossfadeRoutine;`r`n        private int gameplayRotationIndex = -1;`r`n        private float musicPitch = 1f;")
}

if (-not $text.Contains("public void StartGameplayMusicRotation")) {
    $needle = "        public void StopMusic(float fadeSeconds = 0.3f)"
    if (-not $text.Contains($needle)) { throw "Could not locate StopMusic() in AudioManager.cs" }
    $methods = @'
        public void StartGameplayMusicRotation(float fadeSeconds = 0.35f)
        {
            AudioClip[] playlist = GetGameplayPlaylist();
            if (playlist == null || playlist.Length == 0)
            {
                PlayMusic(MusicCue.Gameplay, fadeSeconds);
                return;
            }

            OperatorSettings current = settingsManager != null ? settingsManager.Current : null;
            bool rotatePerRound = current == null || current.gameplayMusicRotationEnabled;
            if (!rotatePerRound || playlist.Length == 1)
            {
                gameplayRotationIndex = 0;
            }
            else
            {
                gameplayRotationIndex = (gameplayRotationIndex + 1) % playlist.Length;
            }

            PlayMusicClip(playlist[gameplayRotationIndex], fadeSeconds);
        }

        public void StopGameplayMusicRotation()
        {
            // v1.9.7 intentionally has no mid-round rotation coroutine.
        }

        public void SetMusicPitch(float pitch)
        {
            musicPitch = Mathf.Clamp(pitch, 0.65f, 1.5f);
            EnsureSources();
            musicSourceA.pitch = musicPitch;
            musicSourceB.pitch = musicPitch;
        }

        private AudioClip[] GetGameplayPlaylist()
        {
            List<AudioClip> clips = new List<AudioClip>(3);
            if (config != null)
            {
                if (config.gameplayMusic != null) clips.Add(config.gameplayMusic);
                if (config.gameplayMusicAlt1 != null && !clips.Contains(config.gameplayMusicAlt1)) clips.Add(config.gameplayMusicAlt1);
                if (config.gameplayMusicAlt2 != null && !clips.Contains(config.gameplayMusicAlt2)) clips.Add(config.gameplayMusicAlt2);
            }
            return clips.ToArray();
        }

        private void PlayMusicClip(AudioClip nextClip, float fadeSeconds)
        {
            EnsureSources();
            if (nextClip == null) return;
            if (activeMusicSource != null && activeMusicSource.clip == nextClip && activeMusicSource.isPlaying)
            {
                activeMusicSource.pitch = musicPitch;
                return;
            }
            if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = StartCoroutine(Crossfade(nextClip, Mathf.Max(0.01f, fadeSeconds)));
        }

'@
    $text = $text.Replace($needle,$methods + $needle)
}
else {
    # Replace the v1.9.6 timer-based rotation body with a once-per-round selection.
    $pattern = '(?s)        public void StartGameplayMusicRotation\(float fadeSeconds = 0\.35f\)\s*\{.*?\n        \}\n\n        public void StopGameplayMusicRotation\(\)'
    $replacement = @'
        public void StartGameplayMusicRotation(float fadeSeconds = 0.35f)
        {
            StopGameplayMusicRotation();
            AudioClip[] playlist = GetGameplayPlaylist();
            if (playlist == null || playlist.Length == 0)
            {
                PlayMusic(MusicCue.Gameplay, fadeSeconds);
                return;
            }

            OperatorSettings current = settingsManager != null ? settingsManager.Current : null;
            bool rotatePerRound = current == null || current.gameplayMusicRotationEnabled;
            if (!rotatePerRound || playlist.Length == 1)
            {
                gameplayRotationIndex = 0;
            }
            else
            {
                gameplayRotationIndex = (gameplayRotationIndex + 1) % playlist.Length;
            }

            // One song is selected here and remains locked for the whole round.
            PlayMusicClip(playlist[gameplayRotationIndex], fadeSeconds);
        }

        public void StopGameplayMusicRotation()
'@
    $text = [regex]::Replace($text,$pattern,$replacement)
}

# Carry dynamic pitch through crossfades on either base or v1.9.6 code.
if (-not $text.Contains("to.pitch = musicPitch;")) {
    $needle = "            to.volume = 0f;`r`n            to.Play();"
    if (-not $text.Contains($needle)) { $needle = "            to.volume = 0f;`n            to.Play();" }
    $replacement = "            to.volume = 0f;`r`n            to.pitch = musicPitch;`r`n            if (from != null) from.pitch = musicPitch;`r`n            to.Play();"
    $text = $text.Replace($needle,$replacement)
}
Write-Text $audioPath $text

# GameManager: start one round track, continuously raise its pitch with difficulty,
# and use SFX stingers for Rush/Golden/Jackpot instead of changing songs.
$text = Read-Text $gamePath
$text = $text.Replace("            GameServices.Audio?.PlayMusic(MusicCue.Gameplay);", "            GameServices.Audio?.StartGameplayMusicRotation();")

# Base Rush handler.
$text = $text.Replace('            GameServices.Audio?.PlayMusic(active ? MusicCue.Rush : MusicCue.Gameplay, 0.35f);', @'
            if (active)
            {
                GameServices.Audio?.PlaySfx(AudioCue.BonusStart, 1.06f, 0.90f);
            }
'@.TrimEnd())
# v1.9.6 Rush handler.
$oldRush = @'
            if (active)
            {
                GameServices.Audio?.StopGameplayMusicRotation();
                GameServices.Audio?.PlayMusic(MusicCue.Rush, 0.35f);
            }
            else
            {
                GameServices.Audio?.StartGameplayMusicRotation(0.35f);
            }
'@
$newRush = @'
            if (active)
            {
                GameServices.Audio?.PlaySfx(AudioCue.BonusStart, 1.06f, 0.90f);
            }
'@
$text = $text.Replace($oldRush,$newRush)

# Golden round start (base and v1.9.6).
$text = $text.Replace("            GameServices.Audio?.PlayMusic(MusicCue.GoldenRound, 0.25f);", "            GameServices.Audio?.PlaySfx(AudioCue.GoldenBalloonAppear, 1f, 0.95f);")
$text = $text.Replace("            GameServices.Audio?.StopGameplayMusicRotation();`r`n            GameServices.Audio?.PlayMusic(MusicCue.GoldenRound, 0.25f);", "            GameServices.Audio?.PlaySfx(AudioCue.GoldenBalloonAppear, 1f, 0.95f);")
$text = $text.Replace("            GameServices.Audio?.StopGameplayMusicRotation();`n            GameServices.Audio?.PlayMusic(MusicCue.GoldenRound, 0.25f);", "            GameServices.Audio?.PlaySfx(AudioCue.GoldenBalloonAppear, 1f, 0.95f);")

# Golden round end should not restart/swap the song.
$text = $text.Replace("            GameServices.Audio?.PlayMusic(rush ? MusicCue.Rush : MusicCue.Gameplay, 0.35f);", "            // v1.9.7 keeps the selected round music playing.")
$oldGolden = @'
            if (rush)
            {
                GameServices.Audio?.PlayMusic(MusicCue.Rush, 0.35f);
            }
            else
            {
                GameServices.Audio?.StartGameplayMusicRotation(0.35f);
            }
'@
$text = $text.Replace($oldGolden,"            // v1.9.7 keeps the selected round music playing.`r`n")

# Jackpot becomes a stinger rather than a music replacement.
$text = $text.Replace("            GameServices.Audio?.PlayMusic(MusicCue.Jackpot, 0.12f);", "            GameServices.Audio?.PlaySfx(AudioCue.Jackpot, 1f, 1f);")
$text = $text.Replace("            GameServices.Audio?.StopGameplayMusicRotation();`r`n            GameServices.Audio?.PlayMusic(MusicCue.Jackpot, 0.12f);", "            GameServices.Audio?.PlaySfx(AudioCue.Jackpot, 1f, 1f);")
$text = $text.Replace("            GameServices.Audio?.StopGameplayMusicRotation();`n            GameServices.Audio?.PlayMusic(MusicCue.Jackpot, 0.12f);", "            GameServices.Audio?.PlaySfx(AudioCue.Jackpot, 1f, 1f);")

# Add pitch tracking if v1.9.6 did not already add it.
if (-not $text.Contains("gameplayMusicEndPitch")) {
    $marker = "        private void SubscribeGameplayEvents()"
    if (-not $text.Contains($marker)) { throw "Could not locate SubscribeGameplayEvents() in GameManager.cs" }
    $update = @'
        private void Update()
        {
            if (!gameplayActive || ending || GameServices.Audio == null)
            {
                return;
            }

            float progress = difficultyManager != null
                ? difficultyManager.NormalizedProgress
                : (roundManager != null ? roundManager.NormalizedProgress : 0f);
            float startPitch = settings != null ? settings.gameplayMusicStartPitch : 0.98f;
            float endPitch = settings != null ? settings.gameplayMusicEndPitch : 1.12f;
            float pitch = Mathf.Lerp(startPitch, endPitch, Mathf.Clamp01(progress));
            if (roundManager != null && roundManager.IsRushMode) pitch += 0.025f;
            GameServices.Audio.SetMusicPitch(pitch);
        }


'@
    $text = $text.Replace($marker,$update + $marker)
}

# Reset pitch for Results. Avoid duplicating if v1.9.6 already did it.
if (-not $text.Contains("SetMusicPitch(1f);")) {
    $text = $text.Replace("            GameServices.Audio?.PlayMusic(MusicCue.Results, 0.4f);", "            GameServices.Audio?.StopGameplayMusicRotation();`r`n            GameServices.Audio?.SetMusicPitch(1f);`r`n            GameServices.Audio?.PlayMusic(MusicCue.Results, 0.4f);")
}
Write-Text $gamePath $text

Write-Host ""; Write-Host "Balloon Rush v1.9.7 code patch applied." -ForegroundColor Green
Write-Host "Return to Unity, wait for compile, then run:" -ForegroundColor Cyan
Write-Host "Tools > Balloon Rush > v1.9.7 - Install Attract + Audio + Visual Polish" -ForegroundColor Cyan
