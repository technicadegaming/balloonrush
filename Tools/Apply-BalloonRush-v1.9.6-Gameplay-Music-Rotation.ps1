param(
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

function Read-Text([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing required file: $path" }
    return [System.IO.File]::ReadAllText($path).Replace("`r`n", "`n")
}

function Write-Text([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Backup-Once([string]$path) {
    $backup = "$path.v1.9.6-backup"
    if (-not (Test-Path $backup)) { Copy-Item $path $backup }
}

$audioConfig = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\Audio\AudioConfig.cs"
$audioManager = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\Audio\AudioManager.cs"
$operatorSettings = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\SaveSystem\OperatorSettings.cs"
$operatorMenu = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\UI\OperatorMenuManager.cs"
$gameManager = Join-Path $ProjectRoot "Assets\BalloonRush\Scripts\Core\GameManager.cs"

foreach ($f in @($audioConfig,$audioManager,$operatorSettings,$operatorMenu,$gameManager)) {
    Backup-Once $f
}

# ---------------- AudioConfig ----------------
$text = Read-Text $audioConfig
if (-not $text.Contains("gameplayMusicAlt1")) {
    $text = $text.Replace(
'        public AudioClip gameplayMusic;',
@'
        public AudioClip gameplayMusic;
        [Tooltip("Additional gameplay tracks used by the rotating gameplay playlist.")]
        public AudioClip gameplayMusicAlt1;
        public AudioClip gameplayMusicAlt2;
'@
    )
    Write-Text $audioConfig $text
}

# ---------------- OperatorSettings ----------------
$text = Read-Text $operatorSettings
if (-not $text.Contains("gameplayMusicRotationEnabled")) {
    $text = $text.Replace(
'        public float sfxVolume = 0.9f;',
@'
        public float sfxVolume = 0.9f;
        public bool gameplayMusicRotationEnabled = true;
        [Tooltip("Seconds before crossfading to the next normal gameplay music track.")]
        public float gameplayMusicRotateSeconds = 8f;
        [Tooltip("Gameplay music pitch at the beginning of a round.")]
        public float gameplayMusicStartPitch = 0.96f;
        [Tooltip("Gameplay music pitch at the end of a round. Pitch follows DifficultyManager progress.")]
        public float gameplayMusicEndPitch = 1.18f;
'@
    )

    # Commercial/default profile.
    $text = $text.Replace(
'            passedBalloonBreaksCombo = true;',
@'
            passedBalloonBreaksCombo = true;

            gameplayMusicRotationEnabled = true;
            gameplayMusicRotateSeconds = 8f;
            gameplayMusicStartPitch = 0.96f;
            gameplayMusicEndPitch = 1.18f;
'@
    )

    # Validation.
    $text = $text.Replace(
'            sfxVolume = Mathf.Clamp01(sfxVolume);',
@'
            sfxVolume = Mathf.Clamp01(sfxVolume);
            gameplayMusicRotateSeconds = Mathf.Clamp(gameplayMusicRotateSeconds, 4f, 30f);
            gameplayMusicStartPitch = Mathf.Clamp(gameplayMusicStartPitch, 0.75f, 1.15f);
            gameplayMusicEndPitch = Mathf.Clamp(gameplayMusicEndPitch, gameplayMusicStartPitch, 1.35f);
'@
    )
    Write-Text $operatorSettings $text
}

# ---------------- Operator Menu ----------------
$text = Read-Text $operatorMenu
if (-not $text.Contains('Gameplay music rotation')) {
    $needle = '            AddFloatField("SFX volume (0-1)", () => editable.sfxVolume, value => editable.sfxVolume = value);'
    $replacement = @'
            AddFloatField("SFX volume (0-1)", () => editable.sfxVolume, value => editable.sfxVolume = value);
            AddToggleField("Gameplay music rotation", () => editable.gameplayMusicRotationEnabled, value => editable.gameplayMusicRotationEnabled = value);
            AddFloatField("Gameplay track rotate every (seconds)", () => editable.gameplayMusicRotateSeconds, value => editable.gameplayMusicRotateSeconds = value);
            AddFloatField("Gameplay music START pitch", () => editable.gameplayMusicStartPitch, value => editable.gameplayMusicStartPitch = value);
            AddFloatField("Gameplay music END pitch", () => editable.gameplayMusicEndPitch, value => editable.gameplayMusicEndPitch = value);
'@
    if (-not $text.Contains($needle)) { throw "Could not locate SFX volume row in OperatorMenuManager.cs" }
    $text = $text.Replace($needle, $replacement)
    Write-Text $operatorMenu $text
}

# ---------------- AudioManager ----------------
$text = Read-Text $audioManager
if (-not $text.Contains("StartGameplayMusicRotation")) {
    $text = $text.Replace(
'        private Coroutine crossfadeRoutine;',
@'
        private Coroutine crossfadeRoutine;
        private Coroutine gameplayRotationRoutine;
        private int gameplayRotationIndex = -1;
        private float musicPitch = 1f;
'@
    )

    $insertBeforeStopMusic = @'
        public void StartGameplayMusicRotation(float fadeSeconds = 0.35f)
        {
            StopGameplayMusicRotation();

            AudioClip[] playlist = GetGameplayPlaylist();
            OperatorSettings settings = settingsManager != null ? settingsManager.Current : null;
            bool rotationEnabled = settings == null || settings.gameplayMusicRotationEnabled;

            if (!rotationEnabled || playlist.Length <= 1)
            {
                PlayMusic(MusicCue.Gameplay, fadeSeconds);
                return;
            }

            gameplayRotationIndex = (gameplayRotationIndex + 1) % playlist.Length;
            PlayMusicClip(playlist[gameplayRotationIndex], fadeSeconds);
            gameplayRotationRoutine = StartCoroutine(GameplayMusicRotationRoutine(playlist));
        }

        public void StopGameplayMusicRotation()
        {
            if (gameplayRotationRoutine != null)
            {
                StopCoroutine(gameplayRotationRoutine);
                gameplayRotationRoutine = null;
            }
        }

        public void SetMusicPitch(float pitch)
        {
            musicPitch = Mathf.Clamp(pitch, 0.65f, 1.5f);
            EnsureSources();
            musicSourceA.pitch = musicPitch;
            musicSourceB.pitch = musicPitch;
        }

        private IEnumerator GameplayMusicRotationRoutine(AudioClip[] playlist)
        {
            while (playlist != null && playlist.Length > 1)
            {
                OperatorSettings settings = settingsManager != null ? settingsManager.Current : null;
                float waitSeconds = settings != null ? settings.gameplayMusicRotateSeconds : 8f;
                float until = Time.unscaledTime + Mathf.Clamp(waitSeconds, 4f, 30f);

                while (Time.unscaledTime < until)
                {
                    yield return null;
                }

                gameplayRotationIndex = (gameplayRotationIndex + 1) % playlist.Length;
                PlayMusicClip(playlist[gameplayRotationIndex], 0.45f);
            }

            gameplayRotationRoutine = null;
        }

        private AudioClip[] GetGameplayPlaylist()
        {
            List<AudioClip> clips = new List<AudioClip>(3);

            void AddUnique(AudioClip clip)
            {
                if (clip != null && !clips.Contains(clip))
                {
                    clips.Add(clip);
                }
            }

            if (config != null)
            {
                AddUnique(config.gameplayMusic);
                AddUnique(config.gameplayMusicAlt1);
                AddUnique(config.gameplayMusicAlt2);
            }

            if (clips.Count == 0)
            {
                AddUnique(GetMusicClip(MusicCue.Gameplay));
            }

            return clips.ToArray();
        }

        private void PlayMusicClip(AudioClip nextClip, float fadeSeconds)
        {
            EnsureSources();
            if (nextClip == null)
            {
                return;
            }

            if (activeMusicSource != null &&
                activeMusicSource.clip == nextClip &&
                activeMusicSource.isPlaying)
            {
                activeMusicSource.pitch = musicPitch;
                return;
            }

            if (crossfadeRoutine != null)
            {
                StopCoroutine(crossfadeRoutine);
            }

            crossfadeRoutine = StartCoroutine(Crossfade(nextClip, Mathf.Max(0.01f, fadeSeconds)));
        }

'@
    $needle = '        public void StopMusic(float fadeSeconds = 0.3f)'
    if (-not $text.Contains($needle)) { throw "Could not locate StopMusic() in AudioManager.cs" }
    $text = $text.Replace($needle, $insertBeforeStopMusic + $needle)

    # Make every crossfade carry the current dynamic pitch.
    $needle2 = (@'
            to.Stop();
            to.clip = nextClip;
            to.volume = 0f;
            to.Play();
'@).Replace("`r`n", "`n")
    $replacement2 = (@'
            to.Stop();
            to.clip = nextClip;
            to.volume = 0f;
            to.pitch = musicPitch;
            if (from != null)
            {
                from.pitch = musicPitch;
            }
            to.Play();
'@).Replace("`r`n", "`n")
    if (-not $text.Contains($needle2)) { throw "Could not locate Crossfade source setup in AudioManager.cs" }
    $text = $text.Replace($needle2, $replacement2)

    # Reset pitch on both ensured music sources.
    $needle3 = '            source.spatialBlend = 0f;'
    $replacement3 = @'
            source.spatialBlend = 0f;
            if (loop)
            {
                source.pitch = musicPitch;
            }
'@
    $text = $text.Replace($needle3, $replacement3)

    Write-Text $audioManager $text
}

# ---------------- GameManager ----------------
$text = Read-Text $gameManager
if (-not $text.Contains("gameplayMusicEndPitch")) {
    # Reset audio state on destruction.
    $oldOnDestroy = (@'
        private void OnDestroy()
        {
            UnsubscribeInput();
            UnsubscribeGameplayEvents();
            Time.timeScale = 1f;
        }
'@).Replace("`r`n", "`n")
    $newOnDestroy = (@'
        private void OnDestroy()
        {
            UnsubscribeInput();
            UnsubscribeGameplayEvents();
            Time.timeScale = 1f;
            GameServices.Audio?.StopGameplayMusicRotation();
            GameServices.Audio?.SetMusicPitch(1f);
        }
'@).Replace("`r`n", "`n")
    if ($text.Contains($oldOnDestroy)) {
        $text = $text.Replace($oldOnDestroy, $newOnDestroy)
    }

    # Start rotating gameplay playlist.
    $text = $text.Replace(
'            GameServices.Audio?.PlayMusic(MusicCue.Gameplay);',
'            GameServices.Audio?.StartGameplayMusicRotation();'
    )

    # Rush transition.
    $oldRush = '            GameServices.Audio?.PlayMusic(active ? MusicCue.Rush : MusicCue.Gameplay, 0.35f);'
    $newRush = @'
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
    $text = $text.Replace($oldRush, $newRush)

    # Golden start.
    $text = $text.Replace(
'            GameServices.Audio?.PlayMusic(MusicCue.GoldenRound, 0.25f);',
@'
            GameServices.Audio?.StopGameplayMusicRotation();
            GameServices.Audio?.PlayMusic(MusicCue.GoldenRound, 0.25f);
'@
    )

    # Golden end.
    $oldGoldenEnd = '            GameServices.Audio?.PlayMusic(rush ? MusicCue.Rush : MusicCue.Gameplay, 0.35f);'
    $newGoldenEnd = @'
            if (rush)
            {
                GameServices.Audio?.PlayMusic(MusicCue.Rush, 0.35f);
            }
            else
            {
                GameServices.Audio?.StartGameplayMusicRotation(0.35f);
            }
'@
    $text = $text.Replace($oldGoldenEnd, $newGoldenEnd)

    # Jackpot.
    $text = $text.Replace(
'            GameServices.Audio?.PlayMusic(MusicCue.Jackpot, 0.12f);',
@'
            GameServices.Audio?.StopGameplayMusicRotation();
            GameServices.Audio?.PlayMusic(MusicCue.Jackpot, 0.12f);
'@
    )

    # Results: restore natural pitch.
    $text = $text.Replace(
'            GameServices.Audio?.PlayMusic(MusicCue.Results, 0.4f);',
@'
            GameServices.Audio?.StopGameplayMusicRotation();
            GameServices.Audio?.SetMusicPitch(1f);
            GameServices.Audio?.PlayMusic(MusicCue.Results, 0.4f);
'@
    )

    # Add per-frame pitch tracking before SubscribeGameplayEvents.
    $marker = '        private void SubscribeGameplayEvents()'
    $updateBlock = @'
        private void Update()
        {
            if (!gameplayActive || ending || GameServices.Audio == null)
            {
                return;
            }

            float progress = difficultyManager != null
                ? difficultyManager.NormalizedProgress
                : (roundManager != null ? roundManager.NormalizedProgress : 0f);

            float startPitch = settings != null ? settings.gameplayMusicStartPitch : 0.96f;
            float endPitch = settings != null ? settings.gameplayMusicEndPitch : 1.18f;
            float pitch = Mathf.Lerp(startPitch, endPitch, Mathf.Clamp01(progress));

            // The final five-second Rush is already near the high end of the curve;
            // a tiny extra lift makes the audio match the visual/spawn intensity.
            if (roundManager != null && roundManager.IsRushMode)
            {
                pitch += 0.035f;
            }

            GameServices.Audio.SetMusicPitch(pitch);
        }


'@
    if (-not $text.Contains($marker)) { throw "Could not locate SubscribeGameplayEvents() in GameManager.cs" }
    $text = $text.Replace($marker, $updateBlock + $marker)

    Write-Text $gameManager $text
}

Write-Host ""
Write-Host "Balloon Rush v1.9.6 code patch applied." -ForegroundColor Green
Write-Host "Return to Unity, wait for compilation, then run:" -ForegroundColor Cyan
Write-Host "Tools > Balloon Rush > v1.9.6 - Install Gameplay Music Rotation" -ForegroundColor Cyan
