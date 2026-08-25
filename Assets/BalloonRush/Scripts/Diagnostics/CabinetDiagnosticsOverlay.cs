using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.Diagnostics
{
    /// <summary>
    /// Runtime service dashboard added automatically to the Operator Menu.
    /// The panel is intentionally independent of OperatorMenuManager so it cannot
    /// disturb cabinet settings, gameplay wiring, or the existing scene builder.
    /// </summary>
    public sealed class CabinetDiagnosticsOverlay : MonoBehaviour
    {
        private const string HostName = "Cabinet Diagnostics v1.9";

        private readonly Dictionary<string, TMP_Text> inputIndicators = new Dictionary<string, TMP_Text>(StringComparer.OrdinalIgnoreCase);

        private CabinetDiagnosticsService service;
        private GameObject panel;
        private Button openButton;
        private TMP_Text serialCard;
        private TMP_Text readerCard;
        private TMP_Text ticketCard;
        private TMP_Text payoutCard;
        private TMP_Text hardwareText;
        private TMP_Text economyText;
        private TMP_Text actionText;
        private float nextRefresh;
        private TMP_FontAsset font;
        private Sprite roundedDark;
        private Sprite roundedBlue;
        private Sprite roundedRed;
        private Sprite roundedGreen;
        private Sprite roundedGold;

        public static void EnsureInstalled()
        {
            if (FindFirstObjectByType<CabinetDiagnosticsOverlay>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            Canvas canvas = FindBestCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("Balloon Rush v1.9 diagnostics could not find the Operator Menu canvas.");
                return;
            }

            GameObject host = new GameObject(HostName, typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)host.transform;
            Stretch(rect, 0f, 0f, 0f, 0f);
            host.AddComponent<CabinetDiagnosticsOverlay>();
            host.transform.SetAsLastSibling();
        }

        private void Awake()
        {
            service = CabinetDiagnosticsService.Instance;
            font = TMP_Settings.defaultFontAsset;
            BuildSprites();
            BuildUI();
        }

        private void Update()
        {
            if (service == null)
            {
                service = CabinetDiagnosticsService.Instance;
            }

            if (service == null || Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + 0.15f;
            Refresh();
        }

        private void BuildSprites()
        {
            roundedDark = CreateRoundedSprite(new Color32(4, 14, 34, 248), new Color32(0, 220, 255, 255), 22, 4);
            roundedBlue = CreateRoundedSprite(new Color32(15, 69, 118, 255), new Color32(63, 225, 255, 255), 22, 4);
            roundedRed = CreateRoundedSprite(new Color32(120, 24, 35, 255), new Color32(255, 79, 90, 255), 22, 4);
            roundedGreen = CreateRoundedSprite(new Color32(14, 91, 55, 255), new Color32(70, 255, 150, 255), 22, 4);
            roundedGold = CreateRoundedSprite(new Color32(104, 65, 9, 255), new Color32(255, 198, 40, 255), 22, 4);
        }

        private void BuildUI()
        {
            // Small reopen button remains when diagnostics is closed.
            openButton = CreateButton(transform, "CABINET\nDIAGNOSTICS", roundedBlue, new Vector2(0.74f, 0.925f), new Vector2(0.985f, 0.992f));
            openButton.onClick.AddListener(() => SetPanelVisible(true));

            panel = new GameObject("Diagnostics Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0.018f, 0.018f);
            panelRect.anchorMax = new Vector2(0.982f, 0.982f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = roundedDark;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;

            CreateText(panel.transform, "CABINET DIAGNOSTICS", 40f, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.925f), new Vector2(0.74f, 0.985f), new Color32(250, 252, 255, 255));
            CreateText(panel.transform, "BALLOON RUSH  v" + CabinetDiagnosticsService.Version,
                19f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.055f, 0.895f), new Vector2(0.62f, 0.932f), new Color32(72, 225, 255, 255));

            Button close = CreateButton(panel.transform, "CLOSE\nDIAGNOSTICS", roundedRed,
                new Vector2(0.75f, 0.918f), new Vector2(0.965f, 0.977f));
            close.onClick.AddListener(() => SetPanelVisible(false));

            serialCard = CreateStatusCard(panel.transform, "CABINET SERIAL", new Vector2(0.035f, 0.805f), new Vector2(0.265f, 0.895f));
            readerCard = CreateStatusCard(panel.transform, "CARD READER", new Vector2(0.275f, 0.805f), new Vector2(0.505f, 0.895f));
            ticketCard = CreateStatusCard(panel.transform, "TICKET CTRL", new Vector2(0.515f, 0.805f), new Vector2(0.745f, 0.895f));
            payoutCard = CreateStatusCard(panel.transform, "PAYOUT", new Vector2(0.755f, 0.805f), new Vector2(0.965f, 0.895f));

            CreateSectionLabel(panel.transform, "LIVE HARDWARE", 0.772f, 0.802f);
            hardwareText = CreatePanelText(panel.transform, new Vector2(0.035f, 0.535f), new Vector2(0.965f, 0.772f), 19f);

            CreateSectionLabel(panel.transform, "LIVE CABINET INPUTS", 0.505f, 0.535f);
            BuildInputIndicators(panel.transform);

            CreateSectionLabel(panel.transform, "SERVICE TESTS", 0.405f, 0.438f);
            BuildTestButtons(panel.transform);

            CreateSectionLabel(panel.transform, "ECONOMY + SOAK DATA", 0.365f, 0.398f);
            economyText = CreatePanelText(panel.transform, new Vector2(0.035f, 0.095f), new Vector2(0.965f, 0.365f), 19f);

            actionText = CreateText(panel.transform, "Ready", 18f, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.082f), new Color32(115, 239, 255, 255));
            actionText.enableAutoSizing = true;
            actionText.fontSizeMin = 12f;
            actionText.fontSizeMax = 18f;

            SetPanelVisible(true);
        }

        private void BuildInputIndicators(Transform parent)
        {
            string[] names = { "LEFT", "POP", "RIGHT", "START", "CARD", "COIN", "OPERATOR", "BACK" };
            float x0 = 0.035f;
            float x1 = 0.965f;
            float y0 = 0.445f;
            float y1 = 0.505f;
            float gap = 0.008f;
            float total = x1 - x0;
            float width = (total - gap * (names.Length - 1)) / names.Length;

            for (int i = 0; i < names.Length; i++)
            {
                float left = x0 + i * (width + gap);
                GameObject card = CreatePanel(parent, "Input " + names[i], roundedDark,
                    new Vector2(left, y0), new Vector2(left + width, y1));
                TMP_Text text = CreateText(card.transform, names[i], 17f, FontStyles.Bold, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, new Color32(120, 140, 160, 255));
                inputIndicators[names[i]] = text;
            }
        }

        private void BuildTestButtons(Transform parent)
        {
            float y0 = 0.372f;
            float y1 = 0.432f;
            float left = 0.035f;
            float right = 0.965f;
            float gap = 0.009f;
            string[] labels =
            {
                "+1 TEST\nCREDIT",
                "1 TICKET\nTEST",
                "5 TICKET\nTEST",
                "10 TICKET\nTEST",
                "RECONNECT\nSERIAL",
                "PING\nCONTROLLER",
                "CLEAR\nERROR DISPLAY"
            };

            float width = (right - left - gap * (labels.Length - 1)) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                float x = left + i * (width + gap);
                Sprite sprite = i == 0 ? roundedBlue : (i >= 1 && i <= 3 ? roundedGold : (i == 6 ? roundedRed : roundedGreen));
                Button button = CreateButton(parent, labels[i], sprite, new Vector2(x, y0), new Vector2(x + width, y1));
                int captured = i;
                button.onClick.AddListener(() => HandleTestButton(captured));
            }
        }

        private void HandleTestButton(int index)
        {
            if (service == null)
            {
                return;
            }

            switch (index)
            {
                case 0:
                    service.AddTestCredit();
                    break;
                case 1:
                    service.QueueTicketTest(1);
                    break;
                case 2:
                    service.QueueTicketTest(5);
                    break;
                case 3:
                    service.QueueTicketTest(10);
                    break;
                case 4:
                    service.ReconnectSerial();
                    break;
                case 5:
                    service.PingSerial();
                    break;
                case 6:
                    service.ClearDiagnosticErrors();
                    break;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (service == null)
            {
                return;
            }

            SetCard(serialCard, "CABINET SERIAL", service.GetSerialStatus(), service.SerialConnected, service.HardwareEnabled);
            SetCard(readerCard, "CARD READER", service.GetCardReaderStatus(), service.SerialConnected, service.HardwareEnabled);
            bool ticketOk = service.SerialConnected && !service.HasPayoutFault;
            SetCard(ticketCard, "TICKET CTRL", service.GetTicketControllerStatus(), ticketOk, service.HardwareEnabled);
            bool payoutOk = !service.HasPayoutFault;
            SetCard(payoutCard, "PAYOUT", service.GetPayoutStatus(), payoutOk, true);

            if (hardwareText != null)
            {
                hardwareText.text = service.GetHardwareDetailsText();
            }
            if (economyText != null)
            {
                economyText.text = service.GetEconomyText();
            }
            if (actionText != null)
            {
                actionText.text = service.LastActionStatus;
                actionText.color = string.IsNullOrWhiteSpace(service.LastHardwareError)
                    ? new Color32(100, 244, 255, 255)
                    : new Color32(255, 110, 100, 255);
            }

            foreach (KeyValuePair<string, TMP_Text> pair in inputIndicators)
            {
                bool active = service.WasInputRecent(pair.Key);
                pair.Value.color = active ? new Color32(75, 255, 135, 255) : new Color32(100, 125, 150, 255);
                pair.Value.text = active ? pair.Key + "\nOK" : pair.Key;
            }
        }

        private static void SetCard(TMP_Text text, string heading, string status, bool ok, bool enabled)
        {
            if (text == null)
            {
                return;
            }

            text.text = heading + "\n" + status;
            if (!enabled)
            {
                text.color = new Color32(160, 165, 180, 255);
            }
            else
            {
                text.color = ok ? new Color32(80, 255, 145, 255) : new Color32(255, 95, 85, 255);
            }
        }

        private TMP_Text CreateStatusCard(Transform parent, string heading, Vector2 min, Vector2 max)
        {
            GameObject card = CreatePanel(parent, heading, roundedDark, min, max);
            TMP_Text text = CreateText(card.transform, heading + "\nWAITING", 18f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0.035f, 0.08f), new Vector2(0.965f, 0.92f), Color.white);
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 18f;
            return text;
        }

        private TMP_Text CreatePanelText(Transform parent, Vector2 min, Vector2 max, float size)
        {
            GameObject card = CreatePanel(parent, "Panel", roundedDark, min, max);
            TMP_Text text = CreateText(card.transform, string.Empty, size, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, new Vector2(0.025f, 0.045f), new Vector2(0.975f, 0.955f), new Color32(235, 244, 255, 255));
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = size;
            return text;
        }

        private void CreateSectionLabel(Transform parent, string label, float minY, float maxY)
        {
            CreateText(parent, label, 21f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.04f, minY), new Vector2(0.96f, maxY), new Color32(255, 202, 55, 255));
        }

        private GameObject CreatePanel(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            return obj;
        }

        private Button CreateButton(Transform parent, string label, Sprite sprite, Vector2 min, Vector2 max)
        {
            GameObject obj = CreatePanel(parent, label.Replace("\n", " "), sprite, min, max);
            Button button = obj.AddComponent<Button>();
            button.targetGraphic = obj.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.88f, 0.95f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            TMP_Text text = CreateText(obj.transform, label, 16f, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), Color.white);
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 16f;
            return button;
        }

        private TMP_Text CreateText(Transform parent, string value, float size, FontStyles style,
            TextAlignmentOptions alignment, Vector2 min, Vector2 max, Color color)
        {
            GameObject obj = new GameObject("Text", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private void SetPanelVisible(bool visible)
        {
            if (panel != null)
            {
                panel.SetActive(visible);
                if (visible)
                {
                    panel.transform.SetAsLastSibling();
                }
            }
            if (openButton != null)
            {
                openButton.gameObject.SetActive(!visible);
            }
        }

        private static Canvas FindBestCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas best = null;
            int bestOrder = int.MinValue;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null)
                {
                    continue;
                }
                if (candidate.sortingOrder >= bestOrder)
                {
                    bestOrder = candidate.sortingOrder;
                    best = candidate;
                }
            }
            return best;
        }

        private static Sprite CreateRoundedSprite(Color fill, Color border, int radius, int borderPixels)
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "DiagnosticsRounded"
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool outer = InRoundedRect(x, y, size, size, radius);
                    bool inner = InRoundedRect(x - borderPixels, y - borderPixels,
                        size - borderPixels * 2, size - borderPixels * 2, Mathf.Max(1, radius - borderPixels));
                    texture.SetPixel(x, y, !outer ? Color.clear : inner ? fill : border);
                }
            }
            texture.Apply();

            int slice = Mathf.Max(radius + 2, 18);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        }

        private static bool InRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (width <= 0 || height <= 0 || x < 0 || y < 0 || x >= width || y >= height)
            {
                return false;
            }

            int left = radius;
            int right = width - radius - 1;
            int bottom = radius;
            int top = height - radius - 1;
            if (x >= left && x <= right) return true;
            if (y >= bottom && y <= top) return true;

            int cx = x < left ? left : right;
            int cy = y < bottom ? bottom : top;
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
