using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// v1.9.7a fixes the destructive v1.9.7 visual pass.
    ///
    /// IMPORTANT:
    /// - Never replaces an existing styled sprite.
    /// - Only applies the generated rounded sprite to simple solid-color Images
    ///   that have no source sprite and are already visibly colored.
    /// - Existing cards, Hit Zone graphics, buttons and headers keep their
    ///   original artwork/colors.
    /// </summary>
    [DefaultExecutionOrder(1400)]
    public sealed class ArcadeVisualPolishV197 : MonoBehaviour
    {
        private static Sprite roundedSprite;
        private const string AttractScene = "AttractMode";
        private const string MainScene = "MainGame";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != AttractScene && scene.name != MainScene)
            {
                return;
            }

            if (FindFirstObjectByType<ArcadeVisualPolishV197>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject host = new GameObject("Balloon Rush v1.9.7a Safe Visual Polish");
            host.AddComponent<ArcadeVisualPolishV197>();

            if (FindFirstObjectByType<CabinetEdgeLightControllerV197>(FindObjectsInactive.Include) == null)
            {
                host.AddComponent<CabinetEdgeLightControllerV197>();
            }
        }

        private void Start()
        {
            Invoke(nameof(Apply), 0.05f);
            Invoke(nameof(Apply), 0.35f);
            Invoke(nameof(Apply), 1.0f);
        }

        private void Apply()
        {
            RemoveOperatorHint();
            PolishLogo();
            PolishExistingUiWithoutReplacingSprites();
            RoundOnlySafeSolidColorImages();
        }

        private static void RemoveOperatorHint()
        {
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                string value = text.text;

                if (value.IndexOf("M OPERATOR", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    value = value.Replace("   M OPERATOR", string.Empty);
                    value = value.Replace("  M OPERATOR", string.Empty);
                    value = value.Replace(" M OPERATOR", string.Empty);
                    value = value.Replace("M OPERATOR", string.Empty);
                }

                if (value.IndexOf("M = OPERATOR", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    value = value.Replace("M = OPERATOR MENU", string.Empty);
                    value = value.Replace("M = OPERATOR", string.Empty);
                }

                value = value.Replace("LEFT/RIGHT SELECT", "LEFT / RIGHT MOVE");
                value = value.Replace("UP/SPACE POPS", "UP / SPACE POP");

                // Clean doubled separators/spaces left after removing service text.
                while (value.Contains("    "))
                {
                    value = value.Replace("    ", "   ");
                }

                text.text = value.Trim(' ', '|');
            }
        }

        private static void PolishLogo()
        {
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                if (text.text.IndexOf("BALLOON", StringComparison.OrdinalIgnoreCase) < 0 ||
                    text.text.IndexOf("RUSH", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                Shadow shadow = text.GetComponent<Shadow>();
                if (shadow == null)
                {
                    shadow = text.gameObject.AddComponent<Shadow>();
                }

                shadow.effectColor = new Color(0f, 0.75f, 1f, 0.25f);
                shadow.effectDistance = new Vector2(2.2f, -2.2f);
                shadow.useGraphicAlpha = true;
            }
        }

        private static void PolishExistingUiWithoutReplacingSprites()
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

                string n = image.gameObject.name ?? string.Empty;

                if (ShouldIgnore(n))
                {
                    continue;
                }

                bool important =
                    Contains(n, "Button") ||
                    Contains(n, "Panel") ||
                    Contains(n, "Card") ||
                    Contains(n, "Top Bar") ||
                    Contains(n, "Timer") ||
                    Contains(n, "Display") ||
                    Contains(n, "Hit Zone") ||
                    Contains(n, "Lane Indicator") ||
                    Contains(n, "Payout") ||
                    Contains(n, "Combo");

                if (!important)
                {
                    continue;
                }

                // Preserve the image's existing sprite, material and color.
                // Only add a small shadow/glow to improve depth.
                Shadow shadow = image.GetComponent<Shadow>();
                if (shadow == null)
                {
                    shadow = image.gameObject.AddComponent<Shadow>();
                }

                Color baseColor = image.color;
                Color glow = ChooseAccent(baseColor);
                glow.a = 0.20f;

                shadow.effectColor = glow;
                shadow.effectDistance = new Vector2(1.6f, -1.6f);
                shadow.useGraphicAlpha = true;
            }
        }

        private static void RoundOnlySafeSolidColorImages()
        {
            Image[] images = FindObjectsByType<Image>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.sprite != null)
                {
                    // This is the core hotfix:
                    // if an Image already has a sprite, DO NOT replace it.
                    continue;
                }

                string n = image.gameObject.name ?? string.Empty;
                if (ShouldIgnore(n))
                {
                    continue;
                }

                Color c = image.color;

                // White/near-white Images commonly relied on their old source sprite
                // for their visual appearance. Never turn those into white cards.
                bool nearWhite = c.r > 0.88f && c.g > 0.88f && c.b > 0.88f;
                bool nearlyInvisible = c.a < 0.08f;
                if (nearWhite || nearlyInvisible)
                {
                    continue;
                }

                bool safeTarget =
                    Contains(n, "Lane Indicator") ||
                    Contains(n, "Hit Zone") ||
                    Contains(n, "Button") ||
                    Contains(n, "Badge") ||
                    Contains(n, "Pill");

                if (!safeTarget)
                {
                    continue;
                }

                if (roundedSprite == null)
                {
                    roundedSprite = BuildRoundedSprite();
                }

                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;
            }
        }

        private static bool ShouldIgnore(string name)
        {
            return Contains(name, "Neon") ||
                   Contains(name, "Balloon") ||
                   Contains(name, "Overlay") ||
                   Contains(name, "Glow") ||
                   Contains(name, "Background") ||
                   Contains(name, "Field") ||
                   Contains(name, "Lane 1") ||
                   Contains(name, "Lane 2") ||
                   Contains(name, "Lane 3");
        }

        private static bool Contains(string value, string token)
        {
            return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Color ChooseAccent(Color baseColor)
        {
            if (baseColor.r > baseColor.g * 1.25f && baseColor.r > baseColor.b * 1.15f)
            {
                return new Color(1f, 0.20f, 0.48f, 1f);
            }

            if (baseColor.g > baseColor.r * 1.25f && baseColor.g > baseColor.b * 1.05f)
            {
                return new Color(0.05f, 1f, 0.38f, 1f);
            }

            if (baseColor.b > baseColor.r || baseColor.g > baseColor.r)
            {
                return new Color(0.05f, 0.88f, 1f, 1f);
            }

            return new Color(1f, 0.72f, 0.08f, 1f);
        }

        private static Sprite BuildRoundedSprite()
        {
            const int size = 96;
            const float radius = 27f;

            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);

            texture.name = "BR197a Safe Rounded Solid UI";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2(
                (size - 1) * 0.5f,
                (size - 1) * 0.5f);

            float half = (size - 1) * 0.5f;
            Vector2 inner = new Vector2(
                half - radius,
                half - radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(
                        Mathf.Abs(x - center.x),
                        Mathf.Abs(y - center.y));

                    Vector2 q = new Vector2(
                        Mathf.Max(p.x - inner.x, 0f),
                        Mathf.Max(p.y - inner.y, 0f));

                    float distance = q.magnitude - radius;
                    byte alpha = (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(1f - distance) * 255f);

                    pixels[y * size + x] =
                        new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(31, 31, 31, 31));
        }
    }
}
