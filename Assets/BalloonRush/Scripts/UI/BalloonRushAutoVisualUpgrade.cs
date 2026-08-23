using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Automatic visual polish pass for the generated Balloon Rush MainGame scene.
    /// No Inspector references are required.
    /// Safe to attach to Gameplay Canvas or any object in the MainGame scene.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class BalloonRushAutoVisualUpgrade : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField, Range(0.90f, 1.30f)] private float gameplayFieldWidthScale = 1.14f;
        [SerializeField, Range(0.90f, 1.20f)] private float gameplayFieldHeightScale = 1.03f;

        [Header("Rounded UI")]
        [SerializeField] private Color panelFill = new Color32(3, 20, 43, 245);
        [SerializeField] private Color cyan = new Color32(0, 224, 255, 255);
        [SerializeField] private Color cyanSoft = new Color32(0, 100, 130, 245);
        [SerializeField] private Color blue = new Color32(35, 132, 255, 255);
        [SerializeField] private Color red = new Color32(230, 42, 75, 255);
        [SerializeField] private Color green = new Color32(27, 201, 90, 255);
        [SerializeField] private Color gold = new Color32(244, 181, 23, 255);

        private Sprite panelSprite;
        private Sprite accentSprite;
        private Sprite blueSprite;
        private Sprite redSprite;
        private Sprite greenSprite;
        private Sprite goldSprite;

        private readonly Dictionary<string, Transform> cache = new Dictionary<string, Transform>();

        private void Awake()
        {
            BuildSprites();
            CacheScene();
            UpgradeCanvas();
            UpgradeWorldField();
        }

        private void BuildSprites()
        {
            panelSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(panelFill, cyan, 96, 22, 4, 22);
            accentSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(cyanSoft, cyan, 96, 22, 4, 22);
            blueSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(blue, Color.white, 96, 28, 4, 24);
            redSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(red, Color.white, 96, 28, 4, 24);
            greenSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(green, Color.white, 96, 28, 4, 24);
            goldSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(gold, Color.white, 96, 24, 4, 22);
        }

        private void CacheScene()
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in all)
            {
                if (t == null || cache.ContainsKey(t.name))
                    continue;

                cache.Add(t.name, t);
            }
        }

        private Transform FindNamed(string name)
        {
            if (cache.TryGetValue(name, out Transform t))
                return t;

            return null;
        }

        private RectTransform FindRect(string name)
        {
            Transform t = FindNamed(name);
            return t != null ? t as RectTransform : null;
        }

        private void UpgradeCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            RectTransform topBar = FindRect("Top Bar");
            RectTransform timer = FindRect("Timer");
            RectTransform combo = FindRect("Combo Meter");
            RectTransform payout = FindRect("Payout Ladder");
            RectTransform controls = FindRect("Control Display");

            // Use nearly the complete 9:16 display.
            Stretch(topBar, 0.015f, 0.905f, 0.985f, 0.992f);
            Stretch(combo, 0.015f, 0.175f, 0.145f, 0.895f);
            Stretch(payout, 0.855f, 0.175f, 0.985f, 0.895f);
            Stretch(controls, 0.015f, 0.012f, 0.985f, 0.165f);

            if (timer != null)
            {
                timer.anchorMin = new Vector2(0.41f, 0.848f);
                timer.anchorMax = new Vector2(0.59f, 0.91f);
                timer.offsetMin = Vector2.zero;
                timer.offsetMax = Vector2.zero;
            }

            StylePanel(topBar, panelSprite, 0.95f);
            StylePanel(combo, panelSprite, 0.97f);
            StylePanel(payout, panelSprite, 0.97f);
            StylePanel(controls, panelSprite, 0.97f);
            StylePanel(timer, goldSprite, 1f);

            StyleButtons(controls);
            StyleLaneIndicators();
            StyleAllText(canvas);

            // Give the whole UI a little breathing room from the cabinet edge.
            if (canvasRect != null)
                canvasRect.localScale = Vector3.one;
        }

        private void UpgradeWorldField()
        {
            Transform gameplayField = FindNamed("Gameplay Field");
            if (gameplayField == null)
                return;

            // Widen the actual world-space play field. Children such as lanes,
            // rails, hit zone, spawn locations, etc. stay aligned together.
            Vector3 s = gameplayField.localScale;
            s.x = gameplayFieldWidthScale;
            s.y = gameplayFieldHeightScale;
            gameplayField.localScale = s;

            // Make rail/core visuals softer and less like square boxes where possible.
            SoftenSprite("Outer Field Glow", new Color32(0, 238, 255, 95));
            SoftenSprite("Field Backplate", new Color32(3, 16, 38, 235));
            SoftenSprite("Field Core", new Color32(0, 90, 110, 48));
            SoftenSprite("Field Inner Glow", new Color32(0, 226, 255, 55));
            SoftenSprite("Field Left Rail", new Color32(255, 0, 180, 220));
            SoftenSprite("Field Right Rail", new Color32(0, 220, 255, 220));

            // Lane backgrounds: lower opacity so balloons become the focus.
            SoftenSprite("Lane 1", new Color32(0, 38, 73, 145));
            SoftenSprite("Lane 2", new Color32(0, 82, 91, 150));
            SoftenSprite("Lane 3", new Color32(0, 38, 73, 145));

            // Hit Zone should be very obvious.
            Transform hit = FindNamed("Hit Zone");
            if (hit != null)
            {
                SpriteRenderer sr = hit.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = new Color32(0, 214, 255, 100);

                Vector3 hs = hit.localScale;
                hs.x *= 1.04f;
                hs.y *= 1.15f;
                hit.localScale = hs;
            }
        }

        private void StyleLaneIndicators()
        {
            for (int i = 1; i <= 3; i++)
            {
                RectTransform rt = FindRect("Lane Indicator " + i);
                if (rt == null)
                    continue;

                Image image = rt.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = i == 2 ? goldSprite : accentSprite;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                }

                AddShadow(rt.gameObject, 0.32f);
            }
        }

        private void StyleButtons(RectTransform controls)
        {
            if (controls == null)
                return;

            Button[] buttons = controls.GetComponentsInChildren<Button>(true);

            // Some generated controls are Images rather than Unity Button components.
            Image[] images = controls.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                TMP_Text label = image.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    continue;

                string txt = label.text.Trim().ToUpperInvariant();

                if (txt.Contains("LEFT"))
                    ApplyButton(image, blueSprite);
                else if (txt == "POP" || txt.Contains("POP"))
                    ApplyButton(image, redSprite);
                else if (txt.Contains("RIGHT"))
                    ApplyButton(image, greenSprite);
            }

            foreach (Button button in buttons)
            {
                Image image = button.GetComponent<Image>();
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (image == null || label == null)
                    continue;

                string txt = label.text.Trim().ToUpperInvariant();

                if (txt.Contains("LEFT"))
                    ApplyButton(image, blueSprite);
                else if (txt.Contains("POP"))
                    ApplyButton(image, redSprite);
                else if (txt.Contains("RIGHT"))
                    ApplyButton(image, greenSprite);
            }
        }

        private void ApplyButton(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            AddShadow(image.gameObject, 0.45f);

            RectTransform rt = image.rectTransform;
            if (rt != null)
            {
                Vector2 size = rt.sizeDelta;
                size.y = Mathf.Max(size.y, 132f);
                rt.sizeDelta = size;
            }
        }

        private void StyleAllText(Canvas canvas)
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text == null)
                    continue;

                text.textWrappingMode = TextWrappingModes.NoWrap;

                string n = text.name.ToLowerInvariant();
                string c = text.text.ToLowerInvariant();

                if (c.Contains("balloon") && c.Contains("rush"))
                {
                    text.fontStyle |= FontStyles.Bold;
                    text.fontSize = Mathf.Max(text.fontSize, 42f);
                    text.characterSpacing = 1.5f;
                    text.outlineWidth = Mathf.Max(text.outlineWidth, 0.12f);
                }
                else if (c.Contains("pop") && !c.Contains("operator"))
                {
                    text.fontStyle |= FontStyles.Bold;
                    text.fontSize = Mathf.Max(text.fontSize, 25f);
                }
                else if (c.Contains("jackpot") || c.Contains("tickets"))
                {
                    text.fontStyle |= FontStyles.Bold;
                    text.outlineWidth = Mathf.Max(text.outlineWidth, 0.08f);
                }
            }
        }

        private void StylePanel(RectTransform rt, Sprite sprite, float alpha)
        {
            if (rt == null)
                return;

            Image image = rt.GetComponent<Image>();
            if (image == null)
                image = rt.gameObject.AddComponent<Image>();

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, alpha);

            AddShadow(rt.gameObject, 0.35f);
        }

        private void AddShadow(GameObject go, float alpha)
        {
            Shadow shadow = go.GetComponent<Shadow>();
            if (shadow == null)
                shadow = go.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(4f, -4f);
        }

        private void SoftenSprite(string objectName, Color color)
        {
            Transform t = FindNamed(objectName);
            if (t == null)
                return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = color;
        }

        private static void Stretch(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
