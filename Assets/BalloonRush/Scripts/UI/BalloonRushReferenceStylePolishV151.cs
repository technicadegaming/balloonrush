using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    [DefaultExecutionOrder(-70)]
    public class BalloonRushReferenceStylePolishV151 : MonoBehaviour
    {
        [Header("Gameplay Field")]
        [SerializeField, Range(1.00f, 1.30f)] private float gameplayFieldWidthScale = 1.18f;
        [SerializeField, Range(0.95f, 1.15f)] private float gameplayFieldHeightScale = 1.04f;
        [SerializeField, Range(1.00f, 1.40f)] private float hitZoneWidthScale = 1.14f;
        [SerializeField, Range(1.00f, 1.40f)] private float hitZoneHeightScale = 1.24f;

        [Header("Reference Style Colors")]
        [SerializeField] private Color darkPanel = new Color32(5, 15, 44, 248);
        [SerializeField] private Color cyan = new Color32(32, 221, 255, 255);
        [SerializeField] private Color magenta = new Color32(255, 43, 193, 255);
        [SerializeField] private Color gold = new Color32(255, 196, 33, 255);
        [SerializeField] private Color green = new Color32(63, 236, 84, 255);
        [SerializeField] private Color red = new Color32(255, 72, 72, 255);
        [SerializeField] private Color blue = new Color32(43, 136, 255, 255);
        [SerializeField] private Color purple = new Color32(161, 76, 255, 255);
        [SerializeField] private Color orange = new Color32(255, 147, 30, 255);

        private readonly Dictionary<string, Transform> cache = new Dictionary<string, Transform>();

        private Sprite darkPanelSprite;
        private Sprite cyanPanelSprite;
        private Sprite goldPanelSprite;
        private Sprite blueButtonSprite;
        private Sprite redButtonSprite;
        private Sprite greenButtonSprite;
        private Sprite payoutTileSprite;
        private Sprite laneSprite;
        private Sprite hitZoneSprite;

        private void Awake()
        {
            BuildSprites();
            CacheScene();
            ApplyCanvasStyle();
            ApplyGameplayFieldStyle();
        }

        private void BuildSprites()
        {
            darkPanelSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(darkPanel, cyan, 96, 24, 4, 22);
            cyanPanelSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(10, 66, 100, 240), cyan, 96, 28, 4, 24);
            goldPanelSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(104, 71, 14, 250), gold, 96, 24, 4, 22);
            blueButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(27, 108, 230, 255), cyan, 96, 30, 4, 26);
            redButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(212, 43, 36, 255), orange, 96, 30, 4, 26);
            greenButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(43, 182, 52, 255), cyan, 96, 30, 4, 26);
            payoutTileSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(18, 39, 75, 255), cyan, 96, 20, 4, 18);
            laneSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(Color.white, Color.white, 96, 34, 1, 26);
            hitZoneSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(Color.white, Color.white, 96, 40, 1, 30);
        }

        private void CacheScene()
        {
            cache.Clear();
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
            cache.TryGetValue(name, out Transform t);
            return t;
        }

        private RectTransform FindRect(string name)
        {
            return FindNamed(name) as RectTransform;
        }

        private void ApplyCanvasStyle()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            RectTransform topBar = FindRect("Top Bar");
            RectTransform timer = FindRect("Timer");
            RectTransform combo = FindRect("Combo Meter");
            RectTransform payout = FindRect("Payout Ladder");
            RectTransform controls = FindRect("Control Display");
            RectTransform goldenRoundBanner = FindRect("Golden Round Banner");

            Stretch(topBar, 0.015f, 0.905f, 0.985f, 0.992f);
            Stretch(combo, 0.018f, 0.162f, 0.145f, 0.895f);
            Stretch(payout, 0.855f, 0.162f, 0.982f, 0.895f);
            Stretch(controls, 0.018f, 0.010f, 0.982f, 0.168f);
            Stretch(timer, 0.41f, 0.846f, 0.59f, 0.912f);

            StylePanel(topBar, darkPanelSprite, 1f, cyan);
            StylePanel(combo, darkPanelSprite, 1f, magenta);
            StylePanel(payout, darkPanelSprite, 1f, purple);
            StylePanel(controls, darkPanelSprite, 1f, cyan);
            StylePanel(timer, goldPanelSprite, 1f, gold);

            if (goldenRoundBanner != null)
            {
                Stretch(goldenRoundBanner, 0.18f, 0.86f, 0.82f, 0.905f);
                StylePanel(goldenRoundBanner, goldPanelSprite, 1f, gold);
            }

            StyleTopBar(topBar);
            StylePayoutLadder(payout);
            StyleCombo(combo);
            StyleControls(controls);
            StyleLaneIndicators();
            StyleTexts(canvas);
        }

        private void ApplyGameplayFieldStyle()
        {
            Transform field = FindNamed("Gameplay Field");
            if (field == null)
                return;

            Vector3 scale = field.localScale;
            scale.x = gameplayFieldWidthScale;
            scale.y = gameplayFieldHeightScale;
            field.localScale = scale;

            RoundWorldCard("Field Backplate", new Color32(10, 26, 64, 245), 1.02f, 1.00f);
            RoundWorldCard("Field Core", new Color32(7, 23, 45, 160), 0.98f, 1.00f);
            TintWorld("Outer Field Glow", new Color32(30, 222, 255, 105));
            TintWorld("Field Inner Glow", new Color32(30, 222, 255, 42));
            TintWorld("Field Left Rail", new Color32(255, 46, 196, 230));
            TintWorld("Field Right Rail", new Color32(77, 242, 92, 230));

            RoundLane("Lane 1", new Color32(23, 95, 210, 170));
            RoundLane("Lane 2", new Color32(230, 170, 35, 165));
            RoundLane("Lane 3", new Color32(76, 66, 208, 170));
            RoundHitZone();
        }

        private void StyleTopBar(RectTransform topBar)
        {
            if (topBar == null)
                return;

            Image[] images = topBar.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                string lower = img.name.ToLowerInvariant();
                if (lower.Contains("ticket"))
                    ApplySprite(img, darkPanelSprite, cyan);
                else if (lower.Contains("jackpot"))
                    ApplySprite(img, goldPanelSprite, orange);
                else if (lower.Contains("score") || lower.Contains("high") || lower.Contains("top"))
                    ApplySprite(img, darkPanelSprite, cyan);
            }
        }

        private void StylePayoutLadder(RectTransform payout)
        {
            if (payout == null)
                return;

            TMP_Text[] texts = payout.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                RectTransform rt = text.GetComponentInParent<RectTransform>();
                if (rt == null || rt == payout)
                    continue;

                Image img = rt.GetComponent<Image>();
                if (img == null)
                    continue;

                ApplySprite(img, payoutTileSprite, cyan);
                string value = text.text.Trim();
                if (value == "500") img.color = MakeSolid(gold);
                else if (value == "250") img.color = MakeSolid(purple);
                else if (value == "100") img.color = MakeSolid(blue);
                else if (value == "50") img.color = MakeSolid(green);
                else if (value == "25") img.color = MakeSolid(orange);
                else if (value == "10") img.color = MakeSolid(new Color32(50, 205, 255, 255));
                else if (value == "5") img.color = MakeSolid(new Color32(226, 58, 122, 255));
                else if (value == "1") img.color = MakeSolid(new Color32(236, 190, 35, 255));

                AddOutline(img.gameObject, new Color(1f, 1f, 1f, 0.22f), 1.5f);
                text.fontStyle |= FontStyles.Bold;
            }
        }

        private void StyleCombo(RectTransform combo)
        {
            if (combo == null)
                return;

            Image[] images = combo.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img.transform == combo)
                    continue;

                string lower = img.name.ToLowerInvariant();
                if (lower.Contains("fill"))
                {
                    ApplySprite(img, payoutTileSprite, magenta);
                    img.color = MakeSolid(new Color32(255, 196, 33, 255));
                }
                else
                {
                    ApplySprite(img, payoutTileSprite, magenta);
                    img.color = new Color(0.05f, 0.09f, 0.22f, 0.95f);
                }
            }
        }

        private void StyleControls(RectTransform controls)
        {
            if (controls == null)
                return;

            Image[] images = controls.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                TMP_Text label = img.GetComponentInChildren<TMP_Text>(true);
                string txt = label != null ? label.text.ToUpperInvariant() : img.name.ToUpperInvariant();

                if (txt.Contains("LEFT"))
                    StyleButton(img, blueButtonSprite, cyan, 180f, 128f);
                else if (txt.Contains("POP"))
                    StyleButton(img, redButtonSprite, orange, 200f, 138f);
                else if (txt.Contains("RIGHT"))
                    StyleButton(img, greenButtonSprite, cyan, 180f, 128f);
            }
        }

        private void StyleLaneIndicators()
        {
            for (int i = 1; i <= 3; i++)
            {
                RectTransform rt = FindRect("Lane Indicator " + i);
                if (rt == null)
                    continue;

                Color border = i == 1 ? blue : (i == 2 ? gold : green);
                Sprite sprite = i == 2 ? goldPanelSprite : cyanPanelSprite;
                StylePanel(rt, sprite, 1f, border);
            }
        }

        private void StyleTexts(Canvas canvas)
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text == null)
                    continue;

                text.textWrappingMode = TextWrappingModes.NoWrap;
                string c = text.text.ToLowerInvariant();
                string n = text.name.ToLowerInvariant();

                if ((c.Contains("balloon") && c.Contains("rush")) || n.Contains("title"))
                {
                    text.fontStyle |= FontStyles.Bold;
                    text.fontSize = Mathf.Max(text.fontSize, 42f);
                    text.characterSpacing = 1.4f;
                    text.outlineWidth = Mathf.Max(text.outlineWidth, 0.16f);
                }
                else if (c.Contains("jackpot") || c.Contains("tickets"))
                {
                    text.fontStyle |= FontStyles.Bold;
                    text.outlineWidth = Mathf.Max(text.outlineWidth, 0.10f);
                }
                else if (c.Contains("pop") && !c.Contains("operator") && !c.Contains("debug"))
                {
                    text.fontStyle |= FontStyles.Bold;
                    text.fontSize = Mathf.Max(text.fontSize, 24f);
                }
                else if (c.Contains("golden") || c.Contains("round"))
                {
                    text.fontStyle |= FontStyles.Bold;
                    text.fontSize = Mathf.Max(text.fontSize, 18f);
                }
            }
        }

        private void StyleButton(Image img, Sprite sprite, Color glowColor, float minWidth, float minHeight)
        {
            ApplySprite(img, sprite, glowColor);
            RectTransform rt = img.rectTransform;
            Vector2 size = rt.sizeDelta;
            size.x = Mathf.Max(size.x, minWidth);
            size.y = Mathf.Max(size.y, minHeight);
            rt.sizeDelta = size;
            AddShadow(img.gameObject, 0.45f, 5f);
        }

        private void RoundLane(string objectName, Color color)
        {
            Transform t = FindNamed(objectName);
            if (t == null) return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            Vector2 size = GetRendererSize(sr);
            sr.sprite = laneSprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
        }

        private void RoundHitZone()
        {
            Transform t = FindNamed("Hit Zone");
            if (t == null) return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            Vector2 size = GetRendererSize(sr);
            size.x *= hitZoneWidthScale;
            size.y *= hitZoneHeightScale;
            sr.sprite = hitZoneSprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = new Color32(44, 207, 255, 118);
        }

        private void RoundWorldCard(string objectName, Color color, float widthScale, float heightScale)
        {
            Transform t = FindNamed(objectName);
            if (t == null) return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            Vector2 size = GetRendererSize(sr);
            size.x *= widthScale;
            size.y *= heightScale;
            sr.sprite = laneSprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
        }

        private void TintWorld(string objectName, Color color)
        {
            Transform t = FindNamed(objectName);
            if (t == null) return;
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;
        }

        private static Vector2 GetRendererSize(SpriteRenderer sr)
        {
            if (sr.drawMode != SpriteDrawMode.Simple)
                return sr.size;

            Bounds b = sr.bounds;
            Vector3 lossy = sr.transform.lossyScale;
            float width = Mathf.Abs(lossy.x) > 0.0001f ? b.size.x / Mathf.Abs(lossy.x) : b.size.x;
            float height = Mathf.Abs(lossy.y) > 0.0001f ? b.size.y / Mathf.Abs(lossy.y) : b.size.y;
            return new Vector2(width, height);
        }

        private static void StylePanel(RectTransform rt, Sprite sprite, float alpha, Color glowColor)
        {
            if (rt == null) return;

            Image img = rt.GetComponent<Image>();
            if (img == null)
                img = rt.gameObject.AddComponent<Image>();

            ApplySprite(img, sprite, glowColor);
            img.color = new Color(1f, 1f, 1f, alpha);
            AddShadow(rt.gameObject, 0.35f, 4f);
        }

        private static void ApplySprite(Image img, Sprite sprite, Color glowColor)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            AddOutline(img.gameObject, new Color(glowColor.r, glowColor.g, glowColor.b, 0.28f), 1.5f);
        }

        private static Color MakeSolid(Color color)
        {
            color.a = 1f;
            return color;
        }

        private static void AddShadow(GameObject go, float alpha, float distance)
        {
            Shadow shadow = go.GetComponent<Shadow>();
            if (shadow == null)
                shadow = go.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(distance, -distance);
        }

        private static void AddOutline(GameObject go, Color color, float distance)
        {
            Outline outline = go.GetComponent<Outline>();
            if (outline == null)
                outline = go.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private static void Stretch(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            if (rt == null) return;

            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
