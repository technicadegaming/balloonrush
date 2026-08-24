using System.Collections;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// v1.8.2 refinement companion for the unified MainGame HUD.
    /// Presentation only: no scoring, tickets, credits, spawning, timing windows,
    /// input, jackpot, operator settings, or hardware logic is changed here.
    /// </summary>
    [DefaultExecutionOrder(12)]
    public sealed class BalloonRushPresentationFXV182 : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform unifiedRoot;
        private RectTransform effectsRoot;
        private RectTransform hitZone;
        private TMP_Text hitLeft;
        private TMP_Text hitRight;
        private Image hitImage;
        private RectTransform comboRail;
        private RectTransform ratingPlate;
        private Image ratingPlateImage;
        private TMP_Text ratingText;
        private RectTransform messagePlate;
        private TMP_Text messageText;
        private RectTransform comboBurst;
        private TMP_Text comboBurstText;
        private TMP_Text comboBurstSubtext;
        private Image comboBurstImage;
        private LaneManager laneManager;
        private readonly SpriteRenderer[][] laneRenderers = new SpriteRenderer[3][];

        private Sprite cyanPlate;
        private Sprite greenPlate;
        private Sprite goldPlate;
        private Sprite redPlate;
        private Sprite purplePlate;
        private Sprite hitZonePlate;
        private Sprite sparkSprite;

        private readonly Color cyan = new Color32(0, 226, 255, 255);
        private readonly Color blue = new Color32(41, 134, 255, 255);
        private readonly Color green = new Color32(62, 235, 102, 255);
        private readonly Color gold = new Color32(255, 196, 30, 255);
        private readonly Color red = new Color32(255, 68, 78, 255);
        private readonly Color purple = new Color32(178, 82, 255, 255);
        private readonly Color orange = new Color32(255, 132, 27, 255);

        private Coroutine impactRoutine;
        private Coroutine comboRoutine;
        private int lastMilestone;
        private float chevronPhase;

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                enabled = false;
                return;
            }

            BuildSprites();
            StartCoroutine(InitializeWhenUnifiedHudExists());
        }

        private IEnumerator InitializeWhenUnifiedHudExists()
        {
            float timeout = 3f;
            while (timeout > 0f)
            {
                Transform found = canvas.transform.Find("BalloonRushUnifiedHUD");
                if (found != null)
                {
                    unifiedRoot = found as RectTransform;
                    break;
                }

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (unifiedRoot == null)
            {
                Debug.LogWarning("Balloon Rush v1.8.2 FX: unified HUD not found; refinement disabled.");
                enabled = false;
                yield break;
            }

            CacheTargets();
            RefineExistingHud();
            CacheWorldLanes();
            BuildEffectsRoot();
            BuildComboBurst();
            BindEvents();

            if (laneManager != null)
                HandleLaneChanged(laneManager.SelectedLane);
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void Update()
        {
            AnimateHitZoneChevrons();
        }

        private void BuildSprites()
        {
            cyanPlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(0, 50, 78, 220), cyan, 128, 38, 5, 30);
            greenPlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(7, 65, 42, 220), green, 128, 38, 5, 30);
            goldPlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(78, 49, 0, 225), gold, 128, 38, 5, 30);
            redPlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(72, 8, 21, 225), red, 128, 38, 5, 30);
            purplePlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(34, 12, 64, 222), purple, 128, 38, 5, 30);
            hitZonePlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(0, 100, 135, 18), cyan, 128, 46, 6, 36);
            sparkSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(Color.white, Color.white, 48, 20, 1, 14);
        }

        private void CacheTargets()
        {
            hitZone = FindRect("BRUI_HitZone");
            hitLeft = FindText("BRUI_HitLeft");
            hitRight = FindText("BRUI_HitRight");
            hitImage = hitZone != null ? hitZone.GetComponent<Image>() : null;
            comboRail = FindRect("BRUI_ComboRail");
            ratingPlate = FindRect("BRUI_RatingPlate");
            ratingText = FindText("BRUI_Rating");
            messagePlate = FindRect("BRUI_MessagePlate");
            messageText = FindText("BRUI_Message");
            laneManager = FindFirstObjectByType<LaneManager>(FindObjectsInactive.Include);
        }

        private void RefineExistingHud()
        {
            if (hitImage != null)
            {
                hitImage.sprite = hitZonePlate;
                hitImage.type = Image.Type.Sliced;
                hitImage.color = Color.white;
            }

            if (hitLeft != null)
            {
                hitLeft.fontSize = Mathf.Min(hitLeft.fontSize, 23f);
                hitLeft.fontSizeMax = 23f;
            }
            if (hitRight != null)
            {
                hitRight.fontSize = Mathf.Min(hitRight.fontSize, 23f);
                hitRight.fontSizeMax = 23f;
            }

            if (ratingPlate != null)
            {
                SetAnchors(ratingPlate, new Vector2(0.265f, 0.405f), new Vector2(0.735f, 0.465f));
                ratingPlateImage = ratingPlate.GetComponent<Image>();
                if (ratingPlateImage == null)
                    ratingPlateImage = ratingPlate.gameObject.AddComponent<Image>();
                ratingPlateImage.sprite = purplePlate;
                ratingPlateImage.type = Image.Type.Sliced;
                ratingPlateImage.color = new Color(1f, 1f, 1f, 0f);
                ratingPlateImage.raycastTarget = false;
            }

            if (ratingText != null)
            {
                ratingText.fontSize = 42f;
                ratingText.fontSizeMax = 42f;
                ratingText.fontSizeMin = 24f;
                ratingText.outlineWidth = Mathf.Max(ratingText.outlineWidth, 0.15f);
                ratingText.outlineColor = new Color32(0, 0, 0, 235);
            }

            // Keep Rush/Golden messages separate from hit-rating feedback so simultaneous
            // events never look like duplicate text stacked on the same balloon.
            if (messagePlate != null)
                SetAnchors(messagePlate, new Vector2(0.315f, 0.705f), new Vector2(0.685f, 0.745f));
            if (messageText != null)
            {
                messageText.fontSize = 20f;
                messageText.fontSizeMax = 20f;
                messageText.fontSizeMin = 12f;
            }
        }

        private void CacheWorldLanes()
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int lane = 0; lane < 3; lane++)
            {
                string target = "Lane " + (lane + 1);
                foreach (Transform t in transforms)
                {
                    if (t != null && t.name == target)
                    {
                        laneRenderers[lane] = t.GetComponentsInChildren<SpriteRenderer>(true);
                        break;
                    }
                }
            }
        }

        private void BuildEffectsRoot()
        {
            DestroyRoot("BRFX_V181");
            DestroyRoot("BRFX_V182");

            GameObject go = new GameObject("BRFX_V182", typeof(RectTransform), typeof(CanvasGroup));
            effectsRoot = go.GetComponent<RectTransform>();
            effectsRoot.SetParent(canvas.transform, false);
            SetAnchors(effectsRoot, Vector2.zero, Vector2.one);
            effectsRoot.SetAsLastSibling();

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void BuildComboBurst()
        {
            comboBurst = CreatePanel(
                "BRFX_ComboBurst",
                effectsRoot,
                new Vector2(0.28f, 0.355f),
                new Vector2(0.72f, 0.425f),
                purplePlate,
                new Color(1f, 1f, 1f, 0f));

            comboBurstImage = comboBurst.GetComponent<Image>();

            comboBurstText = CreateText(
                "BRFX_ComboBurstText",
                comboBurst,
                string.Empty,
                38f,
                TextAlignmentOptions.Center,
                Color.white);
            SetAnchors(comboBurstText.rectTransform, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.94f));
            comboBurstText.fontStyle |= FontStyles.Bold;
            comboBurstText.outlineWidth = 0.14f;
            comboBurstText.outlineColor = new Color32(0, 0, 0, 235);

            comboBurstSubtext = CreateText(
                "BRFX_ComboBurstSubtext",
                comboBurst,
                string.Empty,
                14f,
                TextAlignmentOptions.Center,
                gold);
            SetAnchors(comboBurstSubtext.rectTransform, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.30f));
            comboBurstSubtext.fontStyle |= FontStyles.Bold;

            comboBurst.gameObject.SetActive(false);
        }

        private void BindEvents()
        {
            GameEvents.TimingJudged += HandleTimingJudged;
            GameEvents.ComboChanged += HandleComboChanged;
            GameEvents.JackpotWon += HandleJackpotWon;
            GameEvents.GoldenRoundStarted += HandleGoldenRoundStarted;
            if (laneManager != null)
                laneManager.SelectedLaneChanged += HandleLaneChanged;
        }

        private void UnbindEvents()
        {
            GameEvents.TimingJudged -= HandleTimingJudged;
            GameEvents.ComboChanged -= HandleComboChanged;
            GameEvents.JackpotWon -= HandleJackpotWon;
            GameEvents.GoldenRoundStarted -= HandleGoldenRoundStarted;
            if (laneManager != null)
                laneManager.SelectedLaneChanged -= HandleLaneChanged;
        }

        private void AnimateHitZoneChevrons()
        {
            if (hitZone == null || hitLeft == null || hitRight == null)
                return;

            chevronPhase += Time.unscaledDeltaTime * 4.2f;
            float wave = 0.5f + 0.5f * Mathf.Sin(chevronPhase * Mathf.PI * 2f);
            float shift = Mathf.Lerp(1f, 9f, wave);

            hitLeft.rectTransform.anchoredPosition = new Vector2(shift, 0f);
            hitRight.rectTransform.anchoredPosition = new Vector2(-shift, 0f);

            Color glow = Color.Lerp(new Color(cyan.r, cyan.g, cyan.b, 0.58f), Color.white, wave * 0.62f);
            hitLeft.color = glow;
            hitRight.color = glow;
        }

        private void HandleTimingJudged(TimingRating rating)
        {
            Sprite sprite;
            Color color;
            float strength;

            switch (rating)
            {
                case TimingRating.Perfect:
                    sprite = goldPlate;
                    color = gold;
                    strength = 1f;
                    break;
                case TimingRating.Great:
                    sprite = greenPlate;
                    color = green;
                    strength = 0.80f;
                    break;
                case TimingRating.Good:
                    sprite = cyanPlate;
                    color = cyan;
                    strength = 0.60f;
                    break;
                default:
                    sprite = redPlate;
                    color = red;
                    strength = 0.42f;
                    break;
            }

            if (impactRoutine != null)
                StopCoroutine(impactRoutine);
            impactRoutine = StartCoroutine(TimingImpactRoutine(sprite, color, strength));
        }

        private IEnumerator TimingImpactRoutine(Sprite sprite, Color color, float strength)
        {
            if (ratingPlateImage != null)
            {
                ratingPlateImage.sprite = sprite;
                ratingPlateImage.color = new Color(1f, 1f, 1f, 0.88f);
            }

            SpawnBurst(color, Mathf.RoundToInt(Mathf.Lerp(4f, 9f, strength)), 0.38f);

            Vector3 hitBase = hitZone != null ? hitZone.localScale : Vector3.one;
            Vector3 plateBase = ratingPlate != null ? ratingPlate.localScale : Vector3.one;
            float elapsed = 0f;
            const float duration = 0.34f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pop = Mathf.Sin(t * Mathf.PI);

                if (hitZone != null)
                    hitZone.localScale = hitBase * (1f + pop * 0.030f * strength);
                if (ratingPlate != null)
                    ratingPlate.localScale = plateBase * (1f + pop * 0.055f * strength);
                if (ratingPlateImage != null)
                {
                    Color c = ratingPlateImage.color;
                    c.a = Mathf.Lerp(0.88f, 0f, Mathf.Clamp01((t - 0.50f) / 0.50f));
                    ratingPlateImage.color = c;
                }
                yield return null;
            }

            if (hitZone != null)
                hitZone.localScale = hitBase;
            if (ratingPlate != null)
                ratingPlate.localScale = plateBase;
            if (ratingPlateImage != null)
                ratingPlateImage.color = new Color(1f, 1f, 1f, 0f);

            impactRoutine = null;
        }

        private void HandleComboChanged(int combo)
        {
            if (combo < 5)
            {
                lastMilestone = 0;
                return;
            }

            int milestone = GetMilestone(combo);
            if (milestone <= 0 || milestone == lastMilestone)
                return;

            lastMilestone = milestone;
            if (comboRoutine != null)
                StopCoroutine(comboRoutine);
            comboRoutine = StartCoroutine(ComboMilestoneRoutine(milestone));
        }

        private static int GetMilestone(int combo)
        {
            if (combo >= 30) return 30;
            if (combo >= 20) return 20;
            if (combo >= 15) return 15;
            if (combo >= 10) return 10;
            if (combo >= 5) return 5;
            return 0;
        }

        private IEnumerator ComboMilestoneRoutine(int combo)
        {
            if (comboBurst == null)
                yield break;

            Color accent = combo >= 20 ? gold : combo >= 10 ? orange : purple;
            string sub = combo >= 30 ? "MEGA STREAK!" : combo >= 20 ? "AMAZING!" : combo >= 15 ? "KEEP IT GOING!" : combo >= 10 ? "ON FIRE!" : "NICE STREAK!";

            comboBurst.gameObject.SetActive(true);
            comboBurstText.text = "COMBO x" + combo;
            comboBurstSubtext.text = sub;
            comboBurstSubtext.color = accent;
            comboBurstImage.sprite = combo >= 20 ? goldPlate : purplePlate;

            SpawnBurst(accent, combo >= 20 ? 12 : 7, 0.46f);

            float elapsed = 0f;
            const float duration = 0.70f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float entry = Mathf.Clamp01(t / 0.18f);
                float fade = t > 0.62f ? 1f - Mathf.Clamp01((t - 0.62f) / 0.38f) : 1f;
                float bounce = 1f + Mathf.Sin(entry * Mathf.PI) * 0.07f;

                comboBurst.localScale = Vector3.one * bounce;
                comboBurstImage.color = new Color(1f, 1f, 1f, fade * 0.86f);
                comboBurstText.alpha = fade;
                comboBurstSubtext.alpha = fade;

                if (comboRail != null)
                    comboRail.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.015f);

                yield return null;
            }

            comboBurst.localScale = Vector3.one;
            comboBurst.gameObject.SetActive(false);
            if (comboRail != null)
                comboRail.localScale = Vector3.one;
            comboRoutine = null;
        }

        private void HandleLaneChanged(int selectedLane)
        {
            int selected = Mathf.Clamp(selectedLane, 0, 2);
            for (int lane = 0; lane < laneRenderers.Length; lane++)
            {
                SpriteRenderer[] renderers = laneRenderers[lane];
                if (renderers == null)
                    continue;

                bool active = lane == selected;
                Color accent = LaneAccent(lane);

                foreach (SpriteRenderer sr in renderers)
                {
                    if (sr == null)
                        continue;

                    string n = sr.name.ToLowerInvariant();
                    if (n.Contains("border"))
                    {
                        sr.color = new Color(accent.r, accent.g, accent.b, active ? 0.95f : 0.52f);
                    }
                    else if (n.Contains("glow"))
                    {
                        sr.color = new Color(accent.r, accent.g, accent.b, active ? 0.18f : 0.055f);
                    }
                    else if (n.Contains("inner"))
                    {
                        sr.color = new Color(0.025f, 0.075f, 0.15f, active ? 0.34f : 0.20f);
                    }
                    else
                    {
                        sr.color = new Color(0.018f, 0.055f, 0.12f, active ? 0.30f : 0.17f);
                    }
                }
            }
        }

        private Color LaneAccent(int lane)
        {
            if (lane == 0) return blue;
            if (lane == 1) return gold;
            return green;
        }

        private void HandleJackpotWon(int tickets)
        {
            SpawnBurst(gold, 26, 0.88f);
        }

        private void HandleGoldenRoundStarted()
        {
            SpawnBurst(gold, 15, 0.68f);
        }

        private void SpawnBurst(Color color, int count, float radius)
        {
            if (effectsRoot == null)
                return;

            Vector2 center = new Vector2(0.50f, 0.50f);
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / Mathf.Max(1, count)) * i + Random.Range(-8f, 8f);
                float r = Random.Range(radius * 0.35f, radius);
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                StartCoroutine(SparkRoutine(center, direction, r, color, i));
            }
        }

        private IEnumerator SparkRoutine(Vector2 center, Vector2 direction, float distance, Color color, int index)
        {
            GameObject go = new GameObject("BRFX_Spark_" + index, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(effectsRoot, false);
            rt.anchorMin = center;
            rt.anchorMax = center;
            rt.sizeDelta = new Vector2(Random.Range(6f, 12f), Random.Range(7f, 14f));

            Image img = go.GetComponent<Image>();
            img.sprite = sparkSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;

            Vector2 end = direction * distance * 205f;
            float duration = Random.Range(0.26f, 0.45f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                rt.anchoredPosition = Vector2.Lerp(Vector2.zero, end, eased);
                rt.localRotation = Quaternion.Euler(0f, 0f, t * 160f + index * 9f);
                img.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }

            Destroy(go);
        }

        private RectTransform FindRect(string name)
        {
            Transform t = unifiedRoot != null ? unifiedRoot.Find(name) : null;
            if (t == null && unifiedRoot != null)
            {
                RectTransform[] all = unifiedRoot.GetComponentsInChildren<RectTransform>(true);
                foreach (RectTransform rt in all)
                {
                    if (rt != null && rt.name == name)
                        return rt;
                }
            }
            return t as RectTransform;
        }

        private TMP_Text FindText(string name)
        {
            RectTransform rt = FindRect(name);
            return rt != null ? rt.GetComponent<TMP_Text>() : null;
        }

        private void DestroyRoot(string name)
        {
            Transform t = canvas.transform.Find(name);
            if (t != null)
                Destroy(t.gameObject);
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, min, max);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, Vector2.zero, Vector2.one);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(9f, fontSize * 0.58f);
            tmp.fontSizeMax = fontSize;
            tmp.raycastTarget = false;
            return tmp;
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
    }
}
