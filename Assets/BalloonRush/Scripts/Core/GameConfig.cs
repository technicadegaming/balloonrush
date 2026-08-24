using BalloonRush.Audio;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Core
{
    [CreateAssetMenu(menuName = "Balloon Rush/Game Config", fileName = "BalloonRushConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Display")]
        public int targetWidth = 1080;
        public int targetHeight = 1920;
        public int targetFrameRate = 60;
        public string buildVersion = "1.4.0";
        public bool enforcePortraitResolutionInPlayer = true;
        public FullScreenMode playerFullScreenMode = FullScreenMode.FullScreenWindow;
        public bool hideCursorInPlayer = true;
        public bool runInBackground = true;
        [Min(128)] public int runtimeLogMaxKilobytes = 2048;

        [Header("World layout")]
        public float laneSpacing = 2.4f;
        public float spawnY = -6.8f;
        public float despawnY = 6.8f;
        public float hitZoneY = 3.15f;
        public float hitZoneHalfHeight = 0.82f;

        [Header("Pooling")]
        [Min(8)] public int balloonPoolSize = 48;
        [Min(8)] public int floatingTextPoolSize = 24;

        [Header("Flow")]
        [Min(1f)] public float resultsTimeout = 3f;
        [Min(0.1f)] public float preRoundDelay = 0.4f;
        [Min(0.1f)] public float postRoundDelay = 1.2f;

        [Header("Development")]
        [Tooltip("Keep disabled for production builds. Escape still opens diagnostics, but F2-F6 service actions remain locked unless this is enabled.")]
        public bool allowDebugShortcutsInRelease;

        [Header("Referenced configs")]
        public PayoutConfig payoutConfig;
        public DifficultyConfig difficultyConfig;
        public AudioConfig audioConfig;

        public OperatorSettings CreateDefaultSettings()
        {
            OperatorSettings settings = new OperatorSettings();
            settings.ApplyCommercialBalanceProfile(false);

            PayoutConfig payout = payoutConfig;
            if (payout != null && payout.balanceVersion >= 2)
            {
                settings.minimumTicketPayout = payout.minimumTicketsPerGame;
                settings.regularTicketCap = payout.regularTicketsCap;
                settings.jackpotTickets = payout.jackpotTickets;
                settings.maxTicketPayout = payout.maximumTicketsPerGame;
                settings.greenTickets = payout.greenTickets;
                settings.blueTickets = payout.blueTickets;
                settings.goldenTriggerTickets = payout.goldenTriggerTickets;
                settings.mysteryMinimum = payout.mysteryMinimum;
                settings.mysteryMaximum = payout.mysteryMaximum;
                settings.mysteryGoldenChance = payout.mysteryGoldenChance;
                settings.goldenGreatReward = payout.goldenGreatReward;
                settings.goldenGoodReward = payout.goldenGoodReward;
                settings.goldenMissReward = payout.goldenMissReward;
                settings.goodTicketMultiplier = payout.goodTicketMultiplier;
                settings.greatTicketMultiplier = payout.greatTicketMultiplier;
                settings.perfectTicketMultiplier = payout.perfectTicketMultiplier;
                settings.combo5Multiplier = payout.combo5Multiplier;
                settings.combo10Multiplier = payout.combo10Multiplier;
                settings.combo15Multiplier = payout.combo15Multiplier;
                settings.combo20Multiplier = payout.combo20Multiplier;
                settings.combo30Multiplier = payout.combo30Multiplier;
            }

            settings.Validate();
            return settings;
        }
    }
}
