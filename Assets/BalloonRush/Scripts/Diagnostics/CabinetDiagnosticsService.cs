using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.Input;
using BalloonRush.Redemption;
using BalloonRush.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Diagnostics
{
    /// <summary>
    /// Persistent cabinet/service diagnostics. This class is additive and does not
    /// participate in gameplay, score, ticket math, or credit pricing.
    /// </summary>
    [DefaultExecutionOrder(-850)]
    public sealed class CabinetDiagnosticsService : MonoBehaviour
    {
        public const string Version = "1.9.0";
        private const string DiagnosticsFileName = "cabinet-diagnostics.csv";
        private const string DiagnosticsHeader = "utc,event,details";

        private readonly Dictionary<string, float> lastInputTimes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private ArcadeInputManager input;
        private CreditManager credits;
        private TicketManager tickets;
        private SaveManager save;
        private SettingsManager settings;
        private SerialArcadeIO serial;
        private bool bound;
        private bool lastSerialAvailable;
        private bool connectionStateInitialized;
        private float nextAuditRefreshTime;
        private SessionAuditSummary auditSummary = new SessionAuditSummary();

        public static CabinetDiagnosticsService Instance { get; private set; }

        public string LastReaderMessage { get; private set; } = "No reader message yet";
        public string LastReaderUtc { get; private set; } = "-";
        public string LastTicketCommand { get; private set; } = "No ticket command yet";
        public string LastPaidMessage { get; private set; } = "No PAID acknowledgement yet";
        public string LastHardwareError { get; private set; } = string.Empty;
        public string LastHardwareEvent { get; private set; } = "Diagnostics started";
        public string LastActionStatus { get; private set; } = "Ready";

        public bool IsBound => bound;
        public bool HardwareEnabled => settings != null && settings.Current != null && settings.Current.hardwareEnabled;
        public bool SerialConnected => serial != null && serial.IsAvailable;
        public string PortName => serial != null ? serial.PortName : (settings != null && settings.Current != null ? settings.Current.serialPort : "-");
        public int BaudRate => serial != null ? serial.BaudRate : (settings != null && settings.Current != null ? settings.Current.baudRate : 0);
        public int CurrentCredits => credits != null ? credits.Credits : 0;
        public bool HasPayoutFault => tickets != null && tickets.HasPayoutFault;
        public bool IsDispensing => tickets != null && tickets.IsDispensing;
        public int TicketsRemaining => tickets != null ? tickets.TicketsRemaining : 0;
        public string ActiveTransactionId => tickets != null ? tickets.ActiveTransactionId : string.Empty;
        public string TicketFault => tickets != null ? tickets.LastFaultMessage : string.Empty;
        public string DiagnosticsAuditPath => Path.Combine(GetAuditDirectory(), DiagnosticsFileName);
        public string SessionAuditPath => GameServices.Audit != null ? GameServices.Audit.AuditPath : Path.Combine(GetAuditDirectory(), "sessions.csv");
        public string RuntimeLogPath => GameServices.Cabinet != null ? GameServices.Cabinet.RuntimeLogPath : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateAutomatically()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject host = new GameObject("BalloonRush Cabinet Diagnostics");
            host.AddComponent<CabinetDiagnosticsService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            StartCoroutine(BindWhenReady());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Unbind();
        }

        private IEnumerator BindWhenReady()
        {
            while (!GameServices.IsReady)
            {
                yield return null;
            }

            Bind();
            TryInstallOperatorOverlay(SceneManager.GetActiveScene());
        }

        private void Update()
        {
            if (!bound && GameServices.IsReady)
            {
                Bind();
            }

            if (!bound)
            {
                return;
            }

            RefreshSerialReference();
            bool available = SerialConnected;
            if (!connectionStateInitialized || available != lastSerialAvailable)
            {
                connectionStateInitialized = true;
                lastSerialAvailable = available;
                LastHardwareEvent = available
                    ? $"Serial connected: {PortName} @ {BaudRate}"
                    : HardwareEnabled ? $"Serial disconnected: {PortName}" : "Serial hardware disabled";
                AppendDiagnosticEvent(available ? "SERIAL_CONNECTED" : "SERIAL_DISCONNECTED", LastHardwareEvent);
            }

            if (Time.unscaledTime >= nextAuditRefreshTime)
            {
                nextAuditRefreshTime = Time.unscaledTime + 5f;
                RefreshSessionAuditSummary();
            }
        }

        public bool WasInputRecent(string inputName, float seconds = 0.85f)
        {
            return lastInputTimes.TryGetValue(inputName, out float time) && Time.unscaledTime - time <= seconds;
        }

        public string GetSerialStatus()
        {
            if (!HardwareEnabled)
            {
                return "DISABLED";
            }
            return SerialConnected ? "CONNECTED" : "DISCONNECTED";
        }

        public string GetCardReaderStatus()
        {
            if (!HardwareEnabled)
            {
                return "DISABLED";
            }
            return SerialConnected ? "CONNECTED (SHARED SERIAL)" : "DISCONNECTED";
        }

        public string GetTicketControllerStatus()
        {
            if (!HardwareEnabled)
            {
                return "DISABLED";
            }
            if (!SerialConnected)
            {
                return "DISCONNECTED";
            }
            if (HasPayoutFault)
            {
                return "FAULT - REVIEW REQUIRED";
            }
            if (IsDispensing)
            {
                return "BUSY / PAYING";
            }
            return "CONNECTED / READY";
        }

        public string GetPayoutStatus()
        {
            if (HasPayoutFault)
            {
                return "FAULT";
            }
            if (IsDispensing)
            {
                return $"PENDING {TicketsRemaining}";
            }
            return "IDLE";
        }

        public string GetHardwareDetailsText()
        {
            string transaction = string.IsNullOrWhiteSpace(ActiveTransactionId) ? "-" : ActiveTransactionId;
            string error = string.IsNullOrWhiteSpace(LastHardwareError) ? "NONE" : LastHardwareError;
            string fault = string.IsNullOrWhiteSpace(TicketFault) ? "NONE" : TicketFault;

            return
                $"PORT {PortName} @ {BaudRate}    HARDWARE {(HardwareEnabled ? "ENABLED" : "DISABLED")}    CREDITS {CurrentCredits}\n" +
                $"LAST READER: {LastReaderMessage}    UTC {LastReaderUtc}\n" +
                $"LAST TICKET CMD: {LastTicketCommand}\n" +
                $"LAST PAID: {LastPaidMessage}\n" +
                $"PENDING TICKETS: {TicketsRemaining}    ACTIVE TX: {transaction}\n" +
                $"TICKET FAULT: {fault}\n" +
                $"LAST HW ERROR: {error}\n" +
                $"LAST EVENT: {LastHardwareEvent}\n" +
                $"DIAGNOSTIC STATUS: {LastActionStatus}";
        }

        public string GetEconomyText()
        {
            MachineStatistics stats = save != null && save.Data != null ? save.Data.statistics : null;
            OperatorSettings current = settings != null ? settings.Current : null;
            float averageTickets = stats != null ? stats.AverageTicketsPerGame : 0f;
            float estimatedPercent = EconomyMath.EstimatePrizeCostPercent(averageTickets, current);
            float estimatedCostDollars = EconomyMath.EstimatePrizeCostCents(averageTickets, current) / 100f;

            long games = stats != null ? stats.gamesPlayed : auditSummary.Games;
            long swipes = stats != null ? stats.cardSwipes : 0;
            long awarded = stats != null ? stats.totalTicketsAwarded : 0;
            long paid = stats != null ? stats.totalTicketsPaid : 0;
            long failures = stats != null ? stats.ticketPayoutFailures : 0;
            long mismatches = stats != null ? stats.ticketPayoutMismatches : 0;
            float revenue = stats != null ? stats.totalRevenueCents / 100f : 0f;

            return
                $"GAMES {games:N0}    CARD SWIPES {swipes:N0}    REVENUE ${revenue:N2}\n" +
                $"TICKETS AWARDED {awarded:N0}    CONFIRMED PAID {paid:N0}    AVG {averageTickets:0.0}\n" +
                $"EST PRIZE COST / GAME ${estimatedCostDollars:0.000}    EST COST {estimatedPercent:0.0}%\n" +
                $"PAYOUT FAILURES {failures:N0}    MISMATCHES {mismatches:N0}    JACKPOTS {(stats != null ? stats.jackpotsWon : auditSummary.Jackpots):N0}\n" +
                $"AUDIT AVG SCORE {auditSummary.AverageScore:0}    HIGH SCORE {auditSummary.HighestScore:N0}\n" +
                $"AUDIT AVG COMBO x{auditSummary.AverageCombo:0.0}    HIGH COMBO x{auditSummary.HighestCombo}\n" +
                $"HIGHEST SESSION PAYOUT {auditSummary.HighestTickets} TICKETS\n" +
                $"TIMING TOTALS  PERFECT {auditSummary.Perfect:N0}  GREAT {auditSummary.Great:N0}  GOOD {auditSummary.Good:N0}  MISS {auditSummary.Miss:N0}  ACC {auditSummary.AccuracyPercent:0.0}%";
        }

        public bool AddTestCredit()
        {
            if (credits == null)
            {
                SetAction("CreditManager is unavailable.", true);
                return false;
            }

            credits.AddCredits(1, CreditPulseType.CardSwipe, false);
            LastReaderMessage = "OPERATOR TEST CREDIT (+1, no revenue)";
            LastReaderUtc = DateTime.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            PulseInput("CARD");
            SetAction("Added 1 test credit. Revenue statistics were NOT changed.", false);
            AppendDiagnosticEvent("TEST_CREDIT", "+1 test credit, revenue not recorded");
            return true;
        }

        public bool QueueTicketTest(int ticketCount)
        {
            ticketCount = Mathf.Clamp(ticketCount, 1, 10);
            if (tickets == null)
            {
                SetAction("TicketManager is unavailable.", true);
                return false;
            }
            if (!HardwareEnabled)
            {
                SetAction("Hardware is disabled. Enable Serial hardware in Operator Settings first.", true);
                return false;
            }
            if (!SerialConnected)
            {
                SetAction($"Cannot test tickets: {PortName} is disconnected.", true);
                return false;
            }
            if (tickets.HasPayoutFault)
            {
                SetAction("Existing payout fault must be reviewed first. Use the normal TEST TICKETS fault-review control.", true);
                return false;
            }
            if (tickets.IsDispensing)
            {
                SetAction("Ticket system is busy. Wait for the current payout to finish.", true);
                return false;
            }

            string transactionId = $"diagnostic-{ticketCount}-{Guid.NewGuid():N}";
            tickets.DispenseTickets(ticketCount, transactionId);
            SetAction($"Queued physical {ticketCount}-ticket diagnostic payout. Verify PAID:{ticketCount}.", false);
            AppendDiagnosticEvent("TEST_TICKETS", $"requested={ticketCount}, transaction={transactionId}");
            return true;
        }

        public bool ReconnectSerial()
        {
            RefreshSerialReference();
            if (serial == null)
            {
                SetAction("SerialArcadeIO was not found.", true);
                return false;
            }
            if (!HardwareEnabled)
            {
                SetAction("Hardware is disabled in Operator Settings.", true);
                return false;
            }

            serial.StopIO();
            serial.StartIO();
            connectionStateInitialized = false;
            SetAction($"Reconnect requested for {serial.PortName} @ {serial.BaudRate}.", false);
            AppendDiagnosticEvent("SERIAL_RECONNECT", $"port={serial.PortName}, baud={serial.BaudRate}");
            return true;
        }

        public bool PingSerial()
        {
            RefreshSerialReference();
            if (serial == null || !HardwareEnabled)
            {
                SetAction("Cannot send PING: serial hardware is unavailable or disabled.", true);
                return false;
            }
            if (!serial.IsAvailable)
            {
                SetAction("Cannot send PING: serial port is not open.", true);
                return false;
            }

            serial.SendRawCommand("PING");
            SetAction("PING command queued. Serial open-state is shown above.", false);
            AppendDiagnosticEvent("PING", $"port={serial.PortName}");
            return true;
        }

        public void ClearDiagnosticErrors()
        {
            LastHardwareError = string.Empty;
            LastActionStatus = "Diagnostic error display cleared. Ticket payout faults were NOT cleared.";
            AppendDiagnosticEvent("CLEAR_DIAGNOSTIC_ERRORS", "Display-only error state cleared; payout state unchanged");
        }

        private void Bind()
        {
            if (bound || !GameServices.IsReady)
            {
                return;
            }

            input = GameServices.Input;
            credits = GameServices.Credits;
            tickets = GameServices.Tickets;
            save = GameServices.Save;
            settings = GameServices.Settings;
            RefreshSerialReference();

            if (input != null)
            {
                input.LeftPressed += HandleLeft;
                input.RightPressed += HandleRight;
                input.PopPressed += HandlePop;
                input.StartPressed += HandleStart;
                input.OperatorPressed += HandleOperator;
                input.BackPressed += HandleBack;
                input.CreditPulse += HandleCredit;
                input.TicketsPaid += HandleTicketsPaid;
                input.TicketPayoutTimedOut += HandleTicketTimeout;
                input.HardwareError += HandleHardwareError;
            }

            if (tickets != null)
            {
                tickets.TicketPulseSent += HandleTicketPulseSent;
                tickets.PayoutQueued += HandlePayoutQueued;
                tickets.PayoutCompleted += HandlePayoutCompleted;
                tickets.PayoutFailed += HandlePayoutFailed;
            }

            bound = true;
            RefreshSessionAuditSummary();
            AppendDiagnosticEvent("DIAGNOSTICS_BOUND", $"version={Version}");
        }

        private void Unbind()
        {
            if (input != null)
            {
                input.LeftPressed -= HandleLeft;
                input.RightPressed -= HandleRight;
                input.PopPressed -= HandlePop;
                input.StartPressed -= HandleStart;
                input.OperatorPressed -= HandleOperator;
                input.BackPressed -= HandleBack;
                input.CreditPulse -= HandleCredit;
                input.TicketsPaid -= HandleTicketsPaid;
                input.TicketPayoutTimedOut -= HandleTicketTimeout;
                input.HardwareError -= HandleHardwareError;
            }

            if (tickets != null)
            {
                tickets.TicketPulseSent -= HandleTicketPulseSent;
                tickets.PayoutQueued -= HandlePayoutQueued;
                tickets.PayoutCompleted -= HandlePayoutCompleted;
                tickets.PayoutFailed -= HandlePayoutFailed;
            }

            bound = false;
        }

        private void RefreshSerialReference()
        {
            if (serial != null || input == null)
            {
                return;
            }

            IReadOnlyList<IArcadeIO> sources = input.Sources;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] is SerialArcadeIO candidate)
                {
                    serial = candidate;
                    break;
                }
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstallOperatorOverlay(scene);
        }

        private static void TryInstallOperatorOverlay(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.name, GameBootstrap.OperatorSceneName, StringComparison.Ordinal))
            {
                return;
            }

            CabinetDiagnosticsOverlay.EnsureInstalled();
        }

        private void HandleLeft() => PulseInput("LEFT");
        private void HandleRight() => PulseInput("RIGHT");
        private void HandlePop() => PulseInput("POP");
        private void HandleStart() => PulseInput("START");
        private void HandleOperator() => PulseInput("OPERATOR");
        private void HandleBack() => PulseInput("BACK");

        private void HandleCredit(CreditPulseType type)
        {
            string raw = type == CreditPulseType.CardSwipe ? "READER_CREDIT / CARD SWIPE" : "COIN";
            LastReaderMessage = raw;
            LastReaderUtc = DateTime.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            PulseInput(type == CreditPulseType.CardSwipe ? "CARD" : "COIN");
            AppendDiagnosticEvent("CREDIT_PULSE", raw);
        }

        private void HandleTicketsPaid(int count)
        {
            LastPaidMessage = $"PAID:{Mathf.Max(0, count)} @ {DateTime.UtcNow:HH:mm:ss} UTC";
            LastHardwareEvent = LastPaidMessage;
            AppendDiagnosticEvent("PAID", $"count={Mathf.Max(0, count)}");
        }

        private void HandleTicketTimeout(int count)
        {
            LastHardwareError = $"PAID_TIMEOUT:{Mathf.Max(0, count)}";
            LastHardwareEvent = LastHardwareError;
            AppendDiagnosticEvent("PAID_TIMEOUT", $"count={Mathf.Max(0, count)}");
        }

        private void HandleHardwareError(string error)
        {
            LastHardwareError = string.IsNullOrWhiteSpace(error) ? "Unknown serial hardware error" : error.Trim();
            LastHardwareEvent = LastHardwareError;
            AppendDiagnosticEvent("HARDWARE_ERROR", LastHardwareError);
        }

        private void HandleTicketPulseSent(int count)
        {
            LastTicketCommand = $"TICKETS:{Mathf.Max(0, count)} @ {DateTime.UtcNow:HH:mm:ss} UTC";
            LastHardwareEvent = LastTicketCommand;
            AppendDiagnosticEvent("TICKET_COMMAND", $"TICKETS:{Mathf.Max(0, count)}");
        }

        private void HandlePayoutQueued(string transactionId, int count)
        {
            LastHardwareEvent = $"Payout queued: {count} tickets";
            AppendDiagnosticEvent("PAYOUT_QUEUED", $"transaction={transactionId}, requested={count}");
        }

        private void HandlePayoutCompleted(string transactionId, int requested, int paid, bool verified)
        {
            LastPaidMessage = $"PAID:{paid} / requested {requested} / {(verified ? "VERIFIED" : "MISMATCH")}";
            LastHardwareEvent = LastPaidMessage;
            AppendDiagnosticEvent("PAYOUT_COMPLETED", $"transaction={transactionId}, requested={requested}, paid={paid}, verified={(verified ? 1 : 0)}");
        }

        private void HandlePayoutFailed(string transactionId, int requested, string reason)
        {
            LastHardwareError = reason ?? "Unknown payout fault";
            LastHardwareEvent = $"Payout failed: {requested} tickets";
            AppendDiagnosticEvent("PAYOUT_FAILED", $"transaction={transactionId}, requested={requested}, reason={LastHardwareError}");
        }

        private void PulseInput(string inputName)
        {
            lastInputTimes[inputName] = Time.unscaledTime;
        }

        private void SetAction(string message, bool isError)
        {
            LastActionStatus = message ?? string.Empty;
            if (isError)
            {
                LastHardwareError = LastActionStatus;
            }
        }

        private string GetAuditDirectory()
        {
            if (GameServices.Audit != null)
            {
                return GameServices.Audit.AuditDirectory;
            }
            return Path.Combine(Application.persistentDataPath, "BalloonRushAudit");
        }

        private void AppendDiagnosticEvent(string eventName, string details)
        {
            try
            {
                string directory = GetAuditDirectory();
                Directory.CreateDirectory(directory);
                string path = DiagnosticsAuditPath;
                bool writeHeader = !File.Exists(path) || new FileInfo(path).Length == 0;
                using (StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(false)))
                {
                    if (writeHeader)
                    {
                        writer.WriteLine(DiagnosticsHeader);
                    }
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
                        Csv(eventName),
                        Csv(details)
                    }));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Balloon Rush diagnostics audit write failed: {exception.Message}");
            }
        }

        private void RefreshSessionAuditSummary()
        {
            string path = SessionAuditPath;
            SessionAuditSummary summary = new SessionAuditSummary();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                auditSummary = summary;
                return;
            }

            try
            {
                using (StreamReader reader = new StreamReader(path, Encoding.UTF8, true))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(headerLine))
                    {
                        auditSummary = summary;
                        return;
                    }

                    List<string> header = ParseCsvLine(headerLine);
                    int scoreIndex = header.IndexOf("score");
                    int ticketsIndex = header.IndexOf("tickets_total");
                    int comboIndex = header.IndexOf("highest_combo");
                    int perfectIndex = header.IndexOf("perfect");
                    int greatIndex = header.IndexOf("great");
                    int goodIndex = header.IndexOf("good");
                    int missIndex = header.IndexOf("misses");
                    int jackpotIndex = header.IndexOf("jackpot_won");

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        List<string> values = ParseCsvLine(line);
                        int score = ParseInt(values, scoreIndex);
                        int ticketCount = ParseInt(values, ticketsIndex);
                        int combo = ParseInt(values, comboIndex);
                        int perfect = ParseInt(values, perfectIndex);
                        int great = ParseInt(values, greatIndex);
                        int good = ParseInt(values, goodIndex);
                        int miss = ParseInt(values, missIndex);
                        int jackpot = ParseInt(values, jackpotIndex);

                        summary.Games++;
                        summary.TotalScore += Math.Max(0, score);
                        summary.TotalCombo += Math.Max(0, combo);
                        summary.HighestScore = Math.Max(summary.HighestScore, score);
                        summary.HighestTickets = Math.Max(summary.HighestTickets, ticketCount);
                        summary.HighestCombo = Math.Max(summary.HighestCombo, combo);
                        summary.Perfect += Math.Max(0, perfect);
                        summary.Great += Math.Max(0, great);
                        summary.Good += Math.Max(0, good);
                        summary.Miss += Math.Max(0, miss);
                        summary.Jackpots += jackpot > 0 ? 1 : 0;
                    }
                }
            }
            catch (Exception exception)
            {
                LastHardwareError = $"Could not read session audit: {exception.Message}";
            }

            auditSummary = summary;
        }

        private static int ParseInt(List<string> values, int index)
        {
            if (index < 0 || index >= values.Count)
            {
                return 0;
            }
            return int.TryParse(values[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (c == ',' && !quoted)
                {
                    fields.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(c);
                }
            }
            fields.Add(current.ToString());
            return fields;
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private struct SessionAuditSummary
        {
            public long Games;
            public long TotalScore;
            public long TotalCombo;
            public int HighestScore;
            public int HighestTickets;
            public int HighestCombo;
            public long Perfect;
            public long Great;
            public long Good;
            public long Miss;
            public int Jackpots;

            public float AverageScore => Games > 0 ? (float)TotalScore / Games : 0f;
            public float AverageCombo => Games > 0 ? (float)TotalCombo / Games : 0f;
            public float AccuracyPercent
            {
                get
                {
                    long attempts = Perfect + Great + Good + Miss;
                    return attempts > 0 ? (Perfect + Great + Good) * 100f / attempts : 0f;
                }
            }
        }
    }
}
