using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// v1.8.1 Results celebration. Presentation only.
    /// ResultsManager remains responsible for score/ticket values, payout, replay,
    /// credits, and scene navigation.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public sealed class BalloonRushResultsPresentationV181 : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform backRoot;
        private RectTransform frontRoot;
        private TMP_Text ticketResult;
        private TMP_Text replayPrompt;
        private RectTransform ticketHalo;
        private Image ticketHaloImage;

        private Sprite panel;
        private Sprite goldPanel;
        private Sprite cyanPanel;
        private Sprite sparkSprite;

        private Color cyan = new Color32(0, 226, 255, 255);
        private Color magenta = new Color32(255, 35, 183, 255);
        private Color gold = new Color32(255, 193, 28, 255);
        private Color green = new Color32(52, 231, 93, 255);

        private readonly List<Coroutine> activeSparks = new List<Coroutine>();

        private IEnumerator Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                yield break;

            BuildSprites();
            BuildBackgroundDecor();

            // ResultsManager fills text values during Start. Wait for it.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);

            StyleExistingResults();
            FindImportantTexts();
            BuildForegroundDecor();

            if (ticketResult != null)
                StartCoroutine(WatchTicketCountAndCelebrate());
            if (replayPrompt != null)
                StartCoroutine(PulseReplayPrompt());
        }

        private void BuildSprites()
        {
            panel = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(4, 22, 52, 247), cyan, 128, 34, 5, 30);
            goldPanel = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(82, 50, 0, 248), gold, 128, 40, 6, 34);
            cyanPanel = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(0, 48, 77, 245), cyan, 128, 36, 5, 30);
            sparkSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(Color.white, Color.white, 48, 20, 1, 14);
        }

        private void BuildBackgroundDecor()
        {
            DestroyOld("BRUI_ResultsDecor");
            DestroyOld("BRFX_ResultsBackV181");
            DestroyOld("BRFX_ResultsFrontV181");

            GameObject back = new GameObject("BRFX_ResultsBackV181", typeof(RectTransform), typeof(CanvasGroup));
            backRoot = back.GetComponent<RectTransform>();
            backRoot.SetParent(canvas.transform, false);
            SetAnchors(backRoot, Vector2.zero, Vector2.one);
            backRoot.SetAsFirstSibling();

            CanvasGroup group = back.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            CreateSolid("BRFX_ResultLeftGlow", backRoot, new Vector2(0.003f, 0f), new Vector2(0.014f, 1f), magenta);
            CreateSolid("BRFX_ResultRightGlow", backRoot, new Vector2(0.986f, 0f), new Vector2(0.997f, 1f), cyan);

            // Soft radial-ish celebratory rays.
            for (int i = 0; i < 18; i++)
            {
                float x = 0.02f + i * 0.056f;
                RectTransform ray = CreatePanel(
                    "BRFX_ResultRay_" + i,
                    backRoot,
                    new Vector2(x, 0.18f),
                    new Vector2(Mathf.Min(0.995f, x + 0.010f), 0.91f),
                    panel,
                    i % 2 == 0
                        ? new Color(cyan.r, cyan.g, cyan.b, 0.035f)
                        : new Color(magenta.r, magenta.g, magenta.b, 0.035f));
                ray.localRotation = Quaternion.Euler(0f, 0f, (i - 8.5f) * 2.1f);
            }

            // Corner stars / dots to keep the background from feeling empty.
            for (int i = 0; i < 18; i++)
            {
                float px = Random.Range(0.05f, 0.95f);
                float py = Random.Range(0.16f, 0.92f);
                RectTransform star = CreatePanel(
                    "BRFX_ResultStar_" + i,
                    backRoot,
                    new Vector2(px, py),
                    new Vector2(px + 0.010f, py + 0.010f),
                    sparkSprite,
                    i % 3 == 0 ? gold : (i % 2 == 0 ? cyan : magenta));
                star.localRotation = Quaternion.Euler(0f, 0f, i * 19f);
            }
        }

        private void BuildForegroundDecor()
        {
            GameObject front = new GameObject("BRFX_ResultsFrontV181", typeof(RectTransform), typeof(CanvasGroup));
            frontRoot = front.GetComponent<RectTransform>();
            frontRoot.SetParent(canvas.transform, false);
            SetAnchors(frontRoot, Vector2.zero, Vector2.one);
            frontRoot.SetAsLastSibling();

            CanvasGroup group = front.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            ticketHalo = CreatePanel(
                "BRFX_TicketHalo",
                frontRoot,
                new Vector2(0.17f, 0.525f),
                new Vector2(0.83f, 0.795f),
                goldPanel,
                new Color(1f, 1f, 1f, 0.13f));
            ticketHaloImage = ticketHalo.GetComponent<Image>();
            ticketHalo.SetAsFirstSibling();
        }

        private void StyleExistingResults()
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t == null || IsOurDecor(t.transform))
                    continue;

                string value = (t.text ?? string.Empty).ToUpperInvariant();
                t.textWrappingMode = TextWrappingModes.NoWrap;

                if (value.Contains("BALLOON RUSH RESULTS") || value.Contains("JACKPOT RESULTS"))
                {
                    t.fontStyle |= FontStyles.Bold;
                    t.fontSize = Mathf.Max(t.fontSize, 44f);
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.16f);
                    t.outlineColor = new Color32(0, 0, 0, 235);
                }
                else if (value.Contains("TICKETS"))
                {
                    t.fontStyle |= FontStyles.Bold;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.16f);
                    t.outlineColor = new Color32(0, 0, 0, 235);
                }
                else if (value.Contains("PLAY AGAIN") || value.Contains("ENTER OR P"))
                {
                    t.fontStyle |= FontStyles.Bold;
                    t.color = cyan;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.10f);
                }
                else if (value.Contains("HIGH SCORE") || value.Contains("TOP SCORE") || value.Contains("NEW "))
                {
                    t.color = gold;
                    t.fontStyle |= FontStyles.Bold;
                }
            }

            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img == null || IsOurDecor(img.transform))
                    continue;

                string n = img.name.ToLowerInvariant();
                if (n.Contains("ticket") || n.Contains("result"))
                {
                    img.sprite = goldPanel;
                    img.type = Image.Type.Sliced;
                }
                else if (n.Contains("summary"))
                {
                    img.sprite = panel;
                    img.type = Image.Type.Sliced;
                }
                else if (n.Contains("replay") || n.Contains("play again"))
                {
                    img.sprite = cyanPanel;
                    img.type = Image.Type.Sliced;
                }
            }
        }

        private void FindImportantTexts()
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t == null || IsOurDecor(t.transform))
                    continue;

                string value = (t.text ?? string.Empty).ToUpperInvariant();
                if (value.Contains("TICKETS") && !value.Contains("JACKPOT"))
                {
                    if (ticketResult == null || t.fontSize > ticketResult.fontSize)
                        ticketResult = t;
                }

                if (value.Contains("ENTER OR P") || value.Contains("PLAY AGAIN"))
                {
                    if (replayPrompt == null || t.fontSize > replayPrompt.fontSize)
                        replayPrompt = t;
                }
            }
        }

        private IEnumerator WatchTicketCountAndCelebrate()
        {
            if (ticketResult == null)
                yield break;

            Vector3 baseScale = ticketResult.transform.localScale;
            string previous = ticketResult.text;
            float stable = 0f;
            float timeout = 5f;

            while (timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                string current = ticketResult.text;

                if (current != previous)
                {
                    previous = current;
                    stable = 0f;
                    ticketResult.transform.localScale = baseScale * 1.035f;
                }
                else
                {
                    stable += Time.unscaledDeltaTime;
                    ticketResult.transform.localScale = Vector3.Lerp(ticketResult.transform.localScale, baseScale, Time.unscaledDeltaTime * 12f);
                }

                if (stable >= 0.38f && current.ToUpperInvariant().Contains("TICKETS"))
                    break;

                yield return null;
            }

            ticketResult.transform.localScale = baseScale;
            yield return CelebrateFinalTicketValue(baseScale);
        }

        private IEnumerator CelebrateFinalTicketValue(Vector3 baseScale)
        {
            SpawnConfetti(34);

            float elapsed = 0f;
            const float duration = 1.05f;
            Color baseColor = ticketResult.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float bounce = Mathf.Sin(t * Mathf.PI);
                ticketResult.transform.localScale = baseScale * (1f + bounce * 0.18f);
                ticketResult.color = Color.Lerp(gold, baseColor, t);

                if (ticketHalo != null)
                    ticketHalo.localScale = Vector3.one * (1f + bounce * 0.07f);
                if (ticketHaloImage != null)
                    ticketHaloImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.30f, 0.10f, t));

                yield return null;
            }

            ticketResult.transform.localScale = baseScale;
            ticketResult.color = baseColor;
            if (ticketHalo != null)
                ticketHalo.localScale = Vector3.one;
        }

        private IEnumerator PulseReplayPrompt()
        {
            Vector3 baseScale = replayPrompt.transform.localScale;
            while (replayPrompt != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.3f);
                replayPrompt.transform.localScale = baseScale * Mathf.Lerp(1f, 1.055f, pulse);
                replayPrompt.color = Color.Lerp(cyan, Color.white, pulse * 0.55f);
                yield return null;
            }
        }

        private void SpawnConfetti(int count)
        {
            if (frontRoot == null)
                return;

            for (int i = 0; i < count; i++)
                activeSparks.Add(StartCoroutine(ConfettiRoutine(i)));
        }

        private IEnumerator ConfettiRoutine(int index)
        {
            GameObject go = new GameObject("BRFX_Confetti_" + index, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(frontRoot, false);
            rt.anchorMin = new Vector2(Random.Range(0.20f, 0.80f), Random.Range(0.55f, 0.76f));
            rt.anchorMax = rt.anchorMin;
            rt.sizeDelta = new Vector2(Random.Range(7f, 15f), Random.Range(13f, 25f));

            Image img = go.GetComponent<Image>();
            img.sprite = sparkSprite;
            img.type = Image.Type.Sliced;
            Color[] colors = { cyan, magenta, gold, green, Color.white };
            Color color = colors[index % colors.Length];
            img.color = color;
            img.raycastTarget = false;

            Vector2 velocity = new Vector2(Random.Range(-170f, 170f), Random.Range(120f, 300f));
            float gravity = Random.Range(260f, 420f);
            float duration = Random.Range(0.9f, 1.5f);
            float elapsed = 0f;
            Vector2 position = Vector2.zero;

            while (elapsed < duration)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                velocity.y -= gravity * dt;
                position += velocity * dt;
                rt.anchoredPosition = position;
                rt.localRotation = Quaternion.Euler(0f, 0f, elapsed * (150f + index * 7f));
                float alpha = 1f - Mathf.Clamp01((elapsed - duration * 0.62f) / (duration * 0.38f));
                img.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            Destroy(go);
        }

        private bool IsOurDecor(Transform t)
        {
            return (backRoot != null && t.IsChildOf(backRoot)) || (frontRoot != null && t.IsChildOf(frontRoot));
        }

        private void DestroyOld(string objectName)
        {
            Transform t = canvas.transform.Find(objectName);
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

        private static void CreateSolid(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, min, max);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
