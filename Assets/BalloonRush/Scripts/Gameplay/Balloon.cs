using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class Balloon : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private TextMeshPro iconText;
        [SerializeField] private LineRenderer stringRenderer;

        private BalloonManager manager;
        private Action<Balloon> releaseCallback;
        private float movementSpeed;
        private float despawnY;
        private bool moving;
        private float pulseOffset;
        private Vector3 baseScale = Vector3.one;

        public BalloonDefinition Definition { get; private set; }
        public int LaneIndex { get; private set; }
        public bool IsActiveBalloon { get; private set; }
        public float DistanceTo(float worldY) => Mathf.Abs(transform.position.y - worldY);

        public void ConfigureVisuals(
            Transform configuredVisualRoot,
            SpriteRenderer configuredBody,
            SpriteRenderer configuredGlow,
            TextMeshPro configuredIcon,
            LineRenderer configuredString)
        {
            visualRoot = configuredVisualRoot;
            bodyRenderer = configuredBody;
            glowRenderer = configuredGlow;
            iconText = configuredIcon;
            stringRenderer = configuredString;
        }

        public void Activate(
            BalloonManager owner,
            BalloonDefinition definition,
            int laneIndex,
            Vector3 spawnPosition,
            float speed,
            float configuredDespawnY,
            Action<Balloon> onReleased)
        {
            StopAllCoroutines();
            manager = owner;
            Definition = definition;
            LaneIndex = laneIndex;
            movementSpeed = speed;
            despawnY = configuredDespawnY;
            releaseCallback = onReleased;
            moving = true;
            IsActiveBalloon = true;
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            pulseOffset = UnityEngine.Random.value * 10f;
            gameObject.SetActive(true);
            EnsureVisuals();
            ApplyDefinitionVisuals();
        }

        public void PlayPopAnimation(TimingRating rating)
        {
            if (!IsActiveBalloon)
            {
                return;
            }

            moving = false;
            StartCoroutine(PopRoutine(rating));
        }

        public void ReleaseImmediately()
        {
            StopAllCoroutines();
            moving = false;
            IsActiveBalloon = false;
            manager = null;
            Definition = null;
            gameObject.SetActive(false);
            Action<Balloon> callback = releaseCallback;
            releaseCallback = null;
            callback?.Invoke(this);
        }

        private void Update()
        {
            if (!IsActiveBalloon)
            {
                return;
            }

            if (moving)
            {
                transform.position += Vector3.up * movementSpeed * Time.deltaTime;
                if (transform.position.y > despawnY)
                {
                    moving = false;
                    manager?.HandleBalloonPassed(this);
                    return;
                }
            }

            if (visualRoot != null && Definition != null)
            {
                bool special = Definition.Kind == BalloonKind.GoldenTrigger || Definition.Kind == BalloonKind.GoldenJackpot;
                float amount = special ? 0.08f : 0.025f;
                float pulse = 1f + Mathf.Sin(Time.time * (special ? 7f : 3f) + pulseOffset) * amount;
                visualRoot.localScale = baseScale * pulse;
            }
        }

        private IEnumerator PopRoutine(TimingRating rating)
        {
            float hitStop = rating == TimingRating.Perfect ? 0.045f : 0f;
            if (hitStop > 0f)
            {
                yield return new WaitForSecondsRealtime(hitStop);
            }

            float elapsed = 0f;
            const float expandDuration = 0.07f;
            while (elapsed < expandDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / expandDuration);
                if (visualRoot != null)
                {
                    visualRoot.localScale = Vector3.Lerp(baseScale, baseScale * 1.22f, t);
                }
                yield return null;
            }

            elapsed = 0f;
            const float collapseDuration = 0.10f;
            while (elapsed < collapseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / collapseDuration);
                if (visualRoot != null)
                {
                    visualRoot.localScale = Vector3.Lerp(baseScale * 1.22f, new Vector3(1.35f, 0.05f, 1f), t);
                }
                yield return null;
            }

            ReleaseImmediately();
        }

        private void EnsureVisuals()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SpriteRenderer>();
                if (bodyRenderer == null)
                {
                    bodyRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (bodyRenderer.sprite == null)
            {
                bodyRenderer.sprite = RuntimeSpriteLibrary.BalloonSprite;
            }

            baseScale = visualRoot.localScale == Vector3.zero ? Vector3.one : visualRoot.localScale;
        }

        private void ApplyDefinitionVisuals()
        {
            if (Definition == null)
            {
                return;
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = Definition.Sprite != null ? Definition.Sprite : RuntimeSpriteLibrary.BalloonSprite;
                bodyRenderer.color = Definition.VisualColor;
            }

            if (glowRenderer != null)
            {
                glowRenderer.sprite = RuntimeSpriteLibrary.RadialGlowSprite;
                Color glow = Definition.VisualColor;
                glow.a = Definition.Kind == BalloonKind.GoldenTrigger || Definition.Kind == BalloonKind.GoldenJackpot ? 0.75f : 0.22f;
                glowRenderer.color = glow;
            }

            if (iconText != null)
            {
                iconText.text = GetIconText(Definition.Kind);
                iconText.enableAutoSizing = true;
                iconText.fontSizeMin = 2.0f;
                iconText.fontSizeMax = Definition.Kind == BalloonKind.GoldenTrigger || Definition.Kind == BalloonKind.GoldenJackpot
                    ? 3.8f
                    : 5.5f;
                iconText.color = Definition.Kind == BalloonKind.GoldenTrigger || Definition.Kind == BalloonKind.GoldenJackpot
                    ? new Color(0.23f, 0.12f, 0.01f)
                    : Color.white;
            }

            if (stringRenderer != null)
            {
                stringRenderer.positionCount = 2;
                stringRenderer.SetPosition(0, new Vector3(0f, -0.62f, 0f));
                stringRenderer.SetPosition(1, new Vector3(0.05f, -1.15f, 0f));
                stringRenderer.startColor = new Color(1f, 1f, 1f, 0.75f);
                stringRenderer.endColor = new Color(1f, 1f, 1f, 0.15f);
            }
        }

        private static string GetIconText(BalloonKind kind)
        {
            switch (kind)
            {
                case BalloonKind.Green: return "+1";
                case BalloonKind.Blue: return "+5";
                case BalloonKind.Multiplier: return "x2";
                case BalloonKind.Mystery: return "?";
                case BalloonKind.Bomb: return "!";
                case BalloonKind.SuperBomb: return "!!";
                case BalloonKind.GoldenTrigger: return "GOLD";
                case BalloonKind.GoldenJackpot: return "JP";
                default: return string.Empty;
            }
        }
    }
}
