using BalloonRush.Core;
using BalloonRush.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Pink/green cabinet edge light animation.
    /// v1.9.7a keeps the flashes but greatly reduces geometric scaling so the
    /// rails do not visibly push into or distort the game UI.
    /// </summary>
    public sealed class CabinetEdgeLightControllerV197 : MonoBehaviour
    {
        private Image left;
        private Image right;
        private RectTransform leftRect;
        private RectTransform rightRect;
        private Shadow leftGlow;
        private Shadow rightGlow;
        private GameManager gameManager;

        private float nextLeftChange;
        private float nextRightChange;

        private float leftTarget = 0.5f;
        private float rightTarget = 0.6f;
        private float leftLevel = 0.5f;
        private float rightLevel = 0.6f;

        private readonly Color leftColor =
            new Color(1f, 0.02f, 0.54f, 1f);

        private readonly Color rightColor =
            new Color(0.05f, 1f, 0.32f, 1f);

        private void Start()
        {
            FindRails();

            if (SceneManager.GetActiveScene().name == "MainGame")
            {
                gameManager =
                    FindFirstObjectByType<GameManager>(
                        FindObjectsInactive.Include);
            }
        }

        private void OnDestroy()
        {
            ResetScale();
        }

        private void Update()
        {
            if (left == null || right == null)
            {
                FindRails();

                if (left == null || right == null)
                {
                    return;
                }
            }

            OperatorSettings settings =
                GameServices.Settings != null
                    ? GameServices.Settings.Current
                    : null;

            if (settings != null &&
                !settings.cabinetEdgeLightsEnabled)
            {
                Apply(0.30f, 0.30f);
                ResetScale();
                return;
            }

            if (SceneManager.GetActiveScene().name == "AttractMode")
            {
                UpdateAttract(settings);
            }
            else
            {
                UpdateGameplay(settings);
            }
        }

        private void FindRails()
        {
            Image[] images = FindObjectsByType<Image>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];

                if (image == null)
                {
                    continue;
                }

                if (image.gameObject.name == "Left Neon")
                {
                    left = image;
                }

                if (image.gameObject.name == "Right Neon")
                {
                    right = image;
                }
            }

            if (left != null)
            {
                leftRect = left.rectTransform;
                leftGlow = left.GetComponent<Shadow>();

                if (leftGlow == null)
                {
                    leftGlow =
                        left.gameObject.AddComponent<Shadow>();
                }

                leftGlow.effectDistance =
                    new Vector2(3.2f, 0f);

                leftGlow.useGraphicAlpha = true;
            }

            if (right != null)
            {
                rightRect = right.rectTransform;
                rightGlow = right.GetComponent<Shadow>();

                if (rightGlow == null)
                {
                    rightGlow =
                        right.gameObject.AddComponent<Shadow>();
                }

                rightGlow.effectDistance =
                    new Vector2(-3.2f, 0f);

                rightGlow.useGraphicAlpha = true;
            }
        }

        private void UpdateAttract(OperatorSettings settings)
        {
            float intensity =
                settings != null
                    ? settings.attractEdgeFlickerIntensity
                    : 0.85f;

            intensity = Mathf.Clamp01(intensity);
            float now = Time.unscaledTime;

            if (now >= nextLeftChange)
            {
                leftTarget =
                    Random.value < 0.18f
                        ? 1f
                        : Random.Range(0.20f, 0.94f);

                nextLeftChange =
                    now + Random.Range(0.14f, 0.62f);
            }

            if (now >= nextRightChange)
            {
                rightTarget =
                    Random.value < 0.18f
                        ? 1f
                        : Random.Range(0.20f, 0.94f);

                nextRightChange =
                    now + Random.Range(0.14f, 0.62f);
            }

            leftLevel = Mathf.Lerp(
                leftLevel,
                Mathf.Lerp(0.30f, leftTarget, intensity),
                Time.unscaledDeltaTime * 12f);

            rightLevel = Mathf.Lerp(
                rightLevel,
                Mathf.Lerp(0.30f, rightTarget, intensity),
                Time.unscaledDeltaTime * 12f);

            Apply(leftLevel, rightLevel);
        }

        private void UpdateGameplay(OperatorSettings settings)
        {
            if (gameManager == null)
            {
                gameManager =
                    FindFirstObjectByType<GameManager>(
                        FindObjectsInactive.Include);
            }

            float progress =
                gameManager != null &&
                gameManager.DifficultyManager != null
                    ? gameManager.DifficultyManager.NormalizedProgress
                    : 0f;

            float minHz =
                settings != null
                    ? settings.gameplayEdgePulseMinHz
                    : 1.35f;

            float maxHz =
                settings != null
                    ? settings.gameplayEdgePulseMaxHz
                    : 4.25f;

            float hz = Mathf.Lerp(
                Mathf.Clamp(minHz, 0.4f, 3f),
                Mathf.Clamp(maxHz, 1f, 5f),
                progress);

            float phase =
                Time.unscaledTime *
                Mathf.PI *
                2f *
                hz;

            // Opposing rail pulse gives the cabinet a chase-light feel.
            float leftPulse =
                0.38f +
                0.62f *
                (0.5f + 0.5f * Mathf.Sin(phase));

            float rightPulse =
                0.38f +
                0.62f *
                (0.5f + 0.5f *
                 Mathf.Sin(phase + Mathf.PI));

            if (gameManager != null &&
                gameManager.RoundManager != null &&
                gameManager.RoundManager.IsRushMode)
            {
                leftPulse =
                    Mathf.Clamp01(leftPulse + 0.15f);

                rightPulse =
                    Mathf.Clamp01(rightPulse + 0.15f);
            }

            if (gameManager != null &&
                gameManager.GoldenRoundManager != null &&
                gameManager.GoldenRoundManager.IsActive)
            {
                leftPulse =
                    Mathf.Clamp01(leftPulse + 0.22f);

                rightPulse =
                    Mathf.Clamp01(rightPulse + 0.22f);
            }

            Apply(leftPulse, rightPulse);
        }

        private void Apply(float leftAmount, float rightAmount)
        {
            if (left != null)
            {
                left.color = new Color(
                    leftColor.r,
                    leftColor.g,
                    leftColor.b,
                    Mathf.Lerp(0.30f, 1f, leftAmount));

                if (leftGlow != null)
                {
                    leftGlow.effectColor =
                        new Color(
                            leftColor.r,
                            leftColor.g,
                            leftColor.b,
                            Mathf.Lerp(0.10f, 0.82f, leftAmount));
                }

                if (leftRect != null)
                {
                    // v1.9.7 used 1.55x width which was visually distracting.
                    leftRect.localScale =
                        new Vector3(
                            Mathf.Lerp(1f, 1.10f, leftAmount),
                            1f,
                            1f);
                }
            }

            if (right != null)
            {
                right.color = new Color(
                    rightColor.r,
                    rightColor.g,
                    rightColor.b,
                    Mathf.Lerp(0.30f, 1f, rightAmount));

                if (rightGlow != null)
                {
                    rightGlow.effectColor =
                        new Color(
                            rightColor.r,
                            rightColor.g,
                            rightColor.b,
                            Mathf.Lerp(0.10f, 0.82f, rightAmount));
                }

                if (rightRect != null)
                {
                    rightRect.localScale =
                        new Vector3(
                            Mathf.Lerp(1f, 1.10f, rightAmount),
                            1f,
                            1f);
                }
            }
        }

        private void ResetScale()
        {
            if (leftRect != null)
            {
                leftRect.localScale = Vector3.one;
            }

            if (rightRect != null)
            {
                rightRect.localScale = Vector3.one;
            }
        }
    }
}
