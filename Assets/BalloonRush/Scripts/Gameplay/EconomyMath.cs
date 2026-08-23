using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public static class EconomyMath
    {
        public static float CalculatePrizeBudgetCents(OperatorSettings settings)
        {
            if (settings == null)
            {
                return 20f;
            }

            float playPrice = Mathf.Max(1f, settings.pricePerPlayCents);
            float targetPercent = Mathf.Clamp(settings.targetPrizeCostPercent, 0f, 100f);
            return playPrice * targetPercent / 100f;
        }

        public static int CalculateTargetAverageTickets(OperatorSettings settings)
        {
            if (settings == null)
            {
                return 40;
            }

            float costPerTicket = Mathf.Max(0.01f, settings.estimatedPrizeCostPerTicketCents);
            return Mathf.Max(1, Mathf.FloorToInt(CalculatePrizeBudgetCents(settings) / costPerTicket));
        }

        public static float EstimatePrizeCostCents(float tickets, OperatorSettings settings)
        {
            if (settings == null)
            {
                return Mathf.Max(0f, tickets) * 0.5f;
            }

            return Mathf.Max(0f, tickets) * Mathf.Max(0.01f, settings.estimatedPrizeCostPerTicketCents);
        }

        public static float EstimatePrizeCostPercent(float averageTicketsPerGame, OperatorSettings settings)
        {
            if (settings == null)
            {
                return 0f;
            }

            float price = Mathf.Max(1f, settings.pricePerPlayCents);
            return EstimatePrizeCostCents(averageTicketsPerGame, settings) / price * 100f;
        }
    }
}
