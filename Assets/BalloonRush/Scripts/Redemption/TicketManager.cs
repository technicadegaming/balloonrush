using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BalloonRush.Input;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Redemption
{
    /// <summary>
    /// Queues final ticket awards, sends one TICKETS:n command, and verifies the
    /// dispenser's PAID:n acknowledgement. It never automatically retries a sent
    /// payout because a lost acknowledgement could otherwise double-pay tickets.
    /// </summary>
    public sealed class TicketManager : MonoBehaviour
    {
        private sealed class PayoutRequest
        {
            public string TransactionId;
            public int Tickets;
            public bool Sent;
            public string CreatedUtc;
        }

        private const string AuditFolder = "BalloonRushAudit";
        private const string AuditFile = "ticket-payouts.csv";
        private const string AuditHeader = "transaction_id,created_utc,completed_utc,requested,reported_paid,verified,hardware_enabled,status,details";

        private readonly Queue<PayoutRequest> pending = new Queue<PayoutRequest>();
        private readonly HashSet<string> acceptedTransactionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private SettingsManager settingsManager;
        private ArcadeInputManager inputManager;
        private SaveManager saveManager;
        private Coroutine processRoutine;
        private PayoutRequest activeRequest;
        private PayoutRequest failedRequest;
        private bool acknowledgementReceived;
        private int acknowledgedTickets;
        private bool hardwareTimeoutReceived;
        private int hardwareTimeoutTickets;

        public bool IsDispensing => processRoutine != null || activeRequest != null || pending.Count > 0;
        public bool HasPayoutFault => failedRequest != null;
        public bool CanRetryFailedPayout => failedRequest != null && !failedRequest.Sent;
        public int TicketsRemaining { get; private set; }
        public string LastFaultMessage { get; private set; } = string.Empty;
        public string ActiveTransactionId => activeRequest != null ? activeRequest.TransactionId : string.Empty;

        public event Action<int> TicketPulseSent;
        public event Action DispensingCompleted;
        public event Action<string, int> PayoutQueued;
        public event Action<string, int, int, bool> PayoutCompleted;
        public event Action<string, int, string> PayoutFailed;

        public void Initialize(SettingsManager settings, ArcadeInputManager input, SaveManager save)
        {
            UnsubscribeInput();
            settingsManager = settings;
            inputManager = input;
            saveManager = save;
            SubscribeInput();
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
        }

        public int ClampPayout(int requestedTickets)
        {
            int maximum = settingsManager != null && settingsManager.Current != null
                ? settingsManager.Current.maxTicketPayout
                : 625;
            return Mathf.Clamp(requestedTickets, 0, Mathf.Clamp(maximum, 1, 1000));
        }

        public void DispenseTickets(int requestedTickets)
        {
            DispenseTickets(requestedTickets, Guid.NewGuid().ToString("N"));
        }

        public void DispenseTickets(int requestedTickets, string transactionId)
        {
            int payout = ClampPayout(requestedTickets);
            if (payout <= 0)
            {
                return;
            }

            string normalizedId = string.IsNullOrWhiteSpace(transactionId)
                ? Guid.NewGuid().ToString("N")
                : transactionId.Trim();

            if (!acceptedTransactionIds.Add(normalizedId))
            {
                Debug.LogWarning($"Balloon Rush ignored duplicate ticket transaction {normalizedId}.");
                return;
            }

            PayoutRequest request = new PayoutRequest
            {
                TransactionId = normalizedId,
                Tickets = payout,
                Sent = false,
                CreatedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };

            pending.Enqueue(request);
            PayoutQueued?.Invoke(request.TransactionId, request.Tickets);
            UpdateTicketsRemaining();
            StartProcessingIfNeeded();
        }

        /// <summary>
        /// Retries only requests that were never transmitted. A request that was
        /// already sent must be reconciled manually to prevent a duplicate payout.
        /// </summary>
        public bool RetryFailedPayout()
        {
            if (!CanRetryFailedPayout)
            {
                return false;
            }

            PayoutRequest retry = failedRequest;
            failedRequest = null;
            LastFaultMessage = string.Empty;

            Queue<PayoutRequest> rebuilt = new Queue<PayoutRequest>();
            rebuilt.Enqueue(retry);
            while (pending.Count > 0)
            {
                rebuilt.Enqueue(pending.Dequeue());
            }
            while (rebuilt.Count > 0)
            {
                pending.Enqueue(rebuilt.Dequeue());
            }

            UpdateTicketsRemaining();
            StartProcessingIfNeeded();
            return true;
        }

        /// <summary>Clears an operator-reviewed fault and continues the queue.</summary>
        public bool DiscardFailedPayout()
        {
            if (failedRequest == null)
            {
                return false;
            }

            WriteAudit(failedRequest, 0, false, "DISCARDED", "Operator discarded failed payout after review.");
            failedRequest = null;
            LastFaultMessage = string.Empty;
            UpdateTicketsRemaining();
            StartProcessingIfNeeded();
            return true;
        }

        public void CancelDispensing()
        {
            // Never cancel a command that might already be dispensing physically.
            if (activeRequest != null && activeRequest.Sent)
            {
                Debug.LogWarning("Balloon Rush cannot cancel a ticket request after TICKETS:n was sent. Review the acknowledgement instead.");
                return;
            }

            if (processRoutine != null)
            {
                StopCoroutine(processRoutine);
                processRoutine = null;
            }
            activeRequest = null;
            pending.Clear();
            UpdateTicketsRemaining();
        }

        private void SubscribeInput()
        {
            if (inputManager == null)
            {
                return;
            }

            inputManager.TicketsPaid += HandleTicketsPaid;
            inputManager.TicketPayoutTimedOut += HandleHardwarePayoutTimeout;
            inputManager.HardwareError += HandleHardwareError;
        }

        private void UnsubscribeInput()
        {
            if (inputManager == null)
            {
                return;
            }

            inputManager.TicketsPaid -= HandleTicketsPaid;
            inputManager.TicketPayoutTimedOut -= HandleHardwarePayoutTimeout;
            inputManager.HardwareError -= HandleHardwareError;
        }

        private void StartProcessingIfNeeded()
        {
            if (processRoutine == null && failedRequest == null && pending.Count > 0)
            {
                processRoutine = StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (pending.Count > 0 && failedRequest == null)
            {
                activeRequest = pending.Dequeue();
                UpdateTicketsRemaining();
                OperatorSettings settings = settingsManager != null ? settingsManager.Current : null;
                bool hardwareEnabled = settings != null && settings.hardwareEnabled;

                if (!hardwareEnabled)
                {
                    // Development/free-standing mode: display the award but do not
                    // record it as physically paid by a dispenser.
                    WriteAudit(activeRequest, activeRequest.Tickets, true, "SIMULATED", "Hardware disabled; no physical ticket command sent.");
                    TicketPulseSent?.Invoke(activeRequest.Tickets);
                    PayoutCompleted?.Invoke(activeRequest.TransactionId, activeRequest.Tickets, activeRequest.Tickets, true);
                    activeRequest = null;
                    UpdateTicketsRemaining();
                    yield return null;
                    continue;
                }

                float hardwareWait = settings != null ? settings.ticketHardwareWaitTimeoutSeconds : 12f;
                float hardwareDeadline = Time.unscaledTime + Mathf.Clamp(hardwareWait, 1f, 120f);
                while (inputManager != null && !inputManager.TicketHardwareAvailable && Time.unscaledTime < hardwareDeadline)
                {
                    yield return null;
                }

                if (inputManager == null || !inputManager.TicketHardwareAvailable)
                {
                    FailActive("Ticket hardware was not available before the send timeout.", 0);
                    yield break;
                }

                ResetAcknowledgementState();
                if (!inputManager.TrySendTicketRequest(activeRequest.Tickets))
                {
                    FailActive("Ticket request could not be queued to the serial device.", 0);
                    yield break;
                }

                activeRequest.Sent = true;
                TicketPulseSent?.Invoke(activeRequest.Tickets);
                WriteAudit(activeRequest, 0, false, "REQUESTED", $"Sent TICKETS:{activeRequest.Tickets}; waiting for PAID:n.");

                float ackWait = settings != null ? settings.ticketPaidAckTimeoutSeconds : 30f;
                float ackDeadline = Time.unscaledTime + Mathf.Clamp(ackWait, 2f, 180f);
                while (!acknowledgementReceived && !hardwareTimeoutReceived && Time.unscaledTime < ackDeadline)
                {
                    yield return null;
                }

                if (acknowledgementReceived)
                {
                    bool verified = acknowledgedTickets == activeRequest.Tickets;
                    saveManager?.RecordTicketsPaid(activeRequest.Tickets, acknowledgedTickets, verified);
                    WriteAudit(
                        activeRequest,
                        acknowledgedTickets,
                        verified,
                        verified ? "PAID" : "MISMATCH",
                        verified ? "PAID acknowledgement matched request." : "PAID acknowledgement did not match request; manual review required.");

                    if (!verified)
                    {
                        saveManager?.RecordTicketPayoutFailure();
                        FailActive($"Ticket mismatch: requested {activeRequest.Tickets}, dispenser reported {acknowledgedTickets}. Do not retry automatically.", acknowledgedTickets, false);
                        yield break;
                    }

                    PayoutCompleted?.Invoke(activeRequest.TransactionId, activeRequest.Tickets, acknowledgedTickets, true);
                    activeRequest = null;
                    UpdateTicketsRemaining();
                    continue;
                }

                if (hardwareTimeoutReceived)
                {
                    saveManager?.RecordTicketPayoutFailure();
                    FailActive($"Dispenser reported PAID_TIMEOUT:{hardwareTimeoutTickets}. Manual reconciliation is required.", hardwareTimeoutTickets);
                    yield break;
                }

                saveManager?.RecordTicketPayoutFailure();
                FailActive("No PAID:n acknowledgement arrived. The command may still have dispensed, so automatic retry is disabled.", 0);
                yield break;
            }

            processRoutine = null;
            UpdateTicketsRemaining();
            if (pending.Count == 0 && activeRequest == null && failedRequest == null)
            {
                DispensingCompleted?.Invoke();
            }
        }

        private void HandleTicketsPaid(int paid)
        {
            if (activeRequest == null || !activeRequest.Sent)
            {
                Debug.LogWarning($"Balloon Rush received unsolicited PAID:{paid} with no active ticket request.");
                return;
            }

            acknowledgedTickets = Mathf.Max(0, paid);
            acknowledgementReceived = true;
        }

        private void HandleHardwarePayoutTimeout(int count)
        {
            if (activeRequest == null || !activeRequest.Sent)
            {
                Debug.LogWarning($"Balloon Rush received unsolicited PAID_TIMEOUT:{count} with no active ticket request.");
                return;
            }

            hardwareTimeoutTickets = Mathf.Max(0, count);
            hardwareTimeoutReceived = true;
        }

        private void HandleHardwareError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning($"Balloon Rush ticket hardware: {error}");
            }
        }

        private void ResetAcknowledgementState()
        {
            acknowledgementReceived = false;
            acknowledgedTickets = 0;
            hardwareTimeoutReceived = false;
            hardwareTimeoutTickets = 0;
        }

        private void FailActive(string reason, int reportedPaid, bool writeFailureAudit = true)
        {
            if (activeRequest == null)
            {
                return;
            }

            failedRequest = activeRequest;
            activeRequest = null;
            LastFaultMessage = reason ?? "Unknown payout error.";
            if (writeFailureAudit)
            {
                WriteAudit(failedRequest, reportedPaid, false, "FAILED", LastFaultMessage);
            }
            PayoutFailed?.Invoke(failedRequest.TransactionId, failedRequest.Tickets, LastFaultMessage);
            processRoutine = null;
            UpdateTicketsRemaining();
            Debug.LogError($"Balloon Rush ticket payout fault: {LastFaultMessage}");
        }

        private void UpdateTicketsRemaining()
        {
            int total = activeRequest != null ? activeRequest.Tickets : 0;
            if (failedRequest != null)
            {
                total += failedRequest.Tickets;
            }
            foreach (PayoutRequest request in pending)
            {
                total += request.Tickets;
            }
            TicketsRemaining = Mathf.Max(0, total);
        }

        private void WriteAudit(PayoutRequest request, int reportedPaid, bool verified, string status, string details)
        {
            if (request == null)
            {
                return;
            }

            try
            {
                string directory = Path.Combine(Application.persistentDataPath, AuditFolder);
                string path = Path.Combine(directory, AuditFile);
                Directory.CreateDirectory(directory);
                bool writeHeader = !File.Exists(path) || new FileInfo(path).Length == 0;
                using (StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(false)))
                {
                    if (writeHeader)
                    {
                        writer.WriteLine(AuditHeader);
                    }

                    bool hardwareEnabled = settingsManager != null && settingsManager.Current != null && settingsManager.Current.hardwareEnabled;
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(request.TransactionId),
                        Csv(request.CreatedUtc),
                        Csv(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
                        request.Tickets.ToString(CultureInfo.InvariantCulture),
                        Mathf.Max(0, reportedPaid).ToString(CultureInfo.InvariantCulture),
                        verified ? "1" : "0",
                        hardwareEnabled ? "1" : "0",
                        Csv(status),
                        Csv(details)
                    }));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Balloon Rush could not write ticket audit data: {exception.Message}");
            }
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
    }
}
