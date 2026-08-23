using UnityEngine;

namespace BalloonRush.Gameplay
{
    /// <summary>
    /// Pure ticket math. ScoreManager banks the raw fractional value so repeated
    /// small multipliers are accurate without rounding every +1 balloon upward.
    /// </summary>
    public static class TicketMath
    {
        public static float CalculateRawAward(
            int baseTickets,
            TimingRating rating,
            int combo,
            float activePayoutMultiplier,
            float goodMultiplier = 1f,
            float greatMultiplier = 1.05f,
            float perfectMultiplier = 1.20f,
            float combo5Multiplier = 1.05f,
            float combo10Multiplier = 1.10f,
            float combo15Multiplier = 1.20f,
            float combo20Multiplier = 1.35f,
            float combo30Multiplier = 1.50f)
        {
            if (baseTickets <= 0 || rating == TimingRating.Miss)
            {
                return 0f;
            }

            float timingMultiplier = Mathf.Max(0f, goodMultiplier);
            if (rating == TimingRating.Great) timingMultiplier = Mathf.Max(0f, greatMultiplier);
            if (rating == TimingRating.Perfect) timingMultiplier = Mathf.Max(0f, perfectMultiplier);

            float comboMultiplier = GetComboTicketMultiplier(
                combo,
                combo5Multiplier,
                combo10Multiplier,
                combo15Multiplier,
                combo20Multiplier,
                combo30Multiplier);

            return Mathf.Max(0f, baseTickets * timingMultiplier * comboMultiplier * Mathf.Max(1f, activePayoutMultiplier));
        }

        public static int CalculateAward(
            int baseTickets,
            TimingRating rating,
            int combo,
            float activePayoutMultiplier,
            float goodMultiplier = 1f,
            float greatMultiplier = 1.05f,
            float perfectMultiplier = 1.20f,
            float combo5Multiplier = 1.05f,
            float combo10Multiplier = 1.10f,
            float combo15Multiplier = 1.20f,
            float combo20Multiplier = 1.35f,
            float combo30Multiplier = 1.50f)
        {
            float raw = CalculateRawAward(
                baseTickets,
                rating,
                combo,
                activePayoutMultiplier,
                goodMultiplier,
                greatMultiplier,
                perfectMultiplier,
                combo5Multiplier,
                combo10Multiplier,
                combo15Multiplier,
                combo20Multiplier,
                combo30Multiplier);
            return raw <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        public static float GetComboTicketMultiplier(
            int combo,
            float combo5Multiplier = 1.05f,
            float combo10Multiplier = 1.10f,
            float combo15Multiplier = 1.20f,
            float combo20Multiplier = 1.35f,
            float combo30Multiplier = 1.50f)
        {
            float at5 = Mathf.Max(1f, combo5Multiplier);
            float at10 = Mathf.Max(at5, combo10Multiplier);
            float at15 = Mathf.Max(at10, combo15Multiplier);
            float at20 = Mathf.Max(at15, combo20Multiplier);
            float at30 = Mathf.Max(at20, combo30Multiplier);

            if (combo >= 30) return at30;
            if (combo >= 20) return at20;
            if (combo >= 15) return at15;
            if (combo >= 10) return at10;
            if (combo >= 5) return at5;
            return 1f;
        }
    }
}
