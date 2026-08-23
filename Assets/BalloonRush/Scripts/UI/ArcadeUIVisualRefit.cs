using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    [DefaultExecutionOrder(-100)]
    public class ArcadeUIVisualRefit : MonoBehaviour
    {
        [Header("Optional explicit references")]
        [SerializeField] private RectTransform rootCanvasRect;
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform playArea;
        [SerializeField] private RectTransform leftPanel;
        [SerializeField] private RectTransform rightPanel;
        [SerializeField] private RectTransform bottomPanel;
        [SerializeField] private RectTransform hitZone;

        [Header("Theme")]
        [SerializeField] private Color panelFill = new Color32(6, 26, 52, 240);
        [SerializeField] private Color panelBorder = new Color32(0, 231, 255, 255);
        [SerializeField] private Color accentFill = new Color32(10, 74, 110, 255);
        [SerializeField] private Color accentBorder = new Color32(0, 255, 255, 255);
        [SerializeField] private Color blueButton = new Color32(48, 128, 255, 255);
        [SerializeField] private Color redButton = new Color32(255, 62, 62, 255);
        [SerializeField] private Color greenButton = new Color32(58, 225, 86, 255);
        [SerializeField] private Color yellowPanel = new Color32(235, 186, 30, 255);

        private Sprite panelSprite;
        private Sprite accentSprite;
        private Sprite blueButtonSprite;
        private Sprite redButtonSprite;
        private Sprite greenButtonSprite;
        private Sprite yellowSprite;

        private void Awake()
        {
            if (rootCanvasRect == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    rootCanvasRect = canvas.GetComponent<RectTransform>();
            }

            AutoFindCommonReferences();
            BuildSprites();
            ApplyFullScreenLayout();
            ApplyRoundedTheme();
            StyleTexts();
        }

        private void AutoFindCommonReferences()
        {
            if (topBar == null) topBar = FindByNameContains("top");
            if (playArea == null) playArea = FindByNameContains("play");
            if (leftPanel == null) leftPanel = FindByNameContains("combo");
            if (rightPanel == null) rightPanel = FindByNameContains("payout");
            if (bottomPanel == null) bottomPanel = FindByNameContains("control");
            if (hitZone == null) hitZone = FindByNameContains("hit");
        }

        private RectTransform FindByNameContains(string text)
        {
            RectTransform[] all = GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rt in all)
            {
                if (rt.name.ToLowerInvariant().Contains(text.ToLowerInvariant()))
                    return rt;
            }

            return null;
        }

        private void BuildSprites()
        {
            panelSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(panelFill, panelBorder, 64, 16, 4, 18);
            accentSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(accentFill, accentBorder, 64, 16, 4, 18);
            blueButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(blueButton, Color.white, 64, 18, 4, 18);
            redButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(redButton, Color.white, 64, 18, 4, 18);
            greenButtonSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(greenButton, Color.white, 64, 18, 4, 18);
            yellowSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(yellowPanel, Color.white, 64, 18, 4, 18);
        }

        private void ApplyFullScreenLayout()
        {
            if (topBar != null)
                SetAnchors(topBar, 0.02f, 0.91f, 0.98f, 0.995f);

            if (leftPanel != null)
                SetAnchors(leftPanel, 0.02f, 0.18f, 0.145f, 0.895f);

            if (rightPanel != null)
                SetAnchors(rightPanel, 0.855f, 0.18f, 0.98f, 0.895f);

            if (playArea != null)
                SetAnchors(playArea, 0.15f, 0.18f, 0.85f, 0.895f);

            if (bottomPanel != null)
                SetAnchors(bottomPanel, 0.02f, 0.015f, 0.98f, 0.165f);

            if (hitZone != null)
                SetAnchors(hitZone, 0.20f, 0.49f, 0.80f, 0.575f);
        }

        private static void SetAnchors(
            RectTransform rt,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private void ApplyRoundedTheme()
        {
            Image[] images = GetComponentsInChildren<Image>(true);

            foreach (Image img in images)
            {
                string n = img.name.ToLowerInvariant();

                if (n.Contains("leftbutton") || n.Contains("left button"))
                {
                    ApplySliced(img, blueButtonSprite);
                    AddOutline(img.gameObject, new Color(1f, 1f, 1f, 0.35f));
                }
                else if (n.Contains("rightbutton") || n.Contains("right button"))
                {
                    ApplySliced(img, greenButtonSprite);
                    AddOutline(img.gameObject, new Color(1f, 1f, 1f, 0.35f));
                }
                else if (n.Contains("popbutton") || n.Contains("pop button") || n.Contains("startbutton"))
                {
                    ApplySliced(img, redButtonSprite);
                    AddOutline(img.gameObject, new Color(1f, 1f, 1f, 0.35f));
                }
                else if (n.Contains("jackpot") || n.Contains("lane") || n.Contains("timer"))
                {
                    ApplySliced(img, yellowSprite);
                }
                else if (n.Contains("hitzone") || n.Contains("hit zone"))
                {
                    ApplySliced(img, accentSprite);
                    img.color = new Color(1f, 1f, 1f, 0.85f);
                }
                else if (n.Contains("play") || n.Contains("combo") || n.Contains("payout") ||
                         n.Contains("control") || n.Contains("top"))
                {
                    ApplySliced(img, panelSprite);
                }
            }

            StyleButtonsByText();
        }

        private void StyleButtonsByText()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            foreach (Button btn in buttons)
            {
                Image img = btn.GetComponent<Image>();
                if (img == null)
                    continue;

                TMP_Text txt = btn.GetComponentInChildren<TMP_Text>(true);
                string label = txt != null ? txt.text.ToLowerInvariant() : btn.name.ToLowerInvariant();

                if (label.Contains("left"))
                {
                    ApplySliced(img, blueButtonSprite);
                    EnsureMinimumButtonSize(btn.GetComponent<RectTransform>(), 180f, 120f);
                }
                else if (label.Contains("pop"))
                {
                    ApplySliced(img, redButtonSprite);
                    EnsureMinimumButtonSize(btn.GetComponent<RectTransform>(), 205f, 132f);
                }
                else if (label.Contains("right"))
                {
                    ApplySliced(img, greenButtonSprite);
                    EnsureMinimumButtonSize(btn.GetComponent<RectTransform>(), 180f, 120f);
                }
            }
        }

        private static void EnsureMinimumButtonSize(RectTransform rt, float minWidth, float minHeight)
        {
            if (rt == null)
                return;

            Vector2 size = rt.sizeDelta;
            size.x = Mathf.Max(size.x, minWidth);
            size.y = Mathf.Max(size.y, minHeight);
            rt.sizeDelta = size;
        }

        private static void ApplySliced(Image img, Sprite sprite)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            img.color = Color.white;
        }

        private static void AddOutline(GameObject go, Color color)
        {
            Outline outline = go.GetComponent<Outline>();
            if (outline == null)
                outline = go.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void StyleTexts()
        {
            TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (TMP_Text t in texts)
            {
                if (t == null || t.canvas == null)
                    continue;

                string n = t.name.ToLowerInvariant();
                string content = t.text.ToLowerInvariant();

                t.textWrappingMode = TextWrappingModes.NoWrap;

                if (content.Contains("pop! balloon rush") || n.Contains("title"))
                {
                    t.fontSize = Mathf.Max(t.fontSize, 48f);
                    t.characterSpacing = 2f;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.15f);
                }
                else if (content.Contains("tickets") || content.Contains("jackpot") || content.Contains("score"))
                {
                    t.fontSize = Mathf.Max(t.fontSize, 22f);
                }
                else if (content.Contains("pop"))
                {
                    t.fontSize = Mathf.Max(t.fontSize, 28f);
                    t.fontStyle |= FontStyles.Bold;
                }
                else
                {
                    t.fontSize = Mathf.Max(t.fontSize, 16f);
                }
            }
        }
    }
}
