using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    [DefaultExecutionOrder(-80)]
    public class BalloonRushArcadePolishV150 : MonoBehaviour
    {
        [Header("Gameplay Field")]
        [SerializeField, Range(1.00f, 1.30f)] private float gameplayFieldWidthScale = 1.16f;
        [SerializeField, Range(0.95f, 1.15f)] private float gameplayFieldHeightScale = 1.04f;
        [SerializeField, Range(1.00f, 1.30f)] private float hitZoneWidthScale = 1.06f;
        [SerializeField, Range(1.00f, 1.30f)] private float hitZoneHeightScale = 1.18f;

        [Header("Theme")]
        [SerializeField] private Color panelFill = new Color32(4, 22, 46, 245);
        [SerializeField] private Color panelBorder = new Color32(0, 230, 255, 255);
        [SerializeField] private Color accentFill = new Color32(12, 82, 112, 240);
        [SerializeField] private Color accentBorder = new Color32(91, 244, 255, 255);
        [SerializeField] private Color blueButton = new Color32(44, 129, 255, 255);
        [SerializeField] private Color redButton = new Color32(240, 57, 83, 255);
        [SerializeField] private Color greenButton = new Color32(30, 202, 88, 255);
        [SerializeField] private Color goldPanel = new Color32(242, 184, 28, 255);

        [Header("World Lane Colors")]
        [SerializeField] private Color laneColorLeft = new Color32(0, 44, 84, 150);
        [SerializeField] private Color laneColorCenter = new Color32(0, 104, 112, 165);
        [SerializeField] private Color laneColorRight = new Color32(0, 44, 84, 150);
        [SerializeField] private Color hitZoneColor = new Color32(0, 220, 255, 105);

        private readonly Dictionary<string, Transform> cache = new Dictionary<string, Transform>();

        private Sprite panelSprite;
        private Sprite accentSprite;
        private Sprite blueButtonSprite;
        private Sprite redButtonSprite;
        private Sprite greenButtonSprite;
        private Sprite goldSprite;
        private Sprite laneRoundedSprite;
        private Sprite hitZoneRoundedSprite;
        private Sprite slotSprite;

        private void Awake()
        {
            BuildSprites();
            CacheScene();
            ApplyCanvasPolish();
            ApplyGameplayFieldPolish();
        }

        private void BuildSprites()
        {
            panelSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(panelFill, panelBorder, 96, 24, 4, 22);
            accentSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(accentFill, accentBorder, 96, 26, 4, 24);
            blueButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(blueButton, Color.white, 96, 28, 4, 24);
            redButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(redButton, Color.white, 96, 28, 4, 24);
            greenButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(greenButton, Color.white, 96, 28, 4, 24);
            goldSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(goldPanel, Color.white, 96, 24, 4, 22);
            laneRoundedSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 1f), 96, 28, 1, 24);
            hitZoneRoundedSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 1f), 96, 30, 1, 26);
            slotSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 1f), 96, 20, 1, 20);
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

        private void ApplyCanvasPolish()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            RectTransform topBar = FindRect("Top Bar");
            RectTransform timer = FindRect("Timer");
            RectTransform combo = FindRect("Combo Meter");
            RectTransform payout = FindRect("Payout Ladder");
            RectTransform controls = FindRect("Control Display");

            Stretch(topBar, 0.012f, 0.905f, 0.988f, 0.992f);
            Stretch(combo, 0.018f, 0.165f, 0.142f, 0.895f);
            Stretch(payout, 0.858f, 0.165f, 0.982f, 0.895f);
            Stretch(controls, 0.018f, 0.010f, 0.982f, 0.165f);
            Stretch(timer, 0.41f, 0.846f, 0.59f, 0.91f);

            StylePanel(topBar, panelSprite, 0.98f);
            StylePanel(combo, panelSprite, 0.98f);
            StylePanel(payout, panelSprite, 0.98f);
            StylePanel(controls, panelSprite, 0.98f);
            StylePanel(timer, goldSprite, 1f);

            StyleTopBar(topBar);
            StyleLaneIndicators();
            StylePayoutSlots();
            StyleComboMeter();
            StyleControls(controls);
            StyleTextHierarchy(canvas);
        }

        private void ApplyGameplayFieldPolish()
        {
            Transform field = FindNamed("Gameplay Field");
            if (field == null)
                return;

            Vector3 fieldScale = field.localScale;
            fieldScale.x = gameplayFieldWidthScale;
            fieldScale.y = gameplayFieldHeightScale;
            field.localScale = fieldScale;

            RoundWorldCard("Field Backplate", panelFill, 1.02f, 1.00f);
            RoundWorldCard("Field Core", new Color32(0, 120, 138, 45), 0.98f, 1.00f);
            TintWorld("Outer Field Glow", new Color32(0, 240, 255, 110));
            TintWorld("Field Inner Glow", new Color32(0, 226, 255, 60));
            TintWorld("Field Left Rail", new Color32(255, 0, 170, 230));
            TintWorld("Field Right Rail", new Color32(0, 220, 255, 230));

            RoundLane("Lane 1", laneColorLeft);
            RoundLane("Lane 2", laneColorCenter);
            RoundLane("Lane 3", laneColorRight);
            RoundHitZone();
        }

        private void StyleTopBar(RectTransform topBar)
        {
            if (topBar == null)
                return;

            Image[] images = topBar.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                string lower = image.name.ToLowerInvariant();
                if (lower.Contains("jackpot"))
                    ApplyImageSprite(image, goldSprite, true);
                else if (lower.Contains("ticket") || lower.Contains("score") || lower.Contains("top"))
                    ApplyImageSprite(image, panelSprite, true);
            }
        }

        private void StyleLaneIndicators()
        {
            for (int i = 1; i <= 3; i++)
            {
                RectTransform rt = FindRect("Lane Indicator " + i);
                if (rt == null)
                    continue;

                StylePanel(rt, i == 2 ? goldSprite : accentSprite, 1f);
                AddShadow(rt.gameObject, 0.28f, 3f);
            }
        }

        private void StylePayoutSlots()
        {
            RectTransform payout = FindRect("Payout Ladder");
            if (payout == null)
                return;

            Image[] images = payout.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image.transform == payout)
                    continue;

                ApplyImageSprite(image, slotSprite, true);
                image.color = new Color(0.05f, 0.16f, 0.30f, 0.96f);
                AddOutline(image.gameObject, new Color(0f, 0.85f, 1f, 0.35f), 1.5f);
            }
        }

        private void StyleComboMeter()
        {
            RectTransform combo = FindRect("Combo Meter");
            if (combo == null)
                return;

            Image[] images = combo.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image.transform == combo)
                    continue;

                string lower = image.name.ToLowerInvariant();
                if (lower.Contains("fill"))
                {
                    ApplyImageSprite(image, accentSprite, true);
                    image.color = new Color32(0, 216, 255, 255);
                }
                else
                {
                    ApplyImageSprite(image, slotSprite, true);
                    image.color = new Color(0.04f, 0.12f, 0.22f, 0.94f);
                }
            }
        }

        private void StyleControls(RectTransform controls)
        {
            if (controls == null)
                return;

            Image[] images = controls.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                TMP_Text label = image.GetComponentInChildren<TMP_Text>(true);
                string txt = label != null ? label.text.ToUpperInvariant() : image.name.ToUpperInvariant();

                if (txt.Contains("LEFT"))
                    StyleButtonImage(image, blueButtonSprite);
                else if (txt.Contains("POP"))
                    StyleButtonImage(image, redButtonSprite, 1.06f);
                else if (txt.Contains("RIGHT"))
                    StyleButtonImage(image, greenButtonSprite);
            }
        }

        private void StyleTextHierarchy(Canvas canvas)
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
                    text.fontSize = Mathf.Max(text.fontSize, 40f);
                    text.characterSpacing = 1.2f;
                    text.outlineWidth = Mathf.Max(text.outlineWidth, 0.14f);
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
            }
        }

        private void StyleButtonImage(Image image, Sprite sprite, float heightScale = 1f)
        {
            ApplyImageSprite(image, sprite, true);
            RectTransform rt = image.rectTransform;
            Vector2 size = rt.sizeDelta;
            size.y = Mathf.Max(size.y * heightScale, 132f);
            size.x = Mathf.Max(size.x, 175f);
            rt.sizeDelta = size;
            AddShadow(image.gameObject, 0.42f, 5f);
        }

        private void RoundLane(string objectName, Color color)
        {
            Transform t = FindNamed(objectName);
            if (t == null)
                return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;

            Vector2 size = GetRendererLocalSize(sr);
            sr.sprite = laneRoundedSprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
        }

        private void RoundHitZone()
        {
            Transform t = FindNamed("Hit Zone");
            if (t == null)
                return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Vector2 size = GetRendererLocalSize(sr);
                size.x *= hitZoneWidthScale;
                size.y *= hitZoneHeightScale;
                sr.sprite = hitZoneRoundedSprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
                sr.color = hitZoneColor;
            }
        }

        private void RoundWorldCard(string objectName, Color color, float widthScale, float heightScale)
        {
            Transform t = FindNamed(objectName);
            if (t == null)
                return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;

            Vector2 size = GetRendererLocalSize(sr);
            size.x *= widthScale;
            size.y *= heightScale;
            sr.sprite = laneRoundedSprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
        }

        private void TintWorld(string objectName, Color color)
        {
            Transform t = FindNamed(objectName);
            if (t == null)
                return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = color;
        }

        private static Vector2 GetRendererLocalSize(SpriteRenderer sr)
        {
            if (sr == null)
                return Vector2.one;

            if (sr.drawMode != SpriteDrawMode.Simple)
                return sr.size;

            Bounds b = sr.bounds;
            Vector3 lossy = sr.transform.lossyScale;
            float width = Mathf.Abs(lossy.x) > 0.0001f ? b.size.x / Mathf.Abs(lossy.x) : b.size.x;
            float height = Mathf.Abs(lossy.y) > 0.0001f ? b.size.y / Mathf.Abs(lossy.y) : b.size.y;
            return new Vector2(width, height);
        }

        private static void ApplyImageSprite(Image image, Sprite sprite, bool sliced)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = false;
        }

        private static void StylePanel(RectTransform rt, Sprite sprite, float alpha)
        {
            if (rt == null)
                return;

            Image image = rt.GetComponent<Image>();
            if (image == null)
                image = rt.gameObject.AddComponent<Image>();

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, alpha);
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
