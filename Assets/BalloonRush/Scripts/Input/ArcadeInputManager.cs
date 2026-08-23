using System;
using System.Collections.Generic;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Input
{
    public sealed class ArcadeInputManager : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] inputSources = Array.Empty<MonoBehaviour>();

        private readonly List<IArcadeIO> sources = new List<IArcadeIO>();
        private SerialArcadeIO serialSource;
        private SettingsManager settingsManager;
        private bool hardwareEnabled;
        private float buttonDebounceSeconds = 0.025f;
        private float coinDebounceSeconds = 0.1f;
        private float cardDebounceSeconds = 0.75f;
        private float lastLeft = float.NegativeInfinity;
        private float lastRight = float.NegativeInfinity;
        private float lastPop = float.NegativeInfinity;
        private float lastStart = float.NegativeInfinity;
        private float lastCoin = float.NegativeInfinity;
        private float lastCard = float.NegativeInfinity;
        private float lastOperator = float.NegativeInfinity;
        private float lastBack = float.NegativeInfinity;

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

        public IReadOnlyList<IArcadeIO> Sources => sources;
        public bool HardwareEnabled => hardwareEnabled;
        public bool TicketHardwareAvailable => hardwareEnabled && serialSource != null && serialSource.IsAvailable;

        public void ConfigureSources(params MonoBehaviour[] configuredSources)
        {
            inputSources = configuredSources ?? Array.Empty<MonoBehaviour>();
        }

        public void Initialize(SettingsManager settings)
        {
            settingsManager = settings;
            RebuildSourceList();
            if (settingsManager != null)
            {
                settingsManager.SettingsChanged -= HandleSettingsChanged;
                settingsManager.SettingsChanged += HandleSettingsChanged;
                HandleSettingsChanged(settingsManager.Current);
            }
        }

        private void OnDestroy()
        {
            if (settingsManager != null)
            {
                settingsManager.SettingsChanged -= HandleSettingsChanged;
            }
            UnsubscribeAll();
        }

        /// <summary>
        /// Queues one batch TICKETS:n command only when the cabinet serial link is open.
        /// </summary>
        public bool TrySendTicketRequest(int ticketCount)
        {
            if (ticketCount <= 0 || !TicketHardwareAvailable)
            {
                return false;
            }

            serialSource.SendTicketPulse(ticketCount);
            return true;
        }

        /// <summary>Compatibility wrapper for older callers.</summary>
        public void SendTicketPulse(int ticketCount)
        {
            TrySendTicketRequest(ticketCount);
        }

        private void RebuildSourceList()
        {
            UnsubscribeAll();
            sources.Clear();
            serialSource = null;

            for (int i = 0; i < inputSources.Length; i++)
            {
                if (!(inputSources[i] is IArcadeIO source))
                {
                    continue;
                }

                sources.Add(source);
                if (source is SerialArcadeIO serial)
                {
                    serialSource = serial;
                }

                source.LeftPressed += ForwardLeft;
                source.RightPressed += ForwardRight;
                source.PopPressed += ForwardPop;
                source.StartPressed += ForwardStart;
                source.CreditPulse += ForwardCredit;
                source.OperatorPressed += ForwardOperator;
                source.BackPressed += ForwardBack;

                if (source is ITicketFeedbackSource ticketFeedback)
                {
                    ticketFeedback.TicketsPaid += ForwardTicketsPaid;
                    ticketFeedback.TicketPayoutTimedOut += ForwardTicketTimeout;
                    ticketFeedback.HardwareError += ForwardHardwareError;
                }

                source.StartIO();
            }
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < sources.Count; i++)
            {
                IArcadeIO source = sources[i];
                source.LeftPressed -= ForwardLeft;
                source.RightPressed -= ForwardRight;
                source.PopPressed -= ForwardPop;
                source.StartPressed -= ForwardStart;
                source.CreditPulse -= ForwardCredit;
                source.OperatorPressed -= ForwardOperator;
                source.BackPressed -= ForwardBack;

                if (source is ITicketFeedbackSource ticketFeedback)
                {
                    ticketFeedback.TicketsPaid -= ForwardTicketsPaid;
                    ticketFeedback.TicketPayoutTimedOut -= ForwardTicketTimeout;
                    ticketFeedback.HardwareError -= ForwardHardwareError;
                }

                source.StopIO();
            }
        }

        private void HandleSettingsChanged(OperatorSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            buttonDebounceSeconds = Mathf.Clamp(settings.inputDebounceMilliseconds / 1000f, 0f, 0.25f);
            coinDebounceSeconds = Mathf.Clamp(settings.coinDebounceMilliseconds / 1000f, 0.02f, 2f);
            cardDebounceSeconds = Mathf.Clamp(settings.cardSwipeDebounceMilliseconds / 1000f, 0.1f, 5f);
            hardwareEnabled = settings.hardwareEnabled;

            if (serialSource == null)
            {
                return;
            }

            serialSource.Configure(settings.hardwareEnabled, settings.serialPort, settings.baudRate);
            if (settings.hardwareEnabled)
            {
                serialSource.StartIO();
            }
            else
            {
                serialSource.StopIO();
            }
        }

        private void ForwardLeft()
        {
            if (ShouldForward(ref lastLeft, buttonDebounceSeconds)) LeftPressed?.Invoke();
        }

        private void ForwardRight()
        {
            if (ShouldForward(ref lastRight, buttonDebounceSeconds)) RightPressed?.Invoke();
        }

        private void ForwardPop()
        {
            if (ShouldForward(ref lastPop, buttonDebounceSeconds)) PopPressed?.Invoke();
        }

        private void ForwardStart()
        {
            if (ShouldForward(ref lastStart, buttonDebounceSeconds)) StartPressed?.Invoke();
        }

        private void ForwardCredit(CreditPulseType type)
        {
            if (type == CreditPulseType.CardSwipe)
            {
                if (ShouldForward(ref lastCard, cardDebounceSeconds)) CreditPulse?.Invoke(type);
            }
            else if (ShouldForward(ref lastCoin, coinDebounceSeconds))
            {
                CreditPulse?.Invoke(type);
            }
        }

        private void ForwardOperator()
        {
            if (ShouldForward(ref lastOperator, buttonDebounceSeconds)) OperatorPressed?.Invoke();
        }

        private void ForwardBack()
        {
            if (ShouldForward(ref lastBack, buttonDebounceSeconds)) BackPressed?.Invoke();
        }

        private void ForwardTicketsPaid(int count) => TicketsPaid?.Invoke(count);
        private void ForwardTicketTimeout(int count) => TicketPayoutTimedOut?.Invoke(count);
        private void ForwardHardwareError(string error) => HardwareError?.Invoke(error);

        private static bool ShouldForward(ref float lastTime, float debounceSeconds)
        {
            float now = Time.unscaledTime;
            if (now - lastTime < debounceSeconds)
            {
                return false;
            }

            lastTime = now;
            return true;
        }
    }
}
