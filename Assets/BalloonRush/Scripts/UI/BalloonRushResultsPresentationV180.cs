using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Presentation-only polish for Results. Does not change scores, tickets,
    /// credits, replay logic, or payout behavior.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class BalloonRushResultsPresentationV180 : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform decorRoot;
        private TMP_Text ticketResult;
        private Color cyan = new Color32(0, 226, 255, 255);
        private Color magenta = new Color32(255, 35, 183, 255);
        private Color gold = new Color32(255, 193, 28, 255);
        private Color deep = new Color32(2, 9, 28, 255);
        private Sprite panel;
        private Sprite goldPanel;
        private Sprite tile;

        private IEnumerator Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                yield break;

            BuildSprites();
            BuildDecor();

            // ResultsManager may populate values on Start; wait two frames.
            yield return null;
            yield return null;

            StyleExistingResults();
            FindTicketResult();
            if (ticketResult != null)
                yield return AnimateTicketResult();
        }

        private void BuildSprites()
        {
            panel = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(4, 22, 52, 245), cyan, 128, 30, 5, 28);
            goldPanel = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(80, 50, 2, 245), gold, 128, 34, 5, 30);
            tile = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(14, 30, 68, 235), magenta, 96, 26, 4, 24);
        }

        private void BuildDecor()
        {
            Transform old = canvas.transform.Find("BRUI_ResultsDecor");
            if (old != null)
                Destroy(old.gameObject);

            GameObject root = new GameObject("BRUI_ResultsDecor", typeof(RectTransform), typeof(CanvasGroup));
            decorRoot = root.GetComponent<RectTransform>();
            decorRoot.SetParent(canvas.transform, false);
            SetAnchors(decorRoot, Vector2.zero, Vector2.one);
            decorRoot.SetAsFirstSibling();

            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            CreateSolid("BRUI_ResultLeftGlow", decorRoot, new Vector2(0.005f, 0f), new Vector2(0.015f, 1f), magenta);
            CreateSolid("BRUI_ResultRightGlow", decorRoot, new Vector2(0.985f, 0f), new Vector2(0.995f, 1f), cyan);

            // Soft celebratory rays behind the existing result UI.
            for (int i = 0; i < 14; i++)
            {
                float x = 0.08f + i * 0.065f;
                RectTransform ray = CreatePanel(
                    "BRUI_ResultRay_" + i,
                    decorRoot,
                    new Vector2(x, 0.15f),
                    new Vector2(Mathf.Min(0.98f, x + 0.010f), 0.92f),
                    tile,
                    i % 2 == 0 ? new Color(cyan.r, cyan.g, cyan.b, 0.025f) : new Color(magenta.r, magenta.g, magenta.b, 0.025f));
                ray.localRotation = Quaternion.Euler(0f, 0f, (i - 6.5f) * 2.0f);
            }

            // Gold halo behind the main ticket result region.
            RectTransform halo = CreatePanel(
                "BRUI_ResultHalo",
                decorRoot,
                new Vector2(0.18f, 0.53f),
                new Vector2(0.82f, 0.78f),
                goldPanel,
                new Color(1f, 1f, 1f, 0.10f));
            halo.localScale = Vector3.one * 1.02f;
        }

        private void StyleExistingResults()
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t == null || t.transform.IsChildOf(decorRoot))
                    continue;

                string value = (t.text ?? string.Empty).ToUpperInvariant();
                t.textWrappingMode = TextWrappingModes.NoWrap;

                if (value.Contains("BALLOON RUSH RESULTS"))
                {
                    t.fontStyle |= FontStyles.Bold;
                    t.fontSize = Mathf.Max(t.fontSize, 42f);
                    t.color = Color.white;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.14f);
                }
                else if (value.Contains("TICKETS"))
                {
                    t.fontStyle |= FontStyles.Bold;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.14f);
                }
                else if (value.Contains("PLAY AGAIN") || value.Contains("ENTER OR P"))
                {
                    t.fontStyle |= FontStyles.Bold;
                    t.color = cyan;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.10f);
                }
                else if (value.Contains("HIGH SCORE") || value.Contains("TOP SCORE"))
                {
                    t.color = gold;
                    t.fontStyle |= FontStyles.Bold;
                }
            }

            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img == null || img.transform.IsChildOf(decorRoot))
                    continue;

                string n = img.name.ToLowerInvariant();
                if (n.Contains("ticket") || n.Contains("result"))
                {
                    img.sprite = goldPanel;
                    img.type = Image.Type.Sliced;
                }
                else if (n.Contains("summary") || n.Contains("replay") || n.Contains("play again"))
                {
                    img.sprite = panel;
                    img.type = Image.Type.Sliced;
                }
            }
        }

        private void FindTicketResult()
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t == null || t.transform.IsChildOf(decorRoot))
                    continue;

                string value = (t.text ?? string.Empty).ToUpperInvariant();
                if (value.Contains("TICKETS") && !value.Contains("JACKPOT"))
                {
                    if (ticketResult == null || t.fontSize > ticketResult.fontSize)
                        ticketResult = t;
                }
            }
        }

        private IEnumerator AnimateTicketResult()
        {
            Vector3 baseScale = ticketResult.transform.localScale;
            Color original = ticketResult.color;
            float duration = 0.85f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
                ticketResult.transform.localScale = baseScale * bounce;
                ticketResult.color = Color.Lerp(gold, original, t);
                yield return null;
            }

            ticketResult.transform.localScale = baseScale;
            ticketResult.color = original;
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Sprite sprite, Color color)
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

        private void CreateSolid(string name, Transform parent, Vector2 min, Vector2 max, Color color)
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
