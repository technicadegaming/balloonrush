using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Single visual system for MainGame.
    ///
    /// Goals:
    /// - one clean HUD instead of stacked legacy visual passes
    /// - preserve gameplay/scoring/ticket/hardware code
    /// - keep the generated scene wiring intact
    /// - style the world playfield without changing gameplay positions
    /// - provide readable cabinet-scale feedback and controls
    ///
    /// The installer removes the older ArcadeUIVisualRefit / AutoVisualUpgrade /
    /// ArcadePolish / ReferenceStyle components so only this controller runs.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public sealed class BalloonRushMainGameVisualRebuild : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField, Range(1.00f, 1.30f)] private float gameplayFieldWidthScale = 1.16f;
        [SerializeField, Range(0.95f, 1.12f)] private float gameplayFieldHeightScale = 1.02f;
        [SerializeField, Range(0.025f, 0.080f)] private float hitZoneHalfHeight = 0.048f;

        [Header("Theme")]
        [SerializeField] private Color deepNavy = new Color32(2, 9, 28, 255);
        [SerializeField] private Color panelNavy = new Color32(4, 22, 52, 250);
        [SerializeField] private Color cyan = new Color32(0, 226, 255, 255);
        [SerializeField] private Color magenta = new Color32(255, 35, 183, 255);
        [SerializeField] private Color blue = new Color32(41, 134, 255, 255);
        [SerializeField] private Color red = new Color32(239, 54, 72, 255);
        [SerializeField] private Color green = new Color32(45, 224, 91, 255);
        [SerializeField] private Color gold = new Color32(255, 193, 28, 255);
        [SerializeField] private Color purple = new Color32(165, 76, 255, 255);
        [SerializeField] private Color orange = new Color32(255, 123, 27, 255);

        private readonly Dictionary<string, Transform> byName =
            new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

        private Canvas canvas;
        private RectTransform root;
        private Camera gameCamera;

        private ScoreManager scoreManager;
        private ComboManager comboManager;
        private LaneManager laneManager;
        private RoundManager roundManager;
        private GoldenRoundManager goldenRoundManager;
        private UIManager legacyUIManager;

        private TMP_Text ticketsValue;
        private TMP_Text scoreValue;
        private TMP_Text jackpotValue;
        private TMP_Text timerValue;
        private TMP_Text comboValue;
        private TMP_Text comboSubtext;
        private TMP_Text multiplierValue;
        private TMP_Text ratingValue;
        private TMP_Text messageValue;
        private TMP_Text countdownValue;
        private TMP_Text goldenValue;

        private Image comboFill;
        private Image hitZonePanel;
        private Image[] laneTabs = new Image[3];
        private TMP_Text[] laneTabLabels = new TMP_Text[3];

        private Transform worldHitZone;
        private float hitZoneViewportY = 0.58f;
        private float nextBalloonScan;
        private Coroutine ratingRoutine;
        private Coroutine jackpotRoutine;

        private Sprite roundedPanel;
        private Sprite roundedPanelMagenta;
        private Sprite roundedPanelGold;
        private Sprite roundedPanelPurple;
        private Sprite roundedPanelGreen;
        private Sprite roundedTile;
        private Sprite roundedButtonBlue;
        private Sprite roundedButtonRed;
        private Sprite roundedButtonGreen;
        private Sprite roundedButtonFace;
        private Sprite roundedHitZone;
        private Sprite roundedLane;

        private static readonly int[] PayoutValues = { 500, 250, 100, 50, 25, 10, 5, 1 };

        private void Awake()
        {
            canvas = FindGameplayCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("Balloon Rush UI: Gameplay Canvas not found. Unified visual pass skipped.");
                enabled = false;
                return;
            }

            gameCamera = Camera.main;
            BuildSpriteLibrary();
            CacheScene();
            RemoveStaleRuntimeVisualObjects();
            HideLegacyHudPanels();
            StyleWorldPlayfield();
            BuildUnifiedHud();
            RestyleLegacyTransientFeedback();
        }

        private void Start()
        {
            BindGameplaySystems();
            RefreshAll();
        }

        private void Update()
        {
            AnimateHitZone();

            if (Time.unscaledTime >= nextBalloonScan)
            {
                nextBalloonScan = Time.unscaledTime + 0.40f;
                PolishActiveBalloons();
            }
        }

        private void OnDestroy()
        {
            UnbindGameplaySystems();
        }

        #region Setup

        private Canvas FindGameplayCanvas()
        {
            Canvas own = GetComponent<Canvas>();
            if (own != null)
                return own;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas item in canvases)
            {
                if (item != null && item.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) >= 0)
                    return item;
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private void CacheScene()
        {
            byName.Clear();
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in all)
            {
                if (t == null || byName.ContainsKey(t.name))
                    continue;
                byName.Add(t.name, t);
            }

            worldHitZone = FindNamed("Hit Zone");
            if (worldHitZone != null && gameCamera != null)
            {
                Vector3 viewport = gameCamera.WorldToViewportPoint(worldHitZone.position);
                if (viewport.z > 0f && viewport.y > 0.10f && viewport.y < 0.90f)
                    hitZoneViewportY = viewport.y;
            }
        }

        private Transform FindNamed(string objectName)
        {
            byName.TryGetValue(objectName, out Transform value);
            return value;
        }

        private RectTransform FindRect(string objectName)
        {
            return FindNamed(objectName) as RectTransform;
        }

        private void BuildSpriteLibrary()
        {
            roundedPanel = RoundedSpriteFactory.CreateRoundedPanelSprite(panelNavy, cyan, 128, 28, 5, 28);
            roundedPanelMagenta = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(27, 11, 53, 250), magenta, 128, 28, 5, 28);
            roundedPanelGold = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(77, 48, 2, 250), gold, 128, 28, 5, 28);
            roundedPanelPurple = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(28, 14, 63, 250), purple, 128, 28, 5, 28);
            roundedPanelGreen = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(7, 57, 45, 250), green, 128, 28, 5, 28);
            roundedTile = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(11, 33, 70, 250), cyan, 96, 24, 4, 24);
            roundedButtonBlue = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(24, 105, 230, 255), cyan, 128, 40, 5, 34);
            roundedButtonRed = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(204, 35, 40, 255), orange, 128, 40, 5, 34);
            roundedButtonGreen = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(24, 185, 72, 255), cyan, 128, 40, 5, 34);
            roundedButtonFace = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(10, 18, 42, 255), Color.white, 128, 58, 5, 48);
            roundedHitZone = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(0, 124, 160, 18), cyan, 128, 48, 6, 38);
            roundedLane = RoundedSpriteFactory.CreateRoundedPanelSprite(Color.white, Color.white, 128, 40, 1, 34);
        }

        private void RemoveStaleRuntimeVisualObjects()
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child == null)
                    continue;

                string n = child.name;
                if (n.StartsWith("V15_", StringComparison.OrdinalIgnoreCase) ||
                    n.StartsWith("BRUI_", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("BalloonRushUnifiedHUD", StringComparison.OrdinalIgnoreCase))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void HideLegacyHudPanels()
        {
            string[] panelNames =
            {
                "Top Bar",
                "Timer",
                "Combo Meter",
                "Payout Ladder",
                "Control Display",
                "Lane Indicator 1",
                "Lane Indicator 2",
                "Lane Indicator 3"
            };

            foreach (string panelName in panelNames)
            {
                RectTransform rt = FindRect(panelName);
                if (rt == null)
                    continue;

                CanvasGroup group = rt.GetComponent<CanvasGroup>();
                if (group == null)
                    group = rt.gameObject.AddComponent<CanvasGroup>();

                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        #endregion

        #region HUD Construction

        private void BuildUnifiedHud()
        {
            GameObject rootObject = new GameObject("BalloonRushUnifiedHUD", typeof(RectTransform), typeof(CanvasGroup));
            root = rootObject.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            SetAnchors(root, Vector2.zero, Vector2.one);
            root.SetAsLastSibling();

            CanvasGroup rootGroup = rootObject.GetComponent<CanvasGroup>();
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;

            CreateSolid("BRUI_LeftEdge", root, new Vector2(0.003f, 0f), new Vector2(0.012f, 1f), magenta);
            CreateSolid("BRUI_RightEdge", root, new Vector2(0.988f, 0f), new Vector2(0.997f, 1f), cyan);

            BuildBackdropDecor();
            BuildHeader();
            BuildComboRail();
            BuildPayoutRail();
            BuildLaneTabs();
            BuildHitZone();
            BuildControlDeck();
            BuildTransientFeedback();
        }

        private void BuildBackdropDecor()
        {
            // Lightweight decorative rays and stars. Pure presentation: no gameplay interaction.
            for (int i = 0; i < 12; i++)
            {
                float x = 0.08f + i * 0.075f;
                Color rayColor = i % 3 == 0
                    ? new Color(magenta.r, magenta.g, magenta.b, 0.035f)
                    : new Color(cyan.r, cyan.g, cyan.b, 0.028f);
                RectTransform ray = CreatePanel(
                    "BRUI_BackRay_" + i,
                    root,
                    new Vector2(x, 0.19f),
                    new Vector2(Mathf.Min(0.98f, x + 0.012f), 0.88f),
                    roundedTile,
                    rayColor,
                    0f);
                ray.localRotation = Quaternion.Euler(0f, 0f, (i - 5.5f) * 1.8f);
                ray.SetAsFirstSibling();
            }

            for (int i = 0; i < 18; i++)
            {
                float x = 0.16f + ((i * 37) % 68) / 100f;
                float y = 0.23f + ((i * 53) % 54) / 100f;
                float size = 0.006f + (i % 3) * 0.002f;
                Color starColor = i % 2 == 0
                    ? new Color(0.35f, 0.90f, 1f, 0.20f)
                    : new Color(1f, 0.75f, 0.18f, 0.16f);
                RectTransform star = CreatePanel(
                    "BRUI_Star_" + i,
                    root,
                    new Vector2(x, y),
                    new Vector2(x + size, y + size),
                    roundedTile,
                    starColor,
                    0f);
                star.SetAsFirstSibling();
            }
        }

        private void BuildHeader()
        {
            RectTransform header = CreatePanel(
                "BRUI_Header",
                root,
                new Vector2(0.016f, 0.895f),
                new Vector2(0.984f, 0.992f),
                roundedPanel,
                Color.white,
                0.45f);

            RectTransform ticketsCard = CreatePanel(
                "BRUI_TicketsCard",
                header,
                new Vector2(0.015f, 0.12f),
                new Vector2(0.205f, 0.88f),
                roundedPanel,
                Color.white,
                0.30f);

            RectTransform titleCard = CreatePanel(
                "BRUI_TitleCard",
                header,
                new Vector2(0.218f, 0.08f),
                new Vector2(0.782f, 0.92f),
                roundedPanelPurple,
                Color.white,
                0.34f);

            RectTransform jackpotCard = CreatePanel(
                "BRUI_JackpotCard",
                header,
                new Vector2(0.795f, 0.12f),
                new Vector2(0.985f, 0.88f),
                roundedPanelGold,
                Color.white,
                0.36f);

            ticketsValue = CreateText(
                "BRUI_Tickets",
                ticketsCard,
                "TICKETS\n0",
                33f,
                TextAlignmentOptions.Center,
                Color.white,
                true);

            TMP_Text title = CreateText(
                "BRUI_Title",
                titleCard,
                "<b><color=#FFFFFF>BALLOON</color> <color=#FF3D59>RUSH</color></b>",
                62f,
                TextAlignmentOptions.Center,
                Color.white,
                true);
            title.richText = true;
            title.characterSpacing = 1.2f;
            SetAnchors(title.rectTransform, new Vector2(0.03f, 0.24f), new Vector2(0.97f, 0.95f));

            scoreValue = CreateText(
                "BRUI_Score",
                titleCard,
                "SCORE 0",
                18f,
                TextAlignmentOptions.Center,
                cyan,
                true);
            SetAnchors(scoreValue.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.30f));

            jackpotValue = CreateText(
                "BRUI_Jackpot",
                jackpotCard,
                "JACKPOT\n500 TICKETS",
                27f,
                TextAlignmentOptions.Center,
                Color.white,
                true);

            RectTransform timerCard = CreatePanel(
                "BRUI_TimerCard",
                root,
                new Vector2(0.405f, 0.828f),
                new Vector2(0.595f, 0.898f),
                roundedPanelGold,
                Color.white,
                0.36f);

            timerValue = CreateText(
                "BRUI_Timer",
                timerCard,
                "TIME\n30",
                32f,
                TextAlignmentOptions.Center,
                Color.white,
                true);
        }

        private void BuildComboRail()
        {
            RectTransform panel = CreatePanel(
                "BRUI_ComboRail",
                root,
                new Vector2(0.018f, 0.205f),
                new Vector2(0.155f, 0.825f),
                roundedPanelMagenta,
                Color.white,
                0.34f);

            comboValue = CreateText(
                "BRUI_ComboValue",
                panel,
                "COMBO\nx0",
                28f,
                TextAlignmentOptions.Center,
                Color.white,
                true);
            SetAnchors(comboValue.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.98f));

            RectTransform meterTrack = CreatePanel(
                "BRUI_ComboTrack",
                panel,
                new Vector2(0.32f, 0.18f),
                new Vector2(0.68f, 0.79f),
                roundedTile,
                new Color(1f, 1f, 1f, 0.86f),
                0.15f);

            GameObject fillObject = new GameObject("BRUI_ComboFill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.SetParent(meterTrack, false);
            fillRect.anchorMin = new Vector2(0.18f, 0.03f);
            fillRect.anchorMax = new Vector2(0.82f, 0.03f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            comboFill = fillObject.GetComponent<Image>();
            comboFill.sprite = roundedPanelGold;
            comboFill.type = Image.Type.Sliced;
            comboFill.color = gold;
            comboFill.raycastTarget = false;

            comboSubtext = CreateText(
                "BRUI_ComboSubtext",
                panel,
                "KEEP IT\nGOING!",
                16f,
                TextAlignmentOptions.Center,
                gold,
                true);
            SetAnchors(comboSubtext.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.17f));
        }

        private void BuildPayoutRail()
        {
            RectTransform panel = CreatePanel(
                "BRUI_PayoutRail",
                root,
                new Vector2(0.845f, 0.205f),
                new Vector2(0.982f, 0.825f),
                roundedPanelPurple,
                Color.white,
                0.34f);

            TMP_Text title = CreateText(
                "BRUI_PayoutTitle",
                panel,
                "PAYOUT",
                19f,
                TextAlignmentOptions.Center,
                gold,
                true);
            SetAnchors(title.rectTransform, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f));

            float top = 0.895f;
            float bottom = 0.055f;
            float gap = 0.012f;
            float usable = top - bottom - gap * (PayoutValues.Length - 1);
            float height = usable / PayoutValues.Length;

            for (int i = 0; i < PayoutValues.Length; i++)
            {
                float maxY = top - i * (height + gap);
                float minY = maxY - height;
                int value = PayoutValues[i];

                Sprite tileSprite = value == 500 ? roundedPanelGold : roundedTile;
                Color tileColor = GetPayoutColor(value);
                RectTransform tile = CreatePanel(
                    "BRUI_Payout_" + value,
                    panel,
                    new Vector2(0.08f, minY),
                    new Vector2(0.92f, maxY),
                    tileSprite,
                    tileColor,
                    0.16f);

                TMP_Text label = CreateText(
                    "BRUI_PayoutLabel_" + value,
                    tile,
                    value.ToString(),
                    value == 500 ? 23f : 18f,
                    TextAlignmentOptions.Center,
                    Color.white,
                    true);
                label.outlineWidth = 0.12f;
            }
        }

        private void BuildLaneTabs()
        {
            float[] centers = { 0.285f, 0.500f, 0.715f };
            for (int i = 0; i < 3; i++)
            {
                RectTransform tab = CreatePanel(
                    "BRUI_LaneTab_" + (i + 1),
                    root,
                    new Vector2(centers[i] - 0.095f, 0.785f),
                    new Vector2(centers[i] + 0.095f, 0.823f),
                    i == 1 ? roundedPanelGold : roundedTile,
                    Color.white,
                    0.25f);

                laneTabs[i] = tab.GetComponent<Image>();
                laneTabLabels[i] = CreateText(
                    "BRUI_LaneLabel_" + (i + 1),
                    tab,
                    "LANE " + (i + 1),
                    15f,
                    TextAlignmentOptions.Center,
                    Color.white,
                    true);
            }
        }

        private void BuildHitZone()
        {
            float minY = Mathf.Clamp(hitZoneViewportY - hitZoneHalfHeight, 0.30f, 0.72f);
            float maxY = Mathf.Clamp(hitZoneViewportY + hitZoneHalfHeight, minY + 0.05f, 0.78f);

            RectTransform panel = CreatePanel(
                "BRUI_HitZone",
                root,
                new Vector2(0.145f, minY),
                new Vector2(0.855f, maxY),
                roundedHitZone,
                Color.white,
                0.18f);

            hitZonePanel = panel.GetComponent<Image>();

            TMP_Text left = CreateText(
                "BRUI_HitLeft",
                panel,
                ">>>",
                26f,
                TextAlignmentOptions.Left,
                cyan,
                true);
            SetAnchors(left.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.25f, 1f));

            TMP_Text label = CreateText(
                "BRUI_HitLabel",
                panel,
                "HIT ZONE  -  POP NOW!",
                27f,
                TextAlignmentOptions.Center,
                Color.white,
                true);
            label.characterSpacing = 2.1f;
            SetAnchors(label.rectTransform, new Vector2(0.22f, 0f), new Vector2(0.78f, 1f));

            TMP_Text right = CreateText(
                "BRUI_HitRight",
                panel,
                "<<<",
                26f,
                TextAlignmentOptions.Right,
                cyan,
                true);
            SetAnchors(right.rectTransform, new Vector2(0.75f, 0f), new Vector2(0.98f, 1f));
        }

        private void BuildControlDeck()
        {
            RectTransform deck = CreatePanel(
                "BRUI_ControlDeck",
                root,
                new Vector2(0.018f, 0.012f),
                new Vector2(0.982f, 0.190f),
                roundedPanel,
                Color.white,
                0.42f);

            TMP_Text caption = CreateText(
                "BRUI_ControlCaption",
                deck,
                "CABINET CONTROLS",
                13f,
                TextAlignmentOptions.Center,
                cyan,
                true);
            SetAnchors(caption.rectTransform, new Vector2(0.20f, 0.84f), new Vector2(0.80f, 0.98f));

            CreateControlCard(deck, "LEFT", "<", "LEFT ARROW / A", new Vector2(0.045f, 0.08f), new Vector2(0.305f, 0.84f), roundedButtonBlue);
            CreateControlCard(deck, "POP", "POP!", "UP ARROW / SPACE", new Vector2(0.365f, 0.03f), new Vector2(0.635f, 0.88f), roundedButtonRed);
            CreateControlCard(deck, "RIGHT", ">", "RIGHT ARROW / D", new Vector2(0.695f, 0.08f), new Vector2(0.955f, 0.84f), roundedButtonGreen);

            TMP_Text service = CreateText(
                "BRUI_ServiceHint",
                deck,
                "M = OPERATOR     ESC = SERVICE / DEBUG",
                11f,
                TextAlignmentOptions.Center,
                new Color(0.72f, 0.90f, 1f, 0.85f),
                false);
            SetAnchors(service.rectTransform, new Vector2(0.20f, 0.00f), new Vector2(0.80f, 0.11f));
        }

        private void CreateControlCard(Transform parent, string label, string main, string hint, Vector2 min, Vector2 max, Sprite sprite)
        {
            RectTransform card = CreatePanel("BRUI_Control_" + label, parent, min, max, sprite, Color.white, 0.52f);

            RectTransform face = CreatePanel("BRUI_ControlFace_" + label, card, new Vector2(0.18f, 0.24f), new Vector2(0.82f, 0.92f), roundedButtonFace, Color.white, 0.35f);
            Image faceImage = face.GetComponent<Image>();
            faceImage.color = label == "LEFT" ? blue : (label == "RIGHT" ? green : red);

            TMP_Text mainText = CreateText("BRUI_ControlMain_" + label, face, main, label == "POP" ? 32f : 44f, TextAlignmentOptions.Center, Color.white, true);
            SetAnchors(mainText.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.90f));

            TMP_Text labelText = CreateText("BRUI_ControlLabel_" + label, card, label, 18f, TextAlignmentOptions.Center, Color.white, true);
            SetAnchors(labelText.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.26f));

            TMP_Text hintText = CreateText("BRUI_ControlHint_" + label, card, hint, 9f, TextAlignmentOptions.Center, new Color(0.86f, 0.94f, 1f, 0.90f), false);
            SetAnchors(hintText.rectTransform, new Vector2(0.03f, 0.00f), new Vector2(0.97f, 0.10f));
        }

        private void BuildTransientFeedback()
        {
            RectTransform ratingPlate = CreatePanel(
                "BRUI_RatingPlate",
                root,
                new Vector2(0.265f, 0.405f),
                new Vector2(0.735f, 0.465f),
                roundedPanelPurple,
                new Color(1f, 1f, 1f, 0f),
                0f);

            ratingValue = CreateText(
                "BRUI_Rating",
                ratingPlate,
                string.Empty,
                42f,
                TextAlignmentOptions.Center,
                Color.white,
                true);
            ratingValue.gameObject.SetActive(false);

            RectTransform messagePlate = CreatePanel(
                "BRUI_MessagePlate",
                root,
                new Vector2(0.315f, 0.705f),
                new Vector2(0.685f, 0.745f),
                roundedPanel,
                new Color(1f, 1f, 1f, 0f),
                0f);

            messageValue = CreateText(
                "BRUI_Message",
                messagePlate,
                string.Empty,
                20f,
                TextAlignmentOptions.Center,
                gold,
                true);
            messageValue.gameObject.SetActive(false);

            multiplierValue = CreateText(
                "BRUI_Multiplier",
                root,
                string.Empty,
                22f,
                TextAlignmentOptions.Center,
                purple,
                true);
            SetAnchors(multiplierValue.rectTransform, new Vector2(0.365f, 0.752f), new Vector2(0.635f, 0.790f));
            multiplierValue.gameObject.SetActive(false);

            countdownValue = CreateText(
                "BRUI_Countdown",
                root,
                string.Empty,
                105f,
                TextAlignmentOptions.Center,
                Color.white,
                true);
            SetAnchors(countdownValue.rectTransform, new Vector2(0.20f, 0.38f), new Vector2(0.80f, 0.62f));
            countdownValue.gameObject.SetActive(false);

            goldenValue = CreateText(
                "BRUI_Golden",
                root,
                string.Empty,
                23f,
                TextAlignmentOptions.Center,
                gold,
                true);
            SetAnchors(goldenValue.rectTransform, new Vector2(0.18f, 0.742f), new Vector2(0.82f, 0.782f));
            goldenValue.gameObject.SetActive(false);
        }

        #endregion

        #region Gameplay Binding

        private void BindGameplaySystems()
        {
            scoreManager = FindFirstObjectByType<ScoreManager>(FindObjectsInactive.Include);
            comboManager = FindFirstObjectByType<ComboManager>(FindObjectsInactive.Include);
            laneManager = FindFirstObjectByType<LaneManager>(FindObjectsInactive.Include);
            roundManager = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
            goldenRoundManager = FindFirstObjectByType<GoldenRoundManager>(FindObjectsInactive.Include);
            legacyUIManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);

            GameEvents.ScoreChanged += HandleScoreChanged;
            GameEvents.TicketsChanged += HandleTicketsChanged;
            GameEvents.ComboChanged += HandleComboChanged;
            GameEvents.TimingJudged += HandleTimingJudged;
            GameEvents.GoldenRoundStarted += HandleGoldenRoundStarted;
            GameEvents.GoldenRoundEnded += HandleGoldenRoundEnded;
            GameEvents.JackpotWon += HandleJackpotWon;

            if (laneManager != null)
                laneManager.SelectedLaneChanged += HandleLaneChanged;

            if (roundManager != null)
            {
                roundManager.TimeChanged += HandleTimeChanged;
                roundManager.RushModeChanged += HandleRushModeChanged;
            }

            if (scoreManager != null)
                scoreManager.PayoutMultiplierChanged += HandlePayoutMultiplierChanged;

            if (goldenRoundManager != null)
                goldenRoundManager.TimeChanged += HandleGoldenTimeChanged;
        }

        private void UnbindGameplaySystems()
        {
            GameEvents.ScoreChanged -= HandleScoreChanged;
            GameEvents.TicketsChanged -= HandleTicketsChanged;
            GameEvents.ComboChanged -= HandleComboChanged;
            GameEvents.TimingJudged -= HandleTimingJudged;
            GameEvents.GoldenRoundStarted -= HandleGoldenRoundStarted;
            GameEvents.GoldenRoundEnded -= HandleGoldenRoundEnded;
            GameEvents.JackpotWon -= HandleJackpotWon;

            if (laneManager != null)
                laneManager.SelectedLaneChanged -= HandleLaneChanged;

            if (roundManager != null)
            {
                roundManager.TimeChanged -= HandleTimeChanged;
                roundManager.RushModeChanged -= HandleRushModeChanged;
            }

            if (scoreManager != null)
                scoreManager.PayoutMultiplierChanged -= HandlePayoutMultiplierChanged;

            if (goldenRoundManager != null)
                goldenRoundManager.TimeChanged -= HandleGoldenTimeChanged;
        }

        private void RefreshAll()
        {
            HandleTicketsChanged(scoreManager != null ? scoreManager.Tickets : 0);
            HandleScoreChanged(scoreManager != null ? scoreManager.Score : 0);
            HandleComboChanged(comboManager != null ? comboManager.CurrentCombo : 0);
            HandleLaneChanged(laneManager != null ? laneManager.SelectedLane : 1);
            HandleTimeChanged(roundManager != null && roundManager.RemainingTime > 0f ? roundManager.RemainingTime : 30f);

            int jackpot = 500;
            OperatorSettings settings = GameServices.Settings != null ? GameServices.Settings.Current : null;
            if (settings != null)
                jackpot = Mathf.Clamp(settings.jackpotTickets, 1, 500);

            jackpotValue.text = "JACKPOT\n" + jackpot + " TICKETS";

            if (scoreManager != null)
                HandlePayoutMultiplierChanged(scoreManager.ActivePayoutMultiplier, scoreManager.PayoutMultiplierRemaining);
        }

        private void HandleTicketsChanged(int tickets)
        {
            if (ticketsValue != null)
                ticketsValue.text = "TICKETS\n" + Mathf.Max(0, tickets);
        }

        private void HandleScoreChanged(int score)
        {
            if (scoreValue != null)
                scoreValue.text = "SCORE  " + Mathf.Max(0, score).ToString("N0");
        }

        private void HandleComboChanged(int combo)
        {
            int safeCombo = Mathf.Max(0, combo);
            if (comboValue != null)
                comboValue.text = "COMBO\nx" + safeCombo;

            float normalized = Mathf.Clamp01(safeCombo / 30f);
            if (comboFill != null)
            {
                RectTransform rt = comboFill.rectTransform;
                rt.anchorMax = new Vector2(rt.anchorMax.x, 0.03f + 0.94f * normalized);
                comboFill.color = Color.Lerp(blue, gold, normalized);
            }

            if (comboSubtext != null)
            {
                comboSubtext.text = safeCombo >= 20 ? "AMAZING!" : safeCombo >= 10 ? "ON FIRE!" : safeCombo >= 5 ? "KEEP IT\nGOING!" : "BUILD YOUR\nCOMBO!";
                comboSubtext.color = safeCombo >= 20 ? gold : safeCombo >= 10 ? orange : new Color(1f, 0.82f, 0.20f);
                comboValue.transform.localScale = safeCombo >= 10 ? Vector3.one * 1.10f : safeCombo >= 5 ? Vector3.one * 1.05f : Vector3.one;
            }
        }

        private void HandleLaneChanged(int selectedLane)
        {
            int selected = Mathf.Clamp(selectedLane, 0, 2);
            for (int i = 0; i < laneTabs.Length; i++)
            {
                if (laneTabs[i] == null)
                    continue;

                bool active = i == selected;
                laneTabs[i].sprite = active ? roundedPanelGold : roundedTile;
                laneTabs[i].color = active ? Color.white : new Color(1f, 1f, 1f, 0.78f);
                laneTabs[i].transform.localScale = active ? Vector3.one * 1.07f : Vector3.one;

                if (laneTabLabels[i] != null)
                    laneTabLabels[i].color = active ? Color.white : new Color(0.78f, 0.92f, 1f);
            }
        }

        private void HandleTimeChanged(float time)
        {
            if (timerValue == null)
                return;

            float safeTime = Mathf.Max(0f, time);
            timerValue.text = safeTime > 9.95f ? "TIME\n" + Mathf.CeilToInt(safeTime) : "TIME\n" + safeTime.ToString("0.0");
            timerValue.color = safeTime <= 5f ? new Color(1f, 0.32f, 0.12f) : Color.white;
            timerValue.transform.localScale = safeTime <= 5f ? Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 10f) * 0.055f) : Vector3.one;
        }

        private void HandlePayoutMultiplierChanged(float multiplier, float remaining)
        {
            if (multiplierValue == null)
                return;

            bool active = multiplier > 1.01f && remaining > 0f;
            multiplierValue.gameObject.SetActive(active);
            if (active)
                multiplierValue.text = "PAYOUT x" + multiplier.ToString("0.#") + "   " + remaining.ToString("0.0") + "s";
        }

        private void HandleTimingJudged(TimingRating rating)
        {
            if (ratingRoutine != null)
                StopCoroutine(ratingRoutine);

            string text;
            Color color;
            switch (rating)
            {
                case TimingRating.Perfect:
                    text = "PERFECT POP!";
                    color = gold;
                    break;
                case TimingRating.Great:
                    text = "GREAT!";
                    color = green;
                    break;
                case TimingRating.Good:
                    text = "GOOD!";
                    color = cyan;
                    break;
                default:
                    text = "MISS!";
                    color = red;
                    break;
            }

            ratingRoutine = StartCoroutine(ShowRatingRoutine(text, color));
        }

        private void HandleGoldenRoundStarted()
        {
            if (goldenValue == null)
                return;

            goldenValue.gameObject.SetActive(true);
            goldenValue.text = "GOLDEN BALLOON ROUND!";
            goldenValue.color = gold;
            ShowUnifiedMessage("POP THE GOLDEN BALLOON!", gold, 1.25f);
        }

        private void HandleGoldenRoundEnded()
        {
            if (goldenValue != null)
                goldenValue.gameObject.SetActive(false);
        }

        private void HandleGoldenTimeChanged(float time)
        {
            if (goldenValue == null || !goldenValue.gameObject.activeSelf)
                return;

            goldenValue.text = time > 0.05f
                ? "GOLDEN ROUND   " + time.ToString("0.0")
                : "FINAL GOLDEN BALLOON!";
        }

        private void HandleRushModeChanged(bool enabledRush)
        {
            if (!enabledRush)
                return;

            // Rush mode is already reflected by gameplay speed/intensity. Avoid a second
            // large center-screen banner competing with timing feedback.
            if (comboSubtext != null)
            {
                comboSubtext.text = "RUSH MODE!";
                comboSubtext.color = orange;
            }
        }

        private void HandleJackpotWon(int tickets)
        {
            if (jackpotRoutine != null)
                StopCoroutine(jackpotRoutine);
            jackpotRoutine = StartCoroutine(JackpotRoutine(tickets));
        }

        #endregion

        #region Feedback

        private IEnumerator ShowRatingRoutine(string text, Color color)
        {
            if (ratingValue == null)
                yield break;

            ratingValue.gameObject.SetActive(true);
            ratingValue.text = text;
            ratingValue.color = color;
            ratingValue.alpha = 1f;

            float duration = 0.48f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = 0.94f + Mathf.Sin(t * Mathf.PI) * 0.10f;
                ratingValue.transform.localScale = Vector3.one * scale;
                ratingValue.alpha = 1f - Mathf.Max(0f, (t - 0.52f) / 0.48f);
                yield return null;
            }

            ratingValue.transform.localScale = Vector3.one;
            ratingValue.alpha = 1f;
            ratingValue.gameObject.SetActive(false);
            ratingRoutine = null;
        }

        private void ShowUnifiedMessage(string text, Color color, float seconds)
        {
            if (messageValue == null)
                return;
            StartCoroutine(MessageRoutine(text, color, seconds));
        }

        private IEnumerator MessageRoutine(string text, Color color, float seconds)
        {
            messageValue.gameObject.SetActive(true);
            messageValue.text = text;
            messageValue.color = color;
            messageValue.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                messageValue.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.09f);
                messageValue.alpha = 1f - Mathf.Max(0f, (t - 0.70f) / 0.30f);
                yield return null;
            }

            messageValue.transform.localScale = Vector3.one;
            messageValue.alpha = 1f;
            messageValue.gameObject.SetActive(false);
        }

        private IEnumerator JackpotRoutine(int tickets)
        {
            ShowUnifiedMessage("JACKPOT!  " + Mathf.Max(0, tickets) + " TICKETS!", gold, 3.0f);

            float duration = 2.8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 13f);
                if (hitZonePanel != null)
                    hitZonePanel.color = Color.Lerp(Color.white, new Color(1f, 0.77f, 0.18f, 1f), pulse);
                yield return null;
            }

            if (hitZonePanel != null)
                hitZonePanel.color = Color.white;
            jackpotRoutine = null;
        }

        private void RestyleLegacyTransientFeedback()
        {
            legacyUIManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (legacyUIManager == null)
                return;

            TMP_Text legacyRating = GetPrivateField<TMP_Text>(legacyUIManager, "ratingText");
            if (legacyRating != null)
                HideLegacyGraphic(legacyRating.gameObject);

            TMP_Text legacyMessage = GetPrivateField<TMP_Text>(legacyUIManager, "messageText");
            if (legacyMessage != null)
            {
                // Keep special gameplay messages working, but put them in one clean location.
                SetAnchors(legacyMessage.rectTransform, new Vector2(0.16f, 0.505f), new Vector2(0.84f, 0.565f));
                legacyMessage.fontSize = 29f;
                legacyMessage.fontStyle |= FontStyles.Bold;
                legacyMessage.alignment = TextAlignmentOptions.Center;
                legacyMessage.outlineWidth = Mathf.Max(legacyMessage.outlineWidth, 0.13f);
            }

            TMP_Text legacyCountdown = GetPrivateField<TMP_Text>(legacyUIManager, "countdownText");
            if (legacyCountdown != null)
            {
                SetAnchors(legacyCountdown.rectTransform, new Vector2(0.20f, 0.36f), new Vector2(0.80f, 0.63f));
                legacyCountdown.fontSize = 105f;
                legacyCountdown.fontStyle |= FontStyles.Bold;
                legacyCountdown.alignment = TextAlignmentOptions.Center;
                legacyCountdown.outlineWidth = Mathf.Max(legacyCountdown.outlineWidth, 0.18f);
            }

            TMP_Text legacyMultiplier = GetPrivateField<TMP_Text>(legacyUIManager, "multiplierText");
            if (legacyMultiplier != null)
                HideLegacyGraphic(legacyMultiplier.gameObject);

            TMP_Text legacyGoldenTimer = GetPrivateField<TMP_Text>(legacyUIManager, "goldenRoundTimerText");
            if (legacyGoldenTimer != null)
                HideLegacyGraphic(legacyGoldenTimer.gameObject);

            GameObject legacyGoldenBanner = GetPrivateField<GameObject>(legacyUIManager, "goldenRoundBanner");
            if (legacyGoldenBanner != null)
                HideLegacyGraphic(legacyGoldenBanner);
        }

        private static T GetPrivateField<T>(object instance, string fieldName) where T : class
        {
            if (instance == null)
                return null;

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(instance) as T : null;
        }

        private static void HideLegacyGraphic(GameObject go)
        {
            if (go == null)
                return;

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            if (group == null)
                group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        #endregion

        #region World Styling

        private void StyleWorldPlayfield()
        {
            Transform field = FindNamed("Gameplay Field");
            if (field != null)
            {
                Vector3 scale = field.localScale;
                scale.x = gameplayFieldWidthScale;
                scale.y = gameplayFieldHeightScale;
                field.localScale = scale;
            }

            TintRenderer("Outer Field Glow", new Color32(0, 226, 255, 75));
            TintRenderer("Field Backplate", new Color32(2, 10, 28, 245));
            TintRenderer("Field Core", new Color32(0, 70, 91, 45));
            TintRenderer("Field Inner Glow", new Color32(0, 226, 255, 36));
            TintRenderer("Field Left Rail", new Color32(255, 35, 183, 220));
            TintRenderer("Field Right Rail", new Color32(0, 226, 255, 220));

            StyleLane(FindNamed("Lane 1"), new Color32(2, 12, 30, 150), blue);
            StyleLane(FindNamed("Lane 2"), new Color32(3, 17, 34, 155), gold);
            StyleLane(FindNamed("Lane 3"), new Color32(2, 12, 30, 150), green);

            // The runtime HUD owns the visible hit-zone treatment. Keep the world hit-zone
            // transform/collider/logic, but hide its old rectangular artwork and label.
            if (worldHitZone != null)
            {
                SpriteRenderer[] hitRenderers = worldHitZone.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (SpriteRenderer sr in hitRenderers)
                {
                    if (sr == null) continue;
                    Color c = sr.color;
                    c.a = 0f;
                    sr.color = c;
                }

                TMP_Text[] labels = worldHitZone.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text label in labels)
                {
                    if (label == null) continue;
                    Color c = label.color;
                    c.a = 0f;
                    label.color = c;
                }
            }
        }

        private void StyleLane(Transform lane, Color bodyColor, Color edgeColor)
        {
            if (lane == null)
                return;

            SpriteRenderer[] renderers = lane.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr == null)
                    continue;

                string n = sr.name.ToLowerInvariant();
                if (n.Contains("border left") || n.Contains("border right"))
                {
                    sr.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.82f);
                    continue;
                }

                Vector2 size = GetLocalRendererSize(sr);
                sr.sprite = roundedLane;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;

                if (n.Contains("glow"))
                    sr.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.12f);
                else if (n.Contains("inner"))
                    sr.color = new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.30f);
                else
                    sr.color = bodyColor;
            }
        }

        private void TintRenderer(string objectName, Color color)
        {
            Transform t = FindNamed(objectName);
            if (t == null)
                return;

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = color;
        }

        private static Vector2 GetLocalRendererSize(SpriteRenderer sr)
        {
            if (sr == null)
                return Vector2.one;

            if (sr.drawMode != SpriteDrawMode.Simple)
                return sr.size;

            Bounds b = sr.bounds;
            Vector3 lossy = sr.transform.lossyScale;
            float width = Mathf.Abs(lossy.x) > 0.0001f ? b.size.x / Mathf.Abs(lossy.x) : b.size.x;
            float height = Mathf.Abs(lossy.y) > 0.0001f ? b.size.y / Mathf.Abs(lossy.y) : b.size.y;
            return new Vector2(Mathf.Max(0.01f, width), Mathf.Max(0.01f, height));
        }

        private void AnimateHitZone()
        {
            if (hitZonePanel == null)
                return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f);
            hitZonePanel.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.72f, 0.94f, pulse));
            hitZonePanel.transform.localScale = Vector3.one * Mathf.Lerp(0.996f, 1.014f, pulse);
        }

        private void PolishActiveBalloons()
        {
            SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr == null || sr.sprite == null)
                    continue;
                if (sr.transform.Find("BRUI_Gloss") != null)
                    continue;
                if (!IsBalloonRenderer(sr.transform))
                    continue;

                AddBalloonGloss(sr);
            }
        }

        private static bool IsBalloonRenderer(Transform candidate)
        {
            Transform cursor = candidate;
            for (int depth = 0; depth < 4 && cursor != null; depth++, cursor = cursor.parent)
            {
                // Query the known gameplay component directly. Avoid broad
                // GetComponents<MonoBehaviour>() enumeration on pooled objects.
                if (cursor.GetComponent<Balloon>() != null)
                    return true;
            }
            return false;
        }

        private void AddBalloonGloss(SpriteRenderer source)
        {
            GameObject gloss = new GameObject("BRUI_Gloss", typeof(SpriteRenderer));
            gloss.transform.SetParent(source.transform, false);
            gloss.transform.localPosition = new Vector3(-0.12f, 0.17f, -0.01f);
            gloss.transform.localScale = new Vector3(0.30f, 0.22f, 1f);

            SpriteRenderer renderer = gloss.GetComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder + 2;
            renderer.color = new Color(1f, 1f, 1f, 0.28f);
        }

        #endregion

        #region UI Helpers

        private RectTransform CreatePanel(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite sprite,
            Color color,
            float shadowAlpha)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, anchorMin, anchorMax);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;

            if (shadowAlpha > 0f)
                AddShadow(go, shadowAlpha, new Vector2(4f, -4f));
            return rt;
        }

        private void CreateSolid(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, anchorMin, anchorMax);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private TMP_Text CreateText(
            string objectName,
            Transform parent,
            string textValue,
            float maxFontSize,
            TextAlignmentOptions alignment,
            Color color,
            bool bold)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, Vector2.zero, Vector2.one);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(8f, maxFontSize * 0.52f);
            text.fontSizeMax = maxFontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.margin = new Vector4(8f, 4f, 8f, 4f);
            text.raycastTarget = false;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.outlineWidth = bold ? 0.08f : 0f;
            text.outlineColor = new Color32(0, 0, 0, 210);

            TMP_Text source = FindAnyExistingTmpFont();
            if (source != null && source.font != null)
                text.font = source.font;

            return text;
        }

        private TMP_Text FindAnyExistingTmpFont()
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text != null && text.font != null && (root == null || !text.transform.IsChildOf(root)))
                    return text;
            }
            return null;
        }

        private static void AddShadow(GameObject go, float alpha, Vector2 distance)
        {
            Shadow shadow = go.GetComponent<Shadow>();
            if (shadow == null)
                shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            if (rt == null)
                return;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private Color GetPayoutColor(int value)
        {
            switch (value)
            {
                case 500: return Color.white;
                case 250: return new Color(0.58f, 0.28f, 0.92f, 1f);
                case 100: return new Color(0.10f, 0.48f, 0.95f, 1f);
                case 50: return new Color(0.10f, 0.60f, 0.31f, 1f);
                case 25: return new Color(0.78f, 0.37f, 0.08f, 1f);
                case 10: return new Color(0.06f, 0.49f, 0.66f, 1f);
                case 5: return new Color(0.59f, 0.18f, 0.48f, 1f);
                default: return new Color(0.48f, 0.34f, 0.08f, 1f);
            }
        }

        #endregion
    }
}
