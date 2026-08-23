using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Effects
{
    public sealed class EffectsManager : MonoBehaviour
    {
        [SerializeField] private ScreenShake screenShake;
        [SerializeField] private FloatingTextPool floatingTextPool;
        [SerializeField] private ParticleSystem popParticles;
        [SerializeField] private ParticleSystem confettiParticles;
        [SerializeField] private ParticleSystem goldParticles;
        [SerializeField] private ParticleSystem bombParticles;

        private SettingsManager settingsManager;

        public void Configure(
            ScreenShake configuredShake,
            FloatingTextPool configuredTextPool,
            ParticleSystem configuredPop,
            ParticleSystem configuredConfetti,
            ParticleSystem configuredGold,
            ParticleSystem configuredBomb,
            SettingsManager settings)
        {
            screenShake = configuredShake;
            floatingTextPool = configuredTextPool;
            popParticles = configuredPop;
            confettiParticles = configuredConfetti;
            goldParticles = configuredGold;
            bombParticles = configuredBomb;
            settingsManager = settings;
            if (Application.isPlaying)
            {
                EnsureParticleSystems();
                floatingTextPool?.Initialize();
            }
        }

        public void ApplySettings(SettingsManager settings)
        {
            settingsManager = settings;
            EnsureParticleSystems();
            floatingTextPool?.Initialize();
        }

        public void PlaySuccessfulPop(Vector3 position, Color color, TimingRating rating, int ticketAward)
        {
            EnsureParticleSystems();
            Emit(popParticles, position, color, rating == TimingRating.Perfect ? 34 : 20);
            if (rating == TimingRating.Perfect)
            {
                Emit(confettiParticles, position, color, 18);
                screenShake?.Shake(0.08f, 0.12f);
            }
            else if (rating == TimingRating.Great)
            {
                screenShake?.Shake(0.04f, 0.08f);
            }

            string text = ticketAward > 0 ? $"{rating.ToString().ToUpper()}  +{ticketAward}" : rating.ToString().ToUpper();
            floatingTextPool?.Show(text, position + Vector3.up * 0.4f, rating == TimingRating.Perfect ? Color.yellow : Color.white);
        }

        public void PlayMiss(Vector3 position)
        {
            floatingTextPool?.Show("MISS!", position, new Color(1f, 0.22f, 0.25f));
        }

        public void PlayBomb(Vector3 position, bool superBomb)
        {
            EnsureParticleSystems();
            Emit(bombParticles, position, new Color(1f, 0.12f, 0.03f), superBomb ? 70 : 45);
            screenShake?.Shake(superBomb ? 0.35f : 0.22f, superBomb ? 0.5f : 0.32f);
            floatingTextPool?.Show(superBomb ? "SUPER BOMB!" : "BOMB!", position, Color.red, 0.95f, 1.35f);
        }

        public void PlayComboMilestone(int combo, Vector3 position)
        {
            EnsureParticleSystems();
            Emit(confettiParticles, position, new Color(1f, 0.25f, 0.9f), 35);
            screenShake?.Shake(0.10f, 0.18f);
            floatingTextPool?.Show($"COMBO x{combo}!", position, Color.yellow, 1.05f, 1.2f);
        }

        public void PlayGoldenBalloon(Vector3 position)
        {
            EnsureParticleSystems();
            Emit(goldParticles, position, new Color(1f, 0.78f, 0.08f), 55);
            screenShake?.Shake(0.12f, 0.22f);
            floatingTextPool?.Show("GOLDEN ROUND!", position, Color.yellow, 1.2f, 1.4f);
        }

        public void PlayJackpot(Vector3 position)
        {
            EnsureParticleSystems();
            Emit(goldParticles, position, Color.yellow, 140);
            Emit(confettiParticles, position, new Color(1f, 0.2f, 0.8f), 120);
            screenShake?.Shake(0.32f, 0.9f);
            floatingTextPool?.Show("JACKPOT!", position, Color.yellow, 1.8f, 1.8f);
        }

        private void EnsureParticleSystems()
        {
            if (popParticles == null) popParticles = CreateParticleSystem("Pop Particles", 0.35f, 3.2f, 0.08f);
            if (confettiParticles == null) confettiParticles = CreateParticleSystem("Confetti Particles", 0.8f, 4.5f, 0.10f);
            if (goldParticles == null) goldParticles = CreateParticleSystem("Gold Particles", 1.1f, 5.0f, 0.11f);
            if (bombParticles == null) bombParticles = CreateParticleSystem("Bomb Particles", 0.6f, 6.0f, 0.15f);
        }

        private ParticleSystem CreateParticleSystem(string objectName, float lifetime, float speed, float size)
        {
            GameObject particleObject = new GameObject(objectName);
            particleObject.transform.SetParent(transform, false);
            ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.maxParticles = 300;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            return system;
        }

        private static void Emit(ParticleSystem system, Vector3 position, Color color, int count)
        {
            if (system == null || count <= 0)
            {
                return;
            }

            system.transform.position = position;
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
            system.Emit(count);
        }
    }
}
