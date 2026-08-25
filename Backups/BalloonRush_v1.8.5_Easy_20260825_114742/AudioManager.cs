using System.Collections;
using System.Collections.Generic;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Audio
{
    public enum AudioCue
    {
        BalloonPop,
        PerfectPop,
        GreatPop,
        GoodPop,
        Miss,
        BombExplosion,
        ButtonClick,
        LaneMove,
        ComboIncrease,
        ComboMilestone,
        GoldenBalloonAppear,
        GoldenBalloonPop,
        BonusStart,
        Countdown,
        GameOver,
        TicketCount,
        Jackpot
    }

    public enum MusicCue
    {
        None,
        Attract,
        Gameplay,
        Rush,
        GoldenRound,
        Jackpot,
        Results
    }

    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource jackpotSource;
        [SerializeField] private AudioSource voiceSource;

        private readonly Dictionary<AudioCue, AudioClip> fallbackClips = new Dictionary<AudioCue, AudioClip>();
        private readonly Dictionary<MusicCue, AudioClip> fallbackMusic = new Dictionary<MusicCue, AudioClip>();
        private AudioConfig config;
        private SettingsManager settingsManager;
        private AudioSource activeMusicSource;
        private Coroutine crossfadeRoutine;
        private Coroutine gameplayRotationRoutine;
        private int gameplayRotationIndex = -1;
        private float musicPitch = 1f;

        public void Initialize(AudioConfig audioConfig, SettingsManager settings)
        {
            config = audioConfig;
            settingsManager = settings;
            EnsureSources();
            BuildFallbackClips();

            if (settingsManager != null)
            {
                settingsManager.SettingsChanged -= HandleSettingsChanged;
                settingsManager.SettingsChanged += HandleSettingsChanged;
                HandleSettingsChanged(settingsManager.Current);
            }
        }

        private void OnDestroy()
        {
            if (settingsManager != null)
            {
                settingsManager.SettingsChanged -= HandleSettingsChanged;
            }
        }

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
        public void PlayMusic(MusicCue cue, float fadeSeconds = 0.5f)
        {
            EnsureSources();
            AudioClip nextClip = GetMusicClip(cue);
            if (nextClip == null)
            {
                StopMusic(fadeSeconds);
                return;
            }

            if (activeMusicSource != null && activeMusicSource.clip == nextClip && activeMusicSource.isPlaying)
            {
                return;
            }

            if (crossfadeRoutine != null)
            {
                StopCoroutine(crossfadeRoutine);
            }

            crossfadeRoutine = StartCoroutine(Crossfade(nextClip, Mathf.Max(0.01f, fadeSeconds)));
        }

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
        public void StopMusic(float fadeSeconds = 0.3f)
        {
            if (crossfadeRoutine != null)
            {
                StopCoroutine(crossfadeRoutine);
            }

            crossfadeRoutine = StartCoroutine(FadeOutAllMusic(Mathf.Max(0.01f, fadeSeconds)));
        }

        private void EnsureSources()
        {
            musicSourceA = EnsureSource(musicSourceA, "Music A", true);
            musicSourceB = EnsureSource(musicSourceB, "Music B", true);
            sfxSource = EnsureSource(sfxSource, "SFX", false);
            uiSource = EnsureSource(uiSource, "UI", false);
            jackpotSource = EnsureSource(jackpotSource, "Jackpot", false);
            voiceSource = EnsureSource(voiceSource, "Voice", false);
            if (activeMusicSource == null)
            {
                activeMusicSource = musicSourceA;
            }
        }

        private AudioSource EnsureSource(AudioSource source, string childName, bool loop)
        {
            if (source == null)
            {
                Transform child = transform.Find(childName);
                GameObject sourceObject = child != null ? child.gameObject : new GameObject(childName);
                if (sourceObject.transform.parent != transform)
                {
                    sourceObject.transform.SetParent(transform, false);
                }

                source = sourceObject.GetComponent<AudioSource>();
                if (source == null)
                {
                    source = sourceObject.AddComponent<AudioSource>();
                }
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            if (loop)
            {
                source.pitch = musicPitch;
            }
            return source;
        }

        private void HandleSettingsChanged(OperatorSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            AudioListener.volume = settings.masterVolume;
            EnsureSources();
            musicSourceA.volume = settings.musicVolume;
            musicSourceB.volume = settings.musicVolume;
            sfxSource.volume = settings.sfxVolume;
            uiSource.volume = settings.sfxVolume;
            jackpotSource.volume = settings.sfxVolume;
            voiceSource.volume = settings.sfxVolume;
        }

        private IEnumerator Crossfade(AudioClip nextClip, float duration)
        {
            AudioSource from = activeMusicSource ?? musicSourceA;
            AudioSource to = from == musicSourceA ? musicSourceB : musicSourceA;
            float targetVolume = settingsManager != null && settingsManager.Current != null
                ? settingsManager.Current.musicVolume
                : 0.65f;

            to.Stop();
            to.clip = nextClip;
            to.volume = 0f;
            to.pitch = musicPitch;
            if (from != null)
            {
                from.pitch = musicPitch;
            }
            to.Play();

            float startVolume = from != null ? from.volume : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (from != null)
                {
                    from.volume = Mathf.Lerp(startVolume, 0f, t);
                }
                to.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            if (from != null)
            {
                from.Stop();
                from.clip = null;
            }

            to.volume = targetVolume;
            activeMusicSource = to;
            crossfadeRoutine = null;
        }

        private IEnumerator FadeOutAllMusic(float duration)
        {
            float a = musicSourceA != null ? musicSourceA.volume : 0f;
            float b = musicSourceB != null ? musicSourceB.volume : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (musicSourceA != null) musicSourceA.volume = Mathf.Lerp(a, 0f, t);
                if (musicSourceB != null) musicSourceB.volume = Mathf.Lerp(b, 0f, t);
                yield return null;
            }

            if (musicSourceA != null) musicSourceA.Stop();
            if (musicSourceB != null) musicSourceB.Stop();
            crossfadeRoutine = null;
        }

        private AudioClip GetMusicClip(MusicCue cue)
        {
            AudioClip clip = null;
            if (config != null)
            {
                switch (cue)
                {
                    case MusicCue.Attract: clip = config.attractMusic; break;
                    case MusicCue.Gameplay: clip = config.gameplayMusic; break;
                    case MusicCue.Rush: clip = config.rushMusic; break;
                    case MusicCue.GoldenRound: clip = config.goldenRoundMusic; break;
                    case MusicCue.Jackpot: clip = config.jackpotMusic; break;
                    case MusicCue.Results: clip = config.resultsMusic; break;
                }
            }

            return clip != null ? clip : (fallbackMusic.TryGetValue(cue, out AudioClip fallback) ? fallback : null);
        }

        private AudioClip GetSfxClip(AudioCue cue)
        {
            AudioClip clip = null;
            if (config != null)
            {
                switch (cue)
                {
                    case AudioCue.BalloonPop: clip = config.balloonPop; break;
                    case AudioCue.PerfectPop: clip = config.perfectPop; break;
                    case AudioCue.GreatPop: clip = config.greatPop; break;
                    case AudioCue.GoodPop: clip = config.goodPop; break;
                    case AudioCue.Miss: clip = config.miss; break;
                    case AudioCue.BombExplosion: clip = config.bombExplosion; break;
                    case AudioCue.ButtonClick: clip = config.buttonClick; break;
                    case AudioCue.LaneMove: clip = config.laneMove; break;
                    case AudioCue.ComboIncrease: clip = config.comboIncrease; break;
                    case AudioCue.ComboMilestone: clip = config.comboMilestone; break;
                    case AudioCue.GoldenBalloonAppear: clip = config.goldenBalloonAppear; break;
                    case AudioCue.GoldenBalloonPop: clip = config.goldenBalloonPop; break;
                    case AudioCue.BonusStart: clip = config.bonusStart; break;
                    case AudioCue.Countdown: clip = config.countdown; break;
                    case AudioCue.GameOver: clip = config.gameOver; break;
                    case AudioCue.TicketCount: clip = config.ticketCount; break;
                    case AudioCue.Jackpot: clip = config.jackpot; break;
                }
            }

            return clip != null ? clip : (fallbackClips.TryGetValue(cue, out AudioClip fallback) ? fallback : null);
        }

        private void BuildFallbackClips()
        {
            if (fallbackClips.Count > 0)
            {
                return;
            }

            fallbackClips[AudioCue.BalloonPop] = CreateTone("Fallback Pop", 650f, 0.07f, 0.16f);
            fallbackClips[AudioCue.PerfectPop] = CreateTone("Fallback Perfect", 980f, 0.11f, 0.20f);
            fallbackClips[AudioCue.GreatPop] = CreateTone("Fallback Great", 820f, 0.09f, 0.18f);
            fallbackClips[AudioCue.GoodPop] = CreateTone("Fallback Good", 720f, 0.08f, 0.15f);
            fallbackClips[AudioCue.Miss] = CreateTone("Fallback Miss", 180f, 0.13f, 0.16f);
            fallbackClips[AudioCue.BombExplosion] = CreateNoise("Fallback Bomb", 0.25f, 0.26f);
            fallbackClips[AudioCue.ButtonClick] = CreateTone("Fallback Click", 520f, 0.04f, 0.10f);
            fallbackClips[AudioCue.LaneMove] = CreateTone("Fallback Lane", 420f, 0.05f, 0.11f);
            fallbackClips[AudioCue.ComboIncrease] = CreateTone("Fallback Combo", 760f, 0.06f, 0.12f);
            fallbackClips[AudioCue.ComboMilestone] = CreateTone("Fallback Milestone", 1120f, 0.16f, 0.18f);
            fallbackClips[AudioCue.GoldenBalloonAppear] = CreateTone("Fallback Golden Appear", 1280f, 0.20f, 0.20f);
            fallbackClips[AudioCue.GoldenBalloonPop] = CreateTone("Fallback Golden Pop", 1420f, 0.18f, 0.22f);
            fallbackClips[AudioCue.BonusStart] = CreateTone("Fallback Bonus", 1040f, 0.22f, 0.20f);
            fallbackClips[AudioCue.Countdown] = CreateTone("Fallback Countdown", 500f, 0.08f, 0.16f);
            fallbackClips[AudioCue.GameOver] = CreateTone("Fallback Game Over", 260f, 0.30f, 0.16f);
            fallbackClips[AudioCue.TicketCount] = CreateTone("Fallback Ticket", 880f, 0.035f, 0.10f);
            fallbackClips[AudioCue.Jackpot] = CreateTone("Fallback Jackpot", 1560f, 0.45f, 0.24f);

            fallbackMusic[MusicCue.Attract] = CreateMusicLoop("Fallback Attract Music", 220f, 0.055f);
            fallbackMusic[MusicCue.Gameplay] = CreateMusicLoop("Fallback Gameplay Music", 262f, 0.065f);
            fallbackMusic[MusicCue.Rush] = CreateMusicLoop("Fallback Rush Music", 330f, 0.070f);
            fallbackMusic[MusicCue.GoldenRound] = CreateMusicLoop("Fallback Golden Music", 392f, 0.070f);
            fallbackMusic[MusicCue.Jackpot] = CreateMusicLoop("Fallback Jackpot Music", 494f, 0.075f);
            fallbackMusic[MusicCue.Results] = CreateMusicLoop("Fallback Results Music", 294f, 0.055f);
        }

        private static AudioClip CreateMusicLoop(string name, float rootFrequency, float amplitude)
        {
            const int sampleRate = 22050;
            const float duration = 4f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float[] ratios = { 1f, 1.25f, 1.5f, 2f, 1.5f, 1.25f, 1.125f, 1.5f };
            float noteDuration = duration / ratios.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                int note = Mathf.Clamp(Mathf.FloorToInt(time / noteDuration), 0, ratios.Length - 1);
                float noteTime = time - note * noteDuration;
                float envelope = Mathf.Clamp01(noteTime / 0.025f) * Mathf.Clamp01((noteDuration - noteTime) / 0.06f);
                float frequency = rootFrequency * ratios[note];
                float lead = Mathf.Sin(2f * Mathf.PI * frequency * time);
                float harmony = Mathf.Sin(2f * Mathf.PI * frequency * 0.5f * time) * 0.35f;
                float pulse = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * 0.25f * time)) * 0.08f;
                samples[i] = (lead + harmony + pulse) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateTone(string name, float frequency, float duration, float amplitude)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoise(string name, float duration, float amplitude)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(173);
            for (int i = 0; i < sampleCount; i++)
            {
                float envelope = 1f - i / (float)sampleCount;
                samples[i] = ((float)random.NextDouble() * 2f - 1f) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
