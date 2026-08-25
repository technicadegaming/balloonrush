using System;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// v1.8.7 attract presentation.
    ///
    /// The original AttractModeManager remains active underneath this overlay
    /// and continues to own credits, Start, operator access and scene changes.
    /// This class is presentation-only.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public sealed class BalloonRushAttractVisualV187 : MonoBehaviour
    {
        private static readonly Color DeepNavy = new Color32(2, 8, 28, 255);
        private static readonly Color PanelNavy = new Color32(5, 20, 48, 248);
        private static readonly Color Cyan = new Color32(0, 229, 255, 255);
        private static readonly Color Magenta = new Color32(255, 38, 188, 255);
        private static readonly Color Blue = new Color32(40, 132, 255, 255);
        private static readonly Color Gold = new Color32(255, 194, 30, 255);
        private static readonly Color Green = new Color32(40, 229, 103, 255);
        private static readonly Color Red = new Color32(242, 58, 75, 255);
        private static readonly Color Purple = new Color32(171, 74, 255, 255);

        private Canvas canvas;
        private RectTransform root;
        private TMP_FontAsset font;

        private Sprite panelSprite;
        private Sprite darkPanelSprite;
        private Sprite circleSprite;
        private Sprite softSprite;

        private TMP_Text creditsText;
        private TMP_Text highScoreText;
        private TMP_Text jackpotText;
        private TMP_Text startText;
        private TMP_Text priceText;
        private TMP_Text taglineText;

        private readonly RectTransform[] demoBalloons = new RectTransform[8];
        private readonly float[] demoStartY =
        {
            -0.16f, 0.03f, 0.23f, 0.48f,
            -0.02f, 0.34f, 0.63f, 0.78f
        };
        private readonly float[] demoSpeeds =
        {
            0.082f, 0.067f, 0.074f, 0.060f,
            0.071f, 0.064f, 0.056f, 0.050f
        };
        private readonly int[] demoLanes =
        {
            0, 1, 2, 0, 2, 1, 0, 2
        };

        private readonly Image[] marqueeBulbs = new Image[28];

        private float nextDataRefresh;
        private float nextTagline;
        private int taglineIndex;

        private static readonly string[] Taglines =
        {
            "SELECT A LANE  -  POP IN THE HIT ZONE",
            "BUILD YOUR COMBO  -  WIN MORE TICKETS",
            "AVOID BOMBS  -  WATCH FOR SPECIAL BALLOONS",
            "TIME THE POP  -  PERFECT HITS PAY BEST"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoad()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallCurrentScene()
        {
            TryInstall();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall();
        }

        private static void TryInstall()
        {
            if (!string.Equals(
                    SceneManager.GetActiveScene().name,
                    "AttractMode",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AttractModeManager manager =
                FindFirstObjectByType<AttractModeManager>(
                    FindObjectsInactive.Include);

            Canvas target =
                manager != null
                    ? manager.GetComponentInParent<Canvas>()
                    : null;

            if (target == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                if (canvases.Length > 0)
                    target = canvases[0];
            }

            if (target != null &&
                target.GetComponent<BalloonRushAttractVisualV187>() == null)
            {
                target.gameObject.AddComponent<BalloonRushAttractVisualV187>();
            }
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                enabled = false;
                return;
            }

            font = FindExistingFont();

            panelSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(
                PanelNavy,
                Cyan,
                128,
                56,
                5,
                50);

            darkPanelSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(
                new Color32(2, 12, 34, 250),
                new Color32(38, 164, 230, 210),
                128,
                56,
                4,
                50);

            circleSprite = CreateCircleSprite(false);
            softSprite = CreateCircleSprite(true);

            Build();
        }

        private void Update()
        {
            if (root != null &&
                root.GetSiblingIndex() != root.parent.childCount - 1)
            {
                root.SetAsLastSibling();
            }

            AnimateDemoBalloons();
            AnimateMarquee();

            if (startText != null)
            {
                float pulse =
                    0.72f +
                    0.28f *
                    (0.5f + 0.5f *
                     Mathf.Sin(Time.unscaledTime * 5.1f));

                Color c = startText.color;
                c.a = pulse;
                startText.color = c;
            }

            if (Time.unscaledTime >= nextDataRefresh)
            {
                nextDataRefresh = Time.unscaledTime + 0.20f;
                RefreshLiveData();
            }

            if (Time.unscaledTime >= nextTagline)
            {
                nextTagline = Time.unscaledTime + 3.1f;
                taglineIndex =
                    (taglineIndex + 1) %
                    Taglines.Length;

                if (taglineText != null)
                    taglineText.text = Taglines[taglineIndex];
            }
        }

        private void Build()
        {
            GameObject rootObject = new GameObject(
                "BR187_AttractOverlay",
                typeof(RectTransform),
                typeof(CanvasGroup));

            root = rootObject.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            Stretch(root);
            root.SetAsLastSibling();

            CanvasGroup group =
                rootObject.GetComponent<CanvasGroup>();

            group.interactable = false;
            group.blocksRaycasts = false;

            CreateSolid(
                "Background",
                root,
                Vector2.zero,
                Vector2.one,
                DeepNavy).SetAsFirstSibling();

            BuildBackdrop();
            BuildHeader();
            BuildPlayfield();
            BuildSideRails();
            BuildControls();
            BuildStartStrip();

            taglineIndex = 0;
            nextTagline = Time.unscaledTime + 3.1f;
            RefreshLiveData();
        }

        private void BuildBackdrop()
        {
            for (int i = 0; i < 12; i++)
            {
                float x = 0.09f + i * 0.075f;

                RectTransform ray = CreateSolid(
                    "Ray_" + i,
                    root,
                    new Vector2(x, 0.20f),
                    new Vector2(x + 0.010f, 0.88f),
                    i % 2 == 0
                        ? new Color(
                            Cyan.r,
                            Cyan.g,
                            Cyan.b,
                            0.025f)
                        : new Color(
                            Magenta.r,
                            Magenta.g,
                            Magenta.b,
                            0.021f));

                ray.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        (i - 5.5f) * 1.8f);

                ray.SetAsFirstSibling();
            }
        }

        private void BuildHeader()
        {
            RectTransform header = CreatePanel(
                "Header",
                root,
                new Vector2(0.018f, 0.892f),
                new Vector2(0.982f, 0.986f),
                panelSprite,
                Color.white);

            RectTransform credits = CreatePanel(
                "CreditsCard",
                header,
                new Vector2(0.014f, 0.12f),
                new Vector2(0.205f, 0.88f),
                darkPanelSprite,
                Color.white);

            creditsText = CreateText(
                "Credits",
                credits,
                "CREDITS\n0",
                31f,
                Color.white,
                TextAlignmentOptions.Center,
                true);

            RectTransform title = CreatePanel(
                "TitleCard",
                header,
                new Vector2(0.216f, 0.08f),
                new Vector2(0.784f, 0.92f),
                panelSprite,
                new Color(
                    Purple.r,
                    Purple.g,
                    Purple.b,
                    0.52f));

            TMP_Text logo = CreateText(
                "Logo",
                title,
                "<b><color=#FFFFFF>BALLOON</color> <color=#FF355F>RUSH</color></b>",
                58f,
                Color.white,
                TextAlignmentOptions.Center,
                true);

            logo.richText = true;
            logo.characterSpacing = 1.1f;
            SetAnchors(
                logo.rectTransform,
                new Vector2(0.04f, 0.25f),
                new Vector2(0.96f, 0.98f));

            highScoreText = CreateText(
                "HighScore",
                title,
                "HIGH SCORE  0",
                16f,
                Cyan,
                TextAlignmentOptions.Center,
                true);

            SetAnchors(
                highScoreText.rectTransform,
                new Vector2(0.08f, 0.01f),
                new Vector2(0.92f, 0.30f));

            RectTransform jackpot = CreatePanel(
                "JackpotCard",
                header,
                new Vector2(0.795f, 0.12f),
                new Vector2(0.986f, 0.88f),
                darkPanelSprite,
                new Color(
                    Gold.r,
                    Gold.g,
                    Gold.b,
                    0.90f));

            jackpotText = CreateText(
                "Jackpot",
                jackpot,
                "JACKPOT\n500 TICKETS",
                24f,
                Color.white,
                TextAlignmentOptions.Center,
                true);
        }

        private void BuildPlayfield()
        {
            RectTransform field = CreatePanel(
                "DemoField",
                root,
                new Vector2(0.165f, 0.235f),
                new Vector2(0.835f, 0.805f),
                darkPanelSprite,
                new Color(1f, 1f, 1f, 0.92f));

            float[] laneMin = { 0.025f, 0.338f, 0.662f };
            float[] laneMax = { 0.338f, 0.662f, 0.975f };
            Color[] laneColors = { Blue, Gold, Green };

            for (int i = 0; i < 3; i++)
            {
                RectTransform lane = CreatePanel(
                    "Lane_" + (i + 1),
                    field,
                    new Vector2(laneMin[i], 0.035f),
                    new Vector2(laneMax[i], 0.965f),
                    darkPanelSprite,
                    new Color(
                        laneColors[i].r,
                        laneColors[i].g,
                        laneColors[i].b,
                        i == 1 ? 0.18f : 0.10f));

                TMP_Text label = CreateText(
                    "LaneLabel_" + i,
                    lane,
                    "LANE " + (i + 1),
                    12f,
                    laneColors[i],
                    TextAlignmentOptions.Center,
                    true);

                SetAnchors(
                    label.rectTransform,
                    new Vector2(0.08f, 0.925f),
                    new Vector2(0.92f, 0.985f));
            }

            RectTransform hitZone = CreatePanel(
                "HitZone",
                field,
                new Vector2(0.018f, 0.553f),
                new Vector2(0.982f, 0.635f),
                panelSprite,
                new Color(
                    Cyan.r,
                    Cyan.g,
                    Cyan.b,
                    0.38f));

            TMP_Text hit = CreateText(
                "HitText",
                hitZone,
                ">>>   HIT ZONE  -  POP NOW!   <<<",
                20f,
                Color.white,
                TextAlignmentOptions.Center,
                true);

            hit.characterSpacing = 1.2f;

            BuildDemoBalloons(field);

            RectTransform leftGuide = CreatePanel(
                "HowTo",
                root,
                new Vector2(0.018f, 0.235f),
                new Vector2(0.155f, 0.805f),
                panelSprite,
                new Color(
                    Magenta.r,
                    Magenta.g,
                    Magenta.b,
                    0.28f));

            CreateText(
                "HowTitle",
                leftGuide,
                "HOW\nTO\nPLAY",
                21f,
                Color.white,
                TextAlignmentOptions.Center,
                true);

            TMP_Text steps = CreateText(
                "Steps",
                leftGuide,
                "1\nSELECT\nLANE\n\n2\nWAIT FOR\nHIT ZONE\n\n3\nPRESS\nPOP",
                13f,
                new Color(0.88f, 0.96f, 1f),
                TextAlignmentOptions.Center,
                true);

            SetAnchors(
                steps.rectTransform,
                new Vector2(0.08f, 0.05f),
                new Vector2(0.92f, 0.75f));

            RectTransform payout = CreatePanel(
                "Payout",
                root,
                new Vector2(0.845f, 0.235f),
                new Vector2(0.982f, 0.805f),
                panelSprite,
                new Color(
                    Purple.r,
                    Purple.g,
                    Purple.b,
                    0.28f));

            TMP_Text payoutTitle = CreateText(
                "PayoutTitle",
                payout,
                "PAYOUT",
                16f,
                Gold,
                TextAlignmentOptions.Center,
                true);

            SetAnchors(
                payoutTitle.rectTransform,
                new Vector2(0.05f, 0.91f),
                new Vector2(0.95f, 0.99f));

            int[] values = { 500, 250, 100, 50, 25, 10, 5, 1 };

            for (int i = 0; i < values.Length; i++)
            {
                float top = 0.88f - i * 0.102f;
                float bottom = top - 0.078f;

                RectTransform tile = CreatePanel(
                    "Pay_" + values[i],
                    payout,
                    new Vector2(0.13f, bottom),
                    new Vector2(0.87f, top),
                    darkPanelSprite,
                    values[i] == 500
                        ? new Color(
                            Gold.r,
                            Gold.g,
                            Gold.b,
                            0.62f)
                        : new Color(
                            Cyan.r,
                            Cyan.g,
                            Cyan.b,
                            0.12f));

                CreateText(
                    "PayText_" + values[i],
                    tile,
                    values[i].ToString(),
                    values[i] == 500 ? 19f : 15f,
                    Color.white,
                    TextAlignmentOptions.Center,
                    true);
            }
        }

        private void BuildDemoBalloons(RectTransform field)
        {
            Color[] colors =
            {
                Green, Blue, Red, Green,
                Purple, Gold, Green, Blue
            };

            string[] icons =
            {
                "+1", "+5", "!", "+1",
                "x2", "?", "+1", "+5"
            };

            float[] laneX = { 0.18f, 0.50f, 0.82f };

            for (int i = 0; i < demoBalloons.Length; i++)
            {
                GameObject go = new GameObject(
                    "DemoBalloon_" + i,
                    typeof(RectTransform),
                    typeof(Image));

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.SetParent(field, false);

                float x = laneX[demoLanes[i]];
                float y = demoStartY[i];

                rt.anchorMin = new Vector2(x, y);
                rt.anchorMax = new Vector2(x, y);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(78f, 100f);

                Image image = go.GetComponent<Image>();
                image.sprite = RuntimeSpriteLibrary.BalloonSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = colors[i];
                image.raycastTarget = false;

                TMP_Text icon = CreateText(
                    "Icon",
                    rt,
                    icons[i],
                    icons[i] == "x2" ? 18f : 22f,
                    Color.white,
                    TextAlignmentOptions.Center,
                    true);

                SetAnchors(
                    icon.rectTransform,
                    new Vector2(0.14f, 0.19f),
                    new Vector2(0.86f, 0.83f));

                demoBalloons[i] = rt;
            }
        }

        private void BuildSideRails()
        {
            RectTransform leftRail = CreatePanel(
                "LeftMarquee",
                root,
                new Vector2(0.004f, 0.20f),
                new Vector2(0.016f, 0.89f),
                darkPanelSprite,
                new Color(
                    Magenta.r,
                    Magenta.g,
                    Magenta.b,
                    0.62f));

            RectTransform rightRail = CreatePanel(
                "RightMarquee",
                root,
                new Vector2(0.984f, 0.20f),
                new Vector2(0.996f, 0.89f),
                darkPanelSprite,
                new Color(
                    Cyan.r,
                    Cyan.g,
                    Cyan.b,
                    0.62f));

            for (int side = 0; side < 2; side++)
            {
                RectTransform rail =
                    side == 0 ? leftRail : rightRail;

                for (int i = 0; i < 14; i++)
                {
                    GameObject bulb = new GameObject(
                        "Bulb_" + side + "_" + i,
                        typeof(RectTransform),
                        typeof(Image));

                    RectTransform rt =
                        bulb.GetComponent<RectTransform>();

                    rt.SetParent(rail, false);

                    float y =
                        Mathf.Lerp(
                            0.025f,
                            0.975f,
                            i / 13f);

                    rt.anchorMin = new Vector2(0.5f, y);
                    rt.anchorMax = new Vector2(0.5f, y);
                    rt.sizeDelta = new Vector2(20f, 20f);

                    Image img = bulb.GetComponent<Image>();
                    img.sprite = circleSprite;
                    img.preserveAspect = true;
                    img.raycastTarget = false;

                    marqueeBulbs[side * 14 + i] = img;
                }
            }
        }

        private void BuildControls()
        {
            RectTransform deck = CreatePanel(
                "ControlDeck",
                root,
                new Vector2(0.018f, 0.018f),
                new Vector2(0.982f, 0.178f),
                panelSprite,
                Color.white);

            TMP_Text caption = CreateText(
                "Caption",
                deck,
                "CABINET CONTROLS",
                12f,
                Cyan,
                TextAlignmentOptions.Center,
                true);

            SetAnchors(
                caption.rectTransform,
                new Vector2(0.25f, 0.84f),
                new Vector2(0.75f, 0.99f));

            CreateControl(
                deck,
                "LEFT",
                "<",
                "LEFT",
                0.17f,
                Blue);

            CreateControl(
                deck,
                "POP",
                "POP!",
                "POP",
                0.50f,
                Red);

            CreateControl(
                deck,
                "RIGHT",
                ">",
                "RIGHT",
                0.83f,
                Green);
        }

        private void CreateControl(
            RectTransform deck,
            string objectName,
            string faceText,
            string label,
            float x,
            Color color)
        {
            GameObject button = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image));

            RectTransform rt =
                button.GetComponent<RectTransform>();

            rt.SetParent(deck, false);
            rt.anchorMin = new Vector2(x, 0.51f);
            rt.anchorMax = new Vector2(x, 0.51f);
            rt.sizeDelta = new Vector2(105f, 105f);

            Image image = button.GetComponent<Image>();
            image.sprite = circleSprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;

            TMP_Text face = CreateText(
                "Face",
                rt,
                faceText,
                objectName == "POP" ? 24f : 40f,
                Color.white,
                TextAlignmentOptions.Center,
                true);

            TMP_Text labelText = CreateText(
                "Label",
                deck,
                label,
                13f,
                Color.white,
                TextAlignmentOptions.Center,
                true);

            SetAnchors(
                labelText.rectTransform,
                new Vector2(x - 0.10f, 0.04f),
                new Vector2(x + 0.10f, 0.21f));
        }

        private void BuildStartStrip()
        {
            RectTransform strip = CreatePanel(
                "StartStrip",
                root,
                new Vector2(0.115f, 0.810f),
                new Vector2(0.885f, 0.886f),
                panelSprite,
                new Color(
                    Cyan.r,
                    Cyan.g,
                    Cyan.b,
                    0.22f));

            taglineText = CreateText(
                "Tagline",
                strip,
                Taglines[0],
                15f,
                new Color(0.82f, 0.95f, 1f),
                TextAlignmentOptions.Center,
                true);

            SetAnchors(
                taglineText.rectTransform,
                new Vector2(0.04f, 0.54f),
                new Vector2(0.96f, 0.96f));

            startText = CreateText(
                "Start",
                strip,
                "SWIPE CARD TO PLAY",
                23f,
                Gold,
                TextAlignmentOptions.Center,
                true);

            SetAnchors(
                startText.rectTransform,
                new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.58f));

            priceText = CreateText(
                "Price",
                root,
                "SWIPE CARD - $1.00",
                12f,
                Gold,
                TextAlignmentOptions.Center,
                true);

            SetAnchors(
                priceText.rectTransform,
                new Vector2(0.16f, 0.181f),
                new Vector2(0.84f, 0.212f));
        }

        private void RefreshLiveData()
        {
            if (!GameServices.IsReady)
                return;

            int credits =
                GameServices.Credits != null
                    ? GameServices.Credits.Credits
                    : 0;

            bool freePlay =
                GameServices.Settings != null &&
                GameServices.Settings.Current != null &&
                GameServices.Settings.Current.freePlay;

            int creditsPerPlay =
                GameServices.Settings != null &&
                GameServices.Settings.Current != null
                    ? Mathf.Max(
                        1,
                        GameServices.Settings.Current.creditsPerPlay)
                    : 1;

            int priceCents =
                GameServices.Settings != null &&
                GameServices.Settings.Current != null
                    ? Mathf.Max(
                        0,
                        GameServices.Settings.Current.pricePerPlayCents)
                    : 100;

            int jackpot =
                GameServices.Settings != null &&
                GameServices.Settings.Current != null
                    ? GameServices.Settings.Current.jackpotTickets
                    : 500;

            int highScore =
                GameServices.Save != null &&
                GameServices.Save.Data != null
                    ? GameServices.Save.Data.highScores.topScore
                    : 0;

            if (creditsText != null)
            {
                creditsText.text =
                    freePlay
                        ? "FREE PLAY"
                        : "CREDITS\n" + credits;
            }

            if (highScoreText != null)
            {
                highScoreText.text =
                    "HIGH SCORE  " +
                    highScore.ToString("N0");
            }

            if (jackpotText != null)
            {
                jackpotText.text =
                    "JACKPOT\n" +
                    jackpot +
                    " TICKETS";
            }

            bool ready =
                freePlay ||
                credits >= creditsPerPlay;

            if (startText != null)
            {
                startText.text =
                    ready
                        ? "PRESS START  -  ENTER / P"
                        : "SWIPE CARD TO PLAY";

                startText.color =
                    ready
                        ? Green
                        : Gold;
            }

            if (priceText != null)
            {
                string text =
                    freePlay
                        ? "FREE PLAY"
                        : "SWIPE CARD - $" +
                          (priceCents / 100f).ToString("0.00");

                if (Application.isEditor ||
                    Debug.isDebugBuild)
                {
                    text += "    |    C = TEST CREDIT";
                }

                priceText.text = text;
            }
        }

        private void AnimateDemoBalloons()
        {
            float time = Time.unscaledTime;
            float[] laneX = { 0.18f, 0.50f, 0.82f };

            for (int i = 0; i < demoBalloons.Length; i++)
            {
                RectTransform rt = demoBalloons[i];
                if (rt == null)
                    continue;

                float y =
                    Mathf.Repeat(
                        demoStartY[i] +
                        time * demoSpeeds[i] +
                        0.18f,
                        1.20f) -
                    0.10f;

                float x =
                    laneX[demoLanes[i]] +
                    Mathf.Sin(
                        time * (1.0f + i * 0.07f) +
                        i * 0.8f) *
                    0.012f;

                rt.anchorMin = new Vector2(x, y);
                rt.anchorMax = new Vector2(x, y);

                float pulse =
                    1f +
                    Mathf.Sin(
                        time * 3.2f +
                        i) *
                    0.025f;

                rt.localScale =
                    Vector3.one * pulse;
            }
        }

        private void AnimateMarquee()
        {
            float time = Time.unscaledTime;

            for (int i = 0; i < marqueeBulbs.Length; i++)
            {
                Image bulb = marqueeBulbs[i];
                if (bulb == null)
                    continue;

                int row = i % 14;
                int side = i / 14;

                float wave =
                    0.5f +
                    0.5f *
                    Mathf.Sin(
                        time * 6.0f -
                        row * 0.80f +
                        side * 2.5f);

                wave = Mathf.Pow(wave, 4.5f);

                float hue =
                    Mathf.Repeat(
                        0.48f +
                        row * 0.038f +
                        time * 0.04f +
                        side * 0.34f,
                        1f);

                Color c =
                    Color.HSVToRGB(
                        hue,
                        0.84f,
                        1f);

                bulb.color =
                    new Color(
                        c.r,
                        c.g,
                        c.b,
                        0.16f + wave * 0.84f);

                bulb.transform.localScale =
                    Vector3.one *
                    (0.75f + wave * 0.55f);
            }
        }

        private RectTransform CreatePanel(
            string objectName,
            Transform parent,
            Vector2 min,
            Vector2 max,
            Sprite sprite,
            Color color)
        {
            GameObject go = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image));

            RectTransform rt =
                go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);
            SetAnchors(rt, min, max);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;

            return rt;
        }

        private RectTransform CreateSolid(
            string objectName,
            Transform parent,
            Vector2 min,
            Vector2 max,
            Color color)
        {
            GameObject go = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image));

            RectTransform rt =
                go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);
            SetAnchors(rt, min, max);

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return rt;
        }

        private TMP_Text CreateText(
            string objectName,
            Transform parent,
            string value,
            float maxSize,
            Color color,
            TextAlignmentOptions alignment,
            bool bold)
        {
            GameObject go = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

            RectTransform rt =
                go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);
            SetAnchors(
                rt,
                Vector2.zero,
                Vector2.one);

            TextMeshProUGUI text =
                go.GetComponent<TextMeshProUGUI>();

            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin =
                Mathf.Max(8f, maxSize * 0.52f);
            text.fontSizeMax = maxSize;
            text.fontStyle =
                bold
                    ? FontStyles.Bold
                    : FontStyles.Normal;
            text.textWrappingMode =
                TextWrappingModes.NoWrap;
            text.outlineWidth =
                bold ? 0.08f : 0f;
            text.outlineColor =
                new Color32(0, 0, 0, 220);
            text.raycastTarget = false;

            if (font != null)
                text.font = font;

            return text;
        }

        private TMP_FontAsset FindExistingFont()
        {
            TMP_Text[] texts =
                canvas.GetComponentsInChildren<TMP_Text>(
                    true);

            foreach (TMP_Text text in texts)
            {
                if (text != null &&
                    text.font != null)
                {
                    return text.font;
                }
            }

            return null;
        }

        private static void SetAnchors(
            RectTransform rt,
            Vector2 min,
            Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void Stretch(
            RectTransform rt)
        {
            SetAnchors(
                rt,
                Vector2.zero,
                Vector2.one);
        }

        private static Sprite CreateCircleSprite(
            bool soft)
        {
            const int size = 128;
            float center = (size - 1) * 0.5f;
            float radius = 60f;

            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false);

            texture.name =
                soft
                    ? "BR187_SoftCircle"
                    : "BR187_Circle";

            texture.filterMode =
                FilterMode.Bilinear;

            texture.wrapMode =
                TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;

                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy);

                    if (distance > radius)
                    {
                        texture.SetPixel(
                            x,
                            y,
                            Color.clear);

                        continue;
                    }

                    float alpha = 1f;

                    if (soft)
                    {
                        float normalized =
                            Mathf.Clamp01(
                                distance /
                                radius);

                        alpha =
                            1f -
                            Mathf.SmoothStep(
                                0.62f,
                                1f,
                                normalized);
                    }

                    texture.SetPixel(
                        x,
                        y,
                        new Color(
                            1f,
                            1f,
                            1f,
                            alpha));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(
                    0,
                    0,
                    size,
                    size),
                new Vector2(
                    0.5f,
                    0.5f),
                100f);
        }
    }
}
