using System;
using System.Collections;
using System.Collections.Generic;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Balloon Rush v1.8.5 arcade "juice" pass.
    ///
    /// Adds:
    /// - stronger rounded/capsule HUD treatment
    /// - animated marquee side rails
    /// - selected-lane scanner and glow
    /// - circular cabinet buttons with reactive rings
    /// - hit-zone energy bars and pop impact rings
    /// - subtle balloon shadow/aura depth
    ///
    /// This is presentation-only. Gameplay, scoring, payout and hardware logic
    /// are not changed by this component.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public sealed class BalloonRushArcadeJuiceV185 : MonoBehaviour
    {
        private static readonly Color Cyan = new Color32(0, 229, 255, 255);
        private static readonly Color Magenta = new Color32(255, 38, 188, 255);
        private static readonly Color Blue = new Color32(40, 132, 255, 255);
        private static readonly Color Gold = new Color32(255, 194, 30, 255);
        private static readonly Color Green = new Color32(40, 229, 103, 255);
        private static readonly Color Red = new Color32(242, 58, 75, 255);

        private readonly List<Image> sideBulbs = new List<Image>(32);
        private readonly Image[] laneFrames = new Image[3];
        private readonly RectTransform[] laneScanners = new RectTransform[3];
        private readonly Image[] buttonRings = new Image[3];
        private readonly float[] buttonPulse = new float[3];

        private Canvas canvas;
        private RectTransform root;
        private LaneManager laneManager;

        private Sprite superRound;
        private Sprite softRound;
        private Sprite circleSprite;
        private Sprite ringSprite;

        private Image hitTop;
        private Image hitBottom;

        private float flashBoost;
        private float nextBalloonPolish;
        private int lastLane = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInitialScene()
        {
            TryInstall();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall();
        }

        private static void TryInstall()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Canvas candidate in canvases)
            {
                if (candidate == null)
                    continue;

                if (candidate.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (candidate.GetComponent<BalloonRushArcadeJuiceV185>() == null)
                    candidate.gameObject.AddComponent<BalloonRushArcadeJuiceV185>();

                return;
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

            DisablePriorCabinetEffects();

            superRound = RoundedSpriteFactory.CreateRoundedPanelSprite(
                new Color32(6, 19, 46, 246),
                new Color32(74, 216, 255, 220),
                128, 58, 5, 52);

            softRound = RoundedSpriteFactory.CreateRoundedPanelSprite(
                Color.white,
                Color.white,
                128, 58, 1, 52);

            circleSprite = CreateCircleSprite(Color.white, Color.white, false);
            ringSprite = CreateCircleSprite(Color.clear, Color.white, true);

            BuildOverlay();
            RestyleExistingHud();
            BuildButtonRings();
        }

        private void Start()
        {
            laneManager = FindFirstObjectByType<LaneManager>(FindObjectsInactive.Include);
            if (laneManager != null)
            {
                lastLane = Mathf.Clamp(laneManager.SelectedLane, 0, 2);
                laneManager.SelectedLaneChanged -= HandleLaneChanged;
                laneManager.SelectedLaneChanged += HandleLaneChanged;
            }

            PolishActiveBalloons();
        }

        private void OnEnable()
        {
            GameEvents.BalloonPopped += HandleBalloonPopped;
            GameEvents.TimingJudged += HandleTimingJudged;
            GameEvents.GoldenRoundStarted += HandleGoldenRoundStarted;
            GameEvents.JackpotWon += HandleJackpot;
        }

        private void OnDisable()
        {
            GameEvents.BalloonPopped -= HandleBalloonPopped;
            GameEvents.TimingJudged -= HandleTimingJudged;
            GameEvents.GoldenRoundStarted -= HandleGoldenRoundStarted;
            GameEvents.JackpotWon -= HandleJackpot;

            if (laneManager != null)
                laneManager.SelectedLaneChanged -= HandleLaneChanged;
        }

        private void Update()
        {
            AnimateMarquee();
            AnimateLanes();
            AnimateButtons();
            AnimateHitZoneEnergy();

            flashBoost = Mathf.MoveTowards(
                flashBoost,
                0f,
                Time.unscaledDeltaTime * 2.6f);

            if (Time.unscaledTime >= nextBalloonPolish)
            {
                nextBalloonPolish = Time.unscaledTime + 0.40f;
                PolishActiveBalloons();
            }
        }

        private void DisablePriorCabinetEffects()
        {
            MonoBehaviour[] behaviours = canvas.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;

                if (string.Equals(
                        behaviour.GetType().Name,
                        "BalloonRushCabinetEffectsV184",
                        StringComparison.Ordinal))
                {
                    behaviour.enabled = false;
                }
            }

            Transform oldFx = canvas.transform.Find("BR184_CabinetFX");
            if (oldFx != null)
                Destroy(oldFx.gameObject);

            Transform previous = canvas.transform.Find("BR185_ArcadeJuice");
            if (previous != null)
                Destroy(previous.gameObject);
        }

        private void BuildOverlay()
        {
            GameObject rootObject = new GameObject(
                "BR185_ArcadeJuice",
                typeof(RectTransform),
                typeof(CanvasGroup));

            root = rootObject.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            Stretch(root);
            root.SetAsLastSibling();

            CanvasGroup group = rootObject.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            BuildLaneEffects();
            BuildHitZoneEnergy();
            BuildSideMarquee();
        }

        private void BuildLaneEffects()
        {
            float[] minX = { 0.170f, 0.390f, 0.610f };
            float[] maxX = { 0.390f, 0.610f, 0.830f };
            Color[] colors = { Blue, Gold, Green };

            for (int i = 0; i < 3; i++)
            {
                RectTransform frame = CreateImage(
                    "BR185_LaneFrame_" + (i + 1),
                    root,
                    new Vector2(minX[i], 0.218f),
                    new Vector2(maxX[i], 0.786f),
                    superRound,
                    new Color(colors[i].r, colors[i].g, colors[i].b, 0.055f),
                    Image.Type.Sliced);

                laneFrames[i] = frame.GetComponent<Image>();

                RectTransform scanner = CreateImage(
                    "BR185_LaneScanner_" + (i + 1),
                    root,
                    new Vector2(minX[i] + 0.018f, 0.240f),
                    new Vector2(maxX[i] - 0.018f, 0.251f),
                    softRound,
                    new Color(colors[i].r, colors[i].g, colors[i].b, 0f),
                    Image.Type.Sliced);

                laneScanners[i] = scanner;
            }
        }

        private void BuildHitZoneEnergy()
        {
            RectTransform top = CreateImage(
                "BR185_HitEnergyTop",
                root,
                new Vector2(0.165f, 0.603f),
                new Vector2(0.835f, 0.611f),
                softRound,
                new Color(Cyan.r, Cyan.g, Cyan.b, 0.32f),
                Image.Type.Sliced);

            RectTransform bottom = CreateImage(
                "BR185_HitEnergyBottom",
                root,
                new Vector2(0.165f, 0.557f),
                new Vector2(0.835f, 0.565f),
                softRound,
                new Color(Magenta.r, Magenta.g, Magenta.b, 0.27f),
                Image.Type.Sliced);

            hitTop = top.GetComponent<Image>();
            hitBottom = bottom.GetComponent<Image>();
        }

        private void BuildSideMarquee()
        {
            CreateImage(
                "BR185_LeftRailGlow",
                root,
                new Vector2(0.006f, 0.205f),
                new Vector2(0.047f, 0.825f),
                superRound,
                new Color(Magenta.r, Magenta.g, Magenta.b, 0.18f),
                Image.Type.Sliced);

            CreateImage(
                "BR185_RightRailGlow",
                root,
                new Vector2(0.953f, 0.205f),
                new Vector2(0.994f, 0.825f),
                superRound,
                new Color(Cyan.r, Cyan.g, Cyan.b, 0.18f),
                Image.Type.Sliced);

            const int bulbCount = 14;

            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? 0.026f : 0.974f;

                for (int i = 0; i < bulbCount; i++)
                {
                    float y = Mathf.Lerp(0.232f, 0.798f, i / (float)(bulbCount - 1));

                    GameObject bulbObject = new GameObject(
                        "BR185_" + (side == 0 ? "L" : "R") + "_Bulb_" + i,
                        typeof(RectTransform),
                        typeof(Image));

                    RectTransform rt = bulbObject.GetComponent<RectTransform>();
                    rt.SetParent(root, false);
                    rt.anchorMin = new Vector2(x, y);
                    rt.anchorMax = new Vector2(x, y);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(24f, 24f);

                    Image image = bulbObject.GetComponent<Image>();
                    image.sprite = circleSprite;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    image.color = new Color(1f, 1f, 1f, 0.12f);

                    sideBulbs.Add(image);
                }
            }
        }

        private void RestyleExistingHud()
        {
            string[] roundedNames =
            {
                "BRUI_Header",
                "BRUI_TicketsCard",
                "BRUI_TitleCard",
                "BRUI_JackpotCard",
                "BRUI_TimerCard",
                "BRUI_ComboRail",
                "BRUI_ComboTrack",
                "BRUI_PayoutRail",
                "BRUI_ControlDeck",
                "BRUI_Control_LEFT",
                "BRUI_Control_POP",
                "BRUI_Control_RIGHT",
                "BRUI_HitZone",
                "BRUI_LaneTab_1",
                "BRUI_LaneTab_2",
                "BRUI_LaneTab_3"
            };

            foreach (string objectName in roundedNames)
            {
                Transform t = FindDeepChild(canvas.transform, objectName);
                if (t == null)
                    continue;

                Image image = t.GetComponent<Image>();
                if (image == null)
                    continue;

                image.sprite = superRound;
                image.type = Image.Type.Sliced;
            }

            Image[] allImages = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image image in allImages)
            {
                if (image == null)
                    continue;

                if (image.name.StartsWith("BRUI_Payout_", StringComparison.OrdinalIgnoreCase))
                {
                    image.sprite = superRound;
                    image.type = Image.Type.Sliced;
                }
            }

            RestyleButtonFace("LEFT", Blue);
            RestyleButtonFace("POP", Red);
            RestyleButtonFace("RIGHT", Green);
        }

        private void RestyleButtonFace(string label, Color color)
        {
            Transform t = FindDeepChild(canvas.transform, "BRUI_ControlFace_" + label);
            if (t == null)
                return;

            Image image = t.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = circleSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = color;

            RectTransform rt = t as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.18f, 0.17f);
                rt.anchorMax = new Vector2(0.82f, 0.93f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private void BuildButtonRings()
        {
            CreateButtonRing("LEFT", 0, Blue);
            CreateButtonRing("POP", 1, Red);
            CreateButtonRing("RIGHT", 2, Green);
        }

        private void CreateButtonRing(string label, int index, Color color)
        {
            Transform face = FindDeepChild(canvas.transform, "BRUI_ControlFace_" + label);
            if (face == null)
                return;

            Transform oldRing = face.Find("BR185_ButtonRing");
            if (oldRing != null)
                Destroy(oldRing.gameObject);

            GameObject ringObject = new GameObject(
                "BR185_ButtonRing",
                typeof(RectTransform),
                typeof(Image));

            RectTransform rt = ringObject.GetComponent<RectTransform>();
            rt.SetParent(face, false);
            rt.anchorMin = new Vector2(-0.14f, -0.14f);
            rt.anchorMax = new Vector2(1.14f, 1.14f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();

            Image ring = ringObject.GetComponent<Image>();
            ring.sprite = ringSprite;
            ring.type = Image.Type.Simple;
            ring.preserveAspect = true;
            ring.raycastTarget = false;
            ring.color = new Color(color.r, color.g, color.b, 0.32f);

            buttonRings[index] = ring;
        }

        private void AnimateMarquee()
        {
            const int perSide = 14;
            float time = Time.unscaledTime;

            for (int i = 0; i < sideBulbs.Count; i++)
            {
                Image bulb = sideBulbs[i];
                if (bulb == null)
                    continue;

                int row = i % perSide;
                int side = i / perSide;

                float phase = time * 6.4f - row * 0.82f + side * 2.8f;
                float wave = 0.5f + 0.5f * Mathf.Sin(phase);
                wave = Mathf.Pow(wave, 5.2f);

                float hue = Mathf.Repeat(
                    0.48f +
                    row * 0.036f +
                    time * 0.055f +
                    side * 0.34f,
                    1f);

                Color rainbow = Color.HSVToRGB(hue, 0.82f, 1f);

                float alpha = Mathf.Clamp01(
                    0.10f +
                    wave * 0.88f +
                    flashBoost * 0.24f);

                bulb.color = new Color(
                    rainbow.r,
                    rainbow.g,
                    rainbow.b,
                    alpha);

                bulb.transform.localScale = Vector3.one * (
                    0.72f +
                    wave * 0.58f +
                    Mathf.Min(0.16f, flashBoost * 0.05f));
            }
        }

        private void AnimateLanes()
        {
            if (laneManager == null)
                laneManager = FindFirstObjectByType<LaneManager>(FindObjectsInactive.Include);

            int selected = laneManager != null
                ? Mathf.Clamp(laneManager.SelectedLane, 0, 2)
                : 1;

            Color[] colors = { Blue, Gold, Green };
            float time = Time.unscaledTime;

            for (int i = 0; i < 3; i++)
            {
                Image frame = laneFrames[i];
                RectTransform scanner = laneScanners[i];

                if (frame == null || scanner == null)
                    continue;

                bool active = i == selected;
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 6.5f + i);

                Color c = colors[i];
                float frameAlpha = active
                    ? 0.12f + pulse * 0.09f + flashBoost * 0.045f
                    : 0.035f;

                frame.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(frameAlpha));

                float y = Mathf.Lerp(
                    0.245f,
                    0.765f,
                    Mathf.PingPong(time * 0.32f + i * 0.18f, 1f));

                scanner.anchorMin = new Vector2(scanner.anchorMin.x, y);
                scanner.anchorMax = new Vector2(scanner.anchorMax.x, y + 0.010f);

                Image scannerImage = scanner.GetComponent<Image>();
                if (scannerImage != null)
                {
                    scannerImage.color = new Color(
                        c.r,
                        c.g,
                        c.b,
                        active ? 0.18f + pulse * 0.15f : 0.015f);
                }
            }
        }

        private void AnimateButtons()
        {
            Color[] colors = { Blue, Red, Green };

            for (int i = 0; i < buttonRings.Length; i++)
            {
                Image ring = buttonRings[i];
                if (ring == null)
                    continue;

                buttonPulse[i] = Mathf.MoveTowards(
                    buttonPulse[i],
                    0f,
                    Time.unscaledDeltaTime * 3.8f);

                float idle = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.0f + i * 1.7f);
                float pulse = buttonPulse[i];

                Color c = colors[i];
                ring.color = new Color(
                    c.r,
                    c.g,
                    c.b,
                    Mathf.Clamp01(0.18f + idle * 0.10f + pulse * 0.65f));

                ring.transform.localScale =
                    Vector3.one * (1f + idle * 0.025f + pulse * 0.18f);
            }
        }

        private void AnimateHitZoneEnergy()
        {
            if (hitTop == null || hitBottom == null)
                return;

            float waveA = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7.4f);
            float waveB = 1f - waveA;

            hitTop.color = new Color(
                Cyan.r,
                Cyan.g,
                Cyan.b,
                Mathf.Clamp01(0.18f + waveA * 0.34f + flashBoost * 0.10f));

            hitBottom.color = new Color(
                Magenta.r,
                Magenta.g,
                Magenta.b,
                Mathf.Clamp01(0.16f + waveB * 0.31f + flashBoost * 0.10f));
        }

        private void HandleLaneChanged(int lane)
        {
            int next = Mathf.Clamp(lane, 0, 2);

            if (next < lastLane)
                buttonPulse[0] = 1f;
            else if (next > lastLane)
                buttonPulse[2] = 1f;

            lastLane = next;
            flashBoost = Mathf.Max(flashBoost, 0.18f);
        }

        private void HandleBalloonPopped(Balloon balloon, TimingRating rating)
        {
            flashBoost = Mathf.Max(
                flashBoost,
                rating == TimingRating.Perfect ? 0.95f : 0.50f);
        }

        private void HandleTimingJudged(TimingRating rating)
        {
            buttonPulse[1] = 1f;

            Color color;
            switch (rating)
            {
                case TimingRating.Perfect:
                    color = Gold;
                    flashBoost = Mathf.Max(flashBoost, 1.0f);
                    break;

                case TimingRating.Great:
                    color = Green;
                    flashBoost = Mathf.Max(flashBoost, 0.70f);
                    break;

                case TimingRating.Good:
                    color = Cyan;
                    flashBoost = Mathf.Max(flashBoost, 0.50f);
                    break;

                default:
                    color = Red;
                    flashBoost = Mathf.Max(flashBoost, 0.32f);
                    break;
            }

            SpawnImpactRing(color, rating == TimingRating.Perfect ? 1.28f : 1f);
        }

        private void HandleGoldenRoundStarted()
        {
            flashBoost = 1.45f;
            buttonPulse[1] = 1.15f;
        }

        private void HandleJackpot(int tickets)
        {
            flashBoost = 2.0f;
            buttonPulse[0] = 1f;
            buttonPulse[1] = 1.35f;
            buttonPulse[2] = 1f;
        }

        private void SpawnImpactRing(Color color, float scaleBoost)
        {
            if (root == null)
                return;

            int selected = laneManager != null
                ? Mathf.Clamp(laneManager.SelectedLane, 0, 2)
                : 1;

            float[] laneCenters = { 0.280f, 0.500f, 0.720f };
            Vector2 point = new Vector2(laneCenters[selected], 0.584f);

            GameObject impact = new GameObject(
                "BR185_ImpactRing",
                typeof(RectTransform),
                typeof(Image));

            RectTransform rt = impact.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.anchorMin = point;
            rt.anchorMax = point;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(110f, 110f);

            Image image = impact.GetComponent<Image>();
            image.sprite = ringSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = color;

            StartCoroutine(ImpactRoutine(rt, image, scaleBoost));
        }

        private IEnumerator ImpactRoutine(RectTransform rt, Image image, float scaleBoost)
        {
            float duration = 0.36f;
            float elapsed = 0f;

            while (elapsed < duration && rt != null && image != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scale = Mathf.Lerp(0.52f, 2.10f * scaleBoost, t);
                rt.localScale = Vector3.one * scale;

                Color c = image.color;
                c.a = 1f - t;
                image.color = c;

                yield return null;
            }

            if (rt != null)
                Destroy(rt.gameObject);
        }

        private void PolishActiveBalloons()
        {
            Balloon[] balloons = FindObjectsByType<Balloon>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (Balloon balloon in balloons)
            {
                if (balloon == null || !balloon.IsActiveBalloon)
                    continue;

                SpriteRenderer source = balloon.GetComponentInChildren<SpriteRenderer>();
                if (source == null || source.sprite == null)
                    continue;

                if (source.transform.Find("BR185_Shadow") == null)
                {
                    CreateBalloonLayer(
                        source,
                        "BR185_Shadow",
                        new Color(0f, 0f, 0f, 0.28f),
                        new Vector3(0.07f, -0.07f, 0.02f),
                        Vector3.one * 1.04f,
                        -2);
                }

                if (source.transform.Find("BR185_Aura") == null)
                {
                    Color aura = source.color;
                    aura.a = 0.16f;

                    CreateBalloonLayer(
                        source,
                        "BR185_Aura",
                        aura,
                        new Vector3(0f, 0f, 0.03f),
                        Vector3.one * 1.16f,
                        -1);
                }
            }
        }

        private static void CreateBalloonLayer(
            SpriteRenderer source,
            string childName,
            Color color,
            Vector3 localPosition,
            Vector3 localScale,
            int orderOffset)
        {
            GameObject layerObject = new GameObject(childName, typeof(SpriteRenderer));
            layerObject.transform.SetParent(source.transform, false);
            layerObject.transform.localPosition = localPosition;
            layerObject.transform.localScale = localScale;

            SpriteRenderer layer = layerObject.GetComponent<SpriteRenderer>();
            layer.sprite = source.sprite;
            layer.color = color;
            layer.sortingLayerID = source.sortingLayerID;
            layer.sortingOrder = source.sortingOrder + orderOffset;
        }

        private RectTransform CreateImage(
            string objectName,
            Transform parent,
            Vector2 min,
            Vector2 max,
            Sprite sprite,
            Color color,
            Image.Type type)
        {
            GameObject go = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image));

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.color = color;
            image.raycastTarget = false;

            return rt;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            Transform[] all = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t != null &&
                    string.Equals(t.name, childName, StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }

            return null;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static Sprite CreateCircleSprite(Color fill, Color border, bool ringOnly)
        {
            const int size = 128;
            const float radius = 61f;
            const float borderWidth = 7f;

            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);

            texture.name = ringOnly ? "BR185_Ring" : "BR185_Circle";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);

                    Color pixel;
                    if (distance > radius)
                    {
                        pixel = Color.clear;
                    }
                    else if (ringOnly)
                    {
                        pixel = distance >= radius - borderWidth ? border : Color.clear;
                    }
                    else
                    {
                        pixel = distance >= radius - borderWidth ? border : fill;
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }
}
