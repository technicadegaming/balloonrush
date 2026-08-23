using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace BalloonRush.Input
{
    /// <summary>
    /// The single owner of the cabinet serial port. It handles gameplay inputs,
    /// $1 card-reader credit messages, TICKETS:n output, and PAID:n verification.
    /// Reflection keeps the project playable when System.IO.Ports is unavailable.
    /// </summary>
    public sealed class SerialArcadeIO : MonoBehaviour, IArcadeIO, ITicketFeedbackSource
    {
        public event Action LeftPressed;
        public event Action RightPressed;
        public event Action PopPressed;
        public event Action StartPressed;
        public event Action<CreditPulseType> CreditPulse;
        public event Action OperatorPressed;
        public event Action BackPressed;
        public event Action<int> TicketsPaid;
        public event Action<int> TicketPayoutTimedOut;
        public event Action<string> HardwareError;

        [SerializeField] private bool hardwareEnabled;
        [SerializeField] private string portName = "COM8";
        [SerializeField] private int baudRate = 115200;
        [SerializeField] private int readTimeoutMilliseconds = 50;
        [SerializeField] private float reconnectDelaySeconds = 1f;

        private readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> outgoing = new ConcurrentQueue<string>();
        private Thread workerThread;
        private volatile bool running;
        private volatile bool portAvailable;
        private object serialPort;
        private Type serialPortType;
        private float nextVisibleWarningTime;

        public bool IsAvailable => portAvailable;
        public bool HardwareEnabled => hardwareEnabled;
        public string PortName => portName;
        public int BaudRate => baudRate;

        public void Configure(bool enabledForHardware, string configuredPort, int configuredBaudRate)
        {
            string normalizedPort = string.IsNullOrWhiteSpace(configuredPort) ? "COM8" : configuredPort.Trim();
            int normalizedBaud = Mathf.Clamp(configuredBaudRate, 1200, 921600);
            bool changed = hardwareEnabled != enabledForHardware || portName != normalizedPort || baudRate != normalizedBaud;

            hardwareEnabled = enabledForHardware;
            portName = normalizedPort;
            baudRate = normalizedBaud;

            if (!changed || !running)
            {
                return;
            }

            StopIO();
            StartIO();
        }

        public void StartIO()
        {
            if (!hardwareEnabled || running)
            {
                return;
            }

            running = true;
            workerThread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "BalloonRushSerialIO"
            };
            workerThread.Start();
        }

        public void StopIO()
        {
            running = false;
            try
            {
                if (workerThread != null && workerThread.IsAlive)
                {
                    workerThread.Join(400);
                }
            }
            catch
            {
                // Cabinet shutdown must never hang the game.
            }

            workerThread = null;
            ClosePortSafely();
            portAvailable = false;
            ClearQueue(outgoing);
        }

        public void SendTicketPulse(int ticketCount)
        {
            if (ticketCount > 0 && hardwareEnabled)
            {
                outgoing.Enqueue($"TICKETS:{ticketCount}");
            }
        }

        public void SendRawCommand(string command)
        {
            if (hardwareEnabled && !string.IsNullOrWhiteSpace(command))
            {
                outgoing.Enqueue(command.Trim());
            }
        }

        private void Update()
        {
            while (incoming.TryDequeue(out string message))
            {
                DispatchMessage(message);
            }
        }

        private void OnDestroy()
        {
            StopIO();
        }

        private void WorkerLoop()
        {
            while (running)
            {
                try
                {
                    if (serialPort == null)
                    {
                        if (!TryOpenPort())
                        {
                            Thread.Sleep(Math.Max(100, (int)Math.Round(reconnectDelaySeconds * 1000f)));
                            continue;
                        }
                    }

                    TryWriteQueuedMessages();
                    TryReadMessage();
                }
                catch (Exception exception)
                {
                    QueueHardwareError(UnwrapException(exception).Message);
                    ClosePortSafely();
                    Thread.Sleep(Math.Max(100, (int)Math.Round(reconnectDelaySeconds * 1000f)));
                }
            }

            ClosePortSafely();
        }

        private bool TryOpenPort()
        {
            serialPortType = Type.GetType("System.IO.Ports.SerialPort, System.IO.Ports")
                             ?? Type.GetType("System.IO.Ports.SerialPort");
            if (serialPortType == null)
            {
                QueueHardwareError("System.IO.Ports is unavailable for this player backend. Keyboard mode remains active.");
                running = false;
                return false;
            }

            try
            {
                serialPort = Activator.CreateInstance(serialPortType);
                SetProperty("PortName", portName);
                SetProperty("BaudRate", baudRate);
                SetProperty("ReadTimeout", readTimeoutMilliseconds);
                SetProperty("WriteTimeout", 250);
                SetProperty("NewLine", "\n");
                serialPortType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public)?.Invoke(serialPort, null);

                // Many Arduino-compatible boards reset on open. DTR matches the
                // supplied TicketManager protocol and allows the board to initialize.
                TrySetOptionalProperty("DtrEnable", true);
                portAvailable = GetProperty<bool>("IsOpen");
                if (portAvailable)
                {
                    incoming.Enqueue($"__STATUS__:OPEN:{portName}:{baudRate}");
                }
                return portAvailable;
            }
            catch (Exception exception)
            {
                QueueHardwareError($"Could not open {portName} at {baudRate}: {UnwrapException(exception).Message}");
                ClosePortSafely();
                return false;
            }
        }

        private void TryReadMessage()
        {
            try
            {
                object value = serialPortType.GetMethod("ReadLine", BindingFlags.Instance | BindingFlags.Public)?.Invoke(serialPort, null);
                if (value is string line && !string.IsNullOrWhiteSpace(line))
                {
                    incoming.Enqueue(line.Trim());
                }
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException;
                if (inner == null || inner.GetType().Name != "TimeoutException")
                {
                    throw;
                }
            }
        }

        private void TryWriteQueuedMessages()
        {
            MethodInfo writeLine = serialPortType.GetMethod(
                "WriteLine",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null);

            while (outgoing.TryDequeue(out string message))
            {
                writeLine?.Invoke(serialPort, new object[] { message });
            }
        }

        private void DispatchMessage(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                return;
            }

            string trimmed = rawMessage.Trim();
            string message = trimmed.ToUpperInvariant();

            if (message.StartsWith("__STATUS__:OPEN:", StringComparison.Ordinal))
            {
                Debug.Log($"Balloon Rush serial opened {trimmed.Substring(16)}");
                return;
            }

            if (message.StartsWith("__ERROR__:", StringComparison.Ordinal))
            {
                string error = trimmed.Substring(10);
                if (Time.unscaledTime >= nextVisibleWarningTime)
                {
                    Debug.LogWarning($"Balloon Rush serial I/O: {error}");
                    nextVisibleWarningTime = Time.unscaledTime + 5f;
                }
                HardwareError?.Invoke(error);
                return;
            }

            if (message == "READY" || message == "UNO_READY" || message == "PONG" ||
                message.StartsWith("TICKET_QUEUE:", StringComparison.Ordinal))
            {
                return;
            }

            if (TryParseCount(message, "PAID:", out int paid))
            {
                TicketsPaid?.Invoke(paid);
                return;
            }

            if (TryParseCount(message, "PAID_TIMEOUT:", out int timedOutCount))
            {
                TicketPayoutTimedOut?.Invoke(timedOutCount);
                return;
            }

            if (message.StartsWith("ERR:", StringComparison.Ordinal))
            {
                string error = trimmed.Substring(4).Trim();
                Debug.LogWarning($"Balloon Rush cabinet reported: {error}");
                HardwareError?.Invoke(error);
                return;
            }

            switch (message)
            {
                case "LEFT":
                    LeftPressed?.Invoke();
                    break;
                case "RIGHT":
                    RightPressed?.Invoke();
                    break;
                case "POP":
                    PopPressed?.Invoke();
                    break;
                case "START":
                    StartPressed?.Invoke();
                    break;
                case "COIN":
                    CreditPulse?.Invoke(CreditPulseType.Coin);
                    break;
                case "READER_CREDIT":
                case "CREDIT":
                case "CARD":
                case "SWIPE":
                    CreditPulse?.Invoke(CreditPulseType.CardSwipe);
                    break;
                case "OPERATOR":
                    OperatorPressed?.Invoke();
                    break;
                case "BACK":
                    BackPressed?.Invoke();
                    break;
                default:
                    Debug.LogWarning($"Balloon Rush ignored unknown serial message: {rawMessage}");
                    break;
            }
        }

        private void QueueHardwareError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                incoming.Enqueue("__ERROR__:" + error.Trim());
            }
        }

        private void SetProperty(string propertyName, object value)
        {
            serialPortType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.SetValue(serialPort, value, null);
        }

        private void TrySetOptionalProperty(string propertyName, object value)
        {
            try
            {
                SetProperty(propertyName, value);
            }
            catch
            {
                // Optional serial properties vary by backend.
            }
        }

        private T GetProperty<T>(string propertyName)
        {
            object value = serialPortType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(serialPort, null);
            return value is T typed ? typed : default;
        }

        private void ClosePortSafely()
        {
            object localPort = serialPort;
            serialPort = null;
            portAvailable = false;
            if (localPort == null || serialPortType == null)
            {
                return;
            }

            try
            {
                object value = serialPortType.GetProperty("IsOpen", BindingFlags.Instance | BindingFlags.Public)?.GetValue(localPort, null);
                if (value is bool isOpen && isOpen)
                {
                    serialPortType.GetMethod("Close", BindingFlags.Instance | BindingFlags.Public)?.Invoke(localPort, null);
                }

                if (localPort is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch
            {
                // Disconnected hardware must never crash the cabinet game.
            }
        }

        private static bool TryParseCount(string message, string prefix, out int count)
        {
            count = 0;
            if (!message.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(message.Substring(prefix.Length).Trim(), out count) && count >= 0;
        }

        private static Exception UnwrapException(Exception exception)
        {
            while (exception is TargetInvocationException target && target.InnerException != null)
            {
                exception = target.InnerException;
            }
            return exception;
        }

        private static void ClearQueue(ConcurrentQueue<string> queue)
        {
            while (queue.TryDequeue(out _))
            {
            }
        }
    }
}
