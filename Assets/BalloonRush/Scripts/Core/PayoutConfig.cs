using UnityEngine;

namespace BalloonRush.Core
{
    [CreateAssetMenu(menuName = "Balloon Rush/Payout Config", fileName = "PayoutConfig")]
    public sealed class PayoutConfig : ScriptableObject
    {
        [Tooltip("Version 2 is the $1 commercial balance profile. Older generated assets are treated as legacy and are not allowed to override the safer runtime defaults.")]
        public int balanceVersion = 2;

        [Header("Visible redemption ladder")]
        public int[] visibleTiers = { 500, 250, 100, 50, 25, 10, 5, 1 };

        [Header("Commercial caps")]
        [Min(0)] public int minimumTicketsPerGame = 5;
        [Min(1)] public int regularTicketsCap = 125;
        [Min(1)] public int jackpotTickets = 500;
        [Min(1)] public int maximumTicketsPerGame = 625;

        [Header("Default ticket values")]
        [Min(0)] public int greenTickets = 1;
        [Min(0)] public int blueTickets = 5;
        [Min(0)] public int goldenTriggerTickets = 1;
        [Min(0)] public int mysteryMinimum = 1;
        [Min(0)] public int mysteryMaximum = 5;
        [Range(0f, 0.25f)] public float mysteryGoldenChance = 0.01f;

        [Header("Timing ticket multipliers")]
        [Min(0f)] public float goodTicketMultiplier = 1f;
        [Min(0f)] public float greatTicketMultiplier = 1f;
        [Min(0f)] public float perfectTicketMultiplier = 1.10f;

        [Header("Combo ticket multipliers")]
        [Min(1f)] public float combo5Multiplier = 1f;
        [Min(1f)] public float combo10Multiplier = 1f;
        [Min(1f)] public float combo15Multiplier = 1.05f;
        [Min(1f)] public float combo20Multiplier = 1.10f;
        [Min(1f)] public float combo30Multiplier = 1.15f;

        [Header("Golden round consolation")]
        [Min(0)] public int goldenGreatReward = 25;
        [Min(0)] public int goldenGoodReward = 10;
        [Min(0)] public int goldenMissReward = 3;
    }
}
