using System;
using UnityEngine;

namespace BalloonRush.SaveSystem
{
    [Serializable]
    public sealed class OperatorSettings
    {
        [Header("Game and credits")]
        public float gameDuration = 30f;
        public int creditsPerPlay = 1;
        public bool freePlay;
        public int coinValue = 1;
        public int cardSwipeValue = 1;
        public int pricePerPlayCents = 100;

        [Header("Payout economics")]
        [Tooltip("Estimated wholesale prize cost represented by one ticket, in cents. Example: 0.50 means two tickets cost about one cent in prizes.")]
        public float estimatedPrizeCostPerTicketCents = 0.50f;
        [Tooltip("Target prize-cost percentage of the play price. This is advisory and is shown in the Operator Menu.")]
        public float targetPrizeCostPercent = 20f;
        public int minimumTicketPayout = 5;
        [Tooltip("Maximum tickets earned from normal balloons and mystery awards before Golden Round bonuses.")]
        public int regularTicketCap = 125;

        [Header("Payout")]
        public int jackpotTickets = 500;
        public int maxTicketPayout = 625;
        public int greenTickets = 1;
        public int blueTickets = 5;
        public int goldenTriggerTickets = 1;
        public int mysteryMinimum = 1;
        public int mysteryMaximum = 5;
        [Range(0f, 0.25f)] public float mysteryGoldenChance = 0.01f;
        public int goldenGreatReward = 25;
        public int goldenGoodReward = 10;
        public int goldenMissReward = 3;
        public int bombTicketPenalty;
        public float goodTicketMultiplier = 1.0f;
        public float greatTicketMultiplier = 1.0f;
        public float perfectTicketMultiplier = 1.10f;

        [Header("Gameplay")]
        public float balloonBaseSpeed = 2.65f;
        public float spawnInterval = 1.15f;
        public float greenSpawnWeight = 1.0f;
        public float blueSpawnWeight = 0.08f;
        public float bombSpawnWeight = 0.10f;
        public float superBombSpawnWeight = 0.01f;
        public float goldenSpawnWeight = 0.0004f;
        public float mysterySpawnWeight = 0.03f;
        public float multiplierSpawnWeight = 0.025f;
        public float comboTimeout = 2.75f;
        public float combo5Multiplier = 1.0f;
        public float combo10Multiplier = 1.0f;
        public float combo15Multiplier = 1.05f;
        public float combo20Multiplier = 1.10f;
        public float combo30Multiplier = 1.15f;
        public float perfectWindow = 0.20f;
        public float greatWindow = 0.45f;
        public float goodWindow = 0.75f;
        public float x2Duration = 2.5f;
        public float goldenRoundDuration = 10f;
        public bool passedBalloonBreaksCombo = true;

        [Header("Audio and accessibility")]
        public float masterVolume = 0.9f;
        public float musicVolume = 0.65f;
        public float sfxVolume = 0.9f;
        public bool cabinetEdgeLightsEnabled = true;
        public float attractEdgeFlickerIntensity = 0.85f;
        public float gameplayEdgePulseMinHz = 1.35f;
        public float gameplayEdgePulseMaxHz = 4.25f;
        public bool gameplayMusicRotationEnabled = true;
        [Tooltip("Seconds before crossfading to the next normal gameplay music track.")]
        public float gameplayMusicRotateSeconds = 8f;
        [Tooltip("Gameplay music pitch at the beginning of a round.")]
        public float gameplayMusicStartPitch = 0.98f;
        [Tooltip("Gameplay music pitch at the end of a round. Pitch follows DifficultyManager progress.")]
        public float gameplayMusicEndPitch = 1.12f;
        public bool reducedScreenShake;
        public bool reducedFlashes;

        [Header("Arcade hardware")]
        public bool hardwareEnabled;
        public string serialPort = "COM8";
        public int baudRate = 115200;
        [Tooltip("Legacy pulse setting retained for save compatibility. Balloon Rush sends one TICKETS:n batch command to the Arduino.")]
        public int ticketsPerPulse = 1;
        [Tooltip("Legacy pulse setting retained for save compatibility. The Arduino controls the actual dispenser pulse timing.")]
        public float pulseDelay = 0.035f;
        public int inputDebounceMilliseconds = 25;
        public int coinDebounceMilliseconds = 100;
        public int cardSwipeDebounceMilliseconds = 750;
        [Tooltip("How long Unity waits for the serial device to become available before flagging the payout for operator review.")]
        public float ticketHardwareWaitTimeoutSeconds = 12f;
        [Tooltip("How long Unity waits for PAID:n after sending TICKETS:n. Unity never retries automatically because a lost acknowledgement could otherwise double-pay.")]
        public float ticketPaidAckTimeoutSeconds = 30f;

        public OperatorSettings Clone()
        {
            return JsonUtility.FromJson<OperatorSettings>(JsonUtility.ToJson(this));
        }

        public void ApplyCommercialBalanceProfile(bool preserveHardwareSettings = true)
        {
            bool oldHardwareEnabled = hardwareEnabled;
            string oldSerialPort = serialPort;
            int oldBaudRate = baudRate;
            int oldInputDebounce = inputDebounceMilliseconds;
            int oldCoinDebounce = coinDebounceMilliseconds;
            int oldCardDebounce = cardSwipeDebounceMilliseconds;
            float oldHardwareWait = ticketHardwareWaitTimeoutSeconds;
            float oldAckTimeout = ticketPaidAckTimeoutSeconds;

            gameDuration = 30f;
            creditsPerPlay = 1;
            freePlay = false;
            coinValue = 1;
            cardSwipeValue = 1;
            pricePerPlayCents = 100;

            estimatedPrizeCostPerTicketCents = 0.50f;
            targetPrizeCostPercent = 20f;
            minimumTicketPayout = 5;
            regularTicketCap = 125;

            jackpotTickets = 500;
            maxTicketPayout = 625;
            greenTickets = 1;
            blueTickets = 5;
            goldenTriggerTickets = 1;
            mysteryMinimum = 1;
            mysteryMaximum = 5;
            mysteryGoldenChance = 0.01f;
            goldenGreatReward = 25;
            goldenGoodReward = 10;
            goldenMissReward = 3;
            bombTicketPenalty = 0;
            goodTicketMultiplier = 1.0f;
            greatTicketMultiplier = 1.0f;
            perfectTicketMultiplier = 1.10f;

            balloonBaseSpeed = 2.65f;
            spawnInterval = 1.15f;
            greenSpawnWeight = 1.0f;
            blueSpawnWeight = 0.08f;
            bombSpawnWeight = 0.10f;
            superBombSpawnWeight = 0.01f;
            goldenSpawnWeight = 0.0004f;
            mysterySpawnWeight = 0.03f;
            multiplierSpawnWeight = 0.025f;
            comboTimeout = 2.75f;
            combo5Multiplier = 1.0f;
            combo10Multiplier = 1.0f;
            combo15Multiplier = 1.05f;
            combo20Multiplier = 1.10f;
            combo30Multiplier = 1.15f;
            perfectWindow = 0.20f;
            greatWindow = 0.45f;
            goodWindow = 0.75f;
            x2Duration = 2.5f;
            goldenRoundDuration = 10f;
            passedBalloonBreaksCombo = true;

            gameplayMusicRotationEnabled = true;
            gameplayMusicRotateSeconds = 8f;
            gameplayMusicStartPitch = 0.96f;
            gameplayMusicEndPitch = 1.18f;

            if (preserveHardwareSettings)
            {
                hardwareEnabled = oldHardwareEnabled;
                serialPort = oldSerialPort;
                baudRate = oldBaudRate;
                inputDebounceMilliseconds = oldInputDebounce;
                coinDebounceMilliseconds = oldCoinDebounce;
                cardSwipeDebounceMilliseconds = oldCardDebounce;
                ticketHardwareWaitTimeoutSeconds = oldHardwareWait;
                ticketPaidAckTimeoutSeconds = oldAckTimeout;
            }
            else
            {
                hardwareEnabled = false;
                serialPort = "COM8";
                baudRate = 115200;
                ticketsPerPulse = 1;
                pulseDelay = 0.035f;
                inputDebounceMilliseconds = 25;
                coinDebounceMilliseconds = 100;
                cardSwipeDebounceMilliseconds = 750;
                ticketHardwareWaitTimeoutSeconds = 12f;
                ticketPaidAckTimeoutSeconds = 30f;
            }

            Validate();
        }

        public void Validate()
        {
            gameDuration = Mathf.Clamp(gameDuration, 20f, 120f);
            creditsPerPlay = Mathf.Clamp(creditsPerPlay, 1, 20);
            coinValue = Mathf.Clamp(coinValue, 1, 100);
            cardSwipeValue = Mathf.Clamp(cardSwipeValue, 1, 100);
            pricePerPlayCents = Mathf.Clamp(pricePerPlayCents, 25, 2000);

            estimatedPrizeCostPerTicketCents = Mathf.Clamp(estimatedPrizeCostPerTicketCents, 0.01f, 25f);
            targetPrizeCostPercent = Mathf.Clamp(targetPrizeCostPercent, 1f, 80f);

            jackpotTickets = Mathf.Clamp(jackpotTickets, 1, 500);
            regularTicketCap = Mathf.Clamp(regularTicketCap, 1, 500);
            int minimumTotalCap = Mathf.Min(1000, jackpotTickets + regularTicketCap);
            maxTicketPayout = Mathf.Clamp(maxTicketPayout, minimumTotalCap, 1000);
            minimumTicketPayout = Mathf.Clamp(minimumTicketPayout, 0, regularTicketCap);
            greenTickets = Mathf.Clamp(greenTickets, 0, 100);
            blueTickets = Mathf.Clamp(blueTickets, 0, 100);
            goldenTriggerTickets = Mathf.Clamp(goldenTriggerTickets, 0, 250);
            mysteryMinimum = Mathf.Clamp(mysteryMinimum, 0, 250);
            mysteryMaximum = Mathf.Clamp(mysteryMaximum, mysteryMinimum, 500);
            mysteryGoldenChance = Mathf.Clamp(mysteryGoldenChance, 0f, 0.25f);
            goldenGreatReward = Mathf.Clamp(goldenGreatReward, 0, jackpotTickets);
            goldenGoodReward = Mathf.Clamp(goldenGoodReward, 0, goldenGreatReward);
            goldenMissReward = Mathf.Clamp(goldenMissReward, 0, goldenGoodReward);
            bombTicketPenalty = Mathf.Clamp(bombTicketPenalty, 0, 100);
            if (goodTicketMultiplier <= 0f) goodTicketMultiplier = 1.0f;
            if (greatTicketMultiplier <= 0f) greatTicketMultiplier = 1.0f;
            if (perfectTicketMultiplier <= 0f) perfectTicketMultiplier = 1.10f;
            goodTicketMultiplier = Mathf.Clamp(goodTicketMultiplier, 0.1f, 5f);
            greatTicketMultiplier = Mathf.Clamp(greatTicketMultiplier, goodTicketMultiplier, 5f);
            perfectTicketMultiplier = Mathf.Clamp(perfectTicketMultiplier, greatTicketMultiplier, 5f);

            balloonBaseSpeed = Mathf.Clamp(balloonBaseSpeed, 0.8f, 8f);
            spawnInterval = Mathf.Clamp(spawnInterval, 0.2f, 3f);
            greenSpawnWeight = Mathf.Clamp(greenSpawnWeight, 0f, 5f);
            blueSpawnWeight = Mathf.Clamp(blueSpawnWeight, 0f, 5f);
            bombSpawnWeight = Mathf.Clamp01(bombSpawnWeight);
            superBombSpawnWeight = Mathf.Clamp01(superBombSpawnWeight);
            goldenSpawnWeight = Mathf.Clamp(goldenSpawnWeight, 0f, 0.25f);
            mysterySpawnWeight = Mathf.Clamp01(mysterySpawnWeight);
            multiplierSpawnWeight = Mathf.Clamp01(multiplierSpawnWeight);
            comboTimeout = Mathf.Clamp(comboTimeout, 0.5f, 10f);
            if (greenSpawnWeight + blueSpawnWeight + bombSpawnWeight + superBombSpawnWeight + goldenSpawnWeight + mysterySpawnWeight + multiplierSpawnWeight <= 0.0001f)
            {
                greenSpawnWeight = 1f;
            }
            if (combo5Multiplier <= 0f) combo5Multiplier = 1.0f;
            if (combo10Multiplier <= 0f) combo10Multiplier = 1.0f;
            if (combo15Multiplier <= 0f) combo15Multiplier = 1.05f;
            if (combo20Multiplier <= 0f) combo20Multiplier = 1.10f;
            if (combo30Multiplier <= 0f) combo30Multiplier = 1.15f;
            combo5Multiplier = Mathf.Clamp(combo5Multiplier, 1f, 5f);
            combo10Multiplier = Mathf.Clamp(combo10Multiplier, combo5Multiplier, 5f);
            combo15Multiplier = Mathf.Clamp(combo15Multiplier, combo10Multiplier, 5f);
            combo20Multiplier = Mathf.Clamp(combo20Multiplier, combo15Multiplier, 5f);
            combo30Multiplier = Mathf.Clamp(combo30Multiplier, combo20Multiplier, 5f);
            perfectWindow = Mathf.Clamp(perfectWindow, 0.05f, 0.5f);
            greatWindow = Mathf.Clamp(greatWindow, perfectWindow, 0.8f);
            goodWindow = Mathf.Clamp(goodWindow, greatWindow, 1f);
            x2Duration = Mathf.Clamp(x2Duration, 1f, 20f);
            goldenRoundDuration = Mathf.Clamp(goldenRoundDuration, 4f, 30f);

            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            attractEdgeFlickerIntensity = Mathf.Clamp01(attractEdgeFlickerIntensity);
            gameplayEdgePulseMinHz = Mathf.Clamp(gameplayEdgePulseMinHz, 0.4f, 3f);
            gameplayEdgePulseMaxHz = Mathf.Clamp(gameplayEdgePulseMaxHz, gameplayEdgePulseMinHz, 5f);
            gameplayMusicRotateSeconds = Mathf.Clamp(gameplayMusicRotateSeconds, 4f, 30f);
            gameplayMusicStartPitch = Mathf.Clamp(gameplayMusicStartPitch, 0.75f, 1.15f);
            gameplayMusicEndPitch = Mathf.Clamp(gameplayMusicEndPitch, gameplayMusicStartPitch, 1.35f);
            baudRate = Mathf.Clamp(baudRate, 1200, 921600);
            ticketsPerPulse = Mathf.Clamp(ticketsPerPulse, 1, 10);
            pulseDelay = Mathf.Clamp(pulseDelay, 0.005f, 0.5f);
            inputDebounceMilliseconds = Mathf.Clamp(inputDebounceMilliseconds, 0, 250);
            coinDebounceMilliseconds = Mathf.Clamp(coinDebounceMilliseconds, 20, 2000);
            cardSwipeDebounceMilliseconds = Mathf.Clamp(cardSwipeDebounceMilliseconds, 100, 5000);
            ticketHardwareWaitTimeoutSeconds = Mathf.Clamp(ticketHardwareWaitTimeoutSeconds, 1f, 120f);
            ticketPaidAckTimeoutSeconds = Mathf.Clamp(ticketPaidAckTimeoutSeconds, 2f, 180f);
            if (string.IsNullOrWhiteSpace(serialPort))
            {
                serialPort = "COM8";
            }
        }
    }
}
